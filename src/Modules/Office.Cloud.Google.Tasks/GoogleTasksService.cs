﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;

using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tasks{
    public static class GoogleTasksService{
        private static readonly ISubject<(Frame frame, UserCredential userCredential)> CredentialsSubject=new Subject<(Frame frame, UserCredential client)>();
        public static IObservable<(Frame frame, UserCredential credential)> Credentials => CredentialsSubject.AsObservable();
        public const string DefaultTasksListId = "@default";
        static readonly Subject<(Task serviceObject, MapAction mapAction)> UpdatedSubject=new();
        public static IObservable<(Task cloud, MapAction mapAction)> Updated{ get; }=UpdatedSubject.AsObservable();
        static readonly Subject<GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)>> CustomizeSynchronizationSubject =
            new();
        
        
        public static IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)>> CustomizeSynchronization 
            => CustomizeSynchronizationSubject.AsObservable();

        
        public static IObservable<(Task cloud, MapAction mapAction)> When(this IObservable<(Task outlookTask, MapAction mapAction)> source, MapAction mapAction)
            => source.Where(t => t.mapAction == mapAction);
        
        
        public static IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)>> When(
            this IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)>> source, MapAction mapAction)
            => source.Where(e => e.Instance.mapAction == mapAction);
        
        private static IObservable<(Task serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<TasksService> source,IModelTasksItem modelTasksItem, 
            IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,TaskList taskList) 
            => source.SelectMany(service => objectSpaceFactory.SynchronizeCloud<Task, ITask>(modelTasksItem.SynchronizationType,objectSpace,
                cloudId => RequestCustomization.Default(service.Tasks.Delete(taskList.Id, cloudId)).ToObservable<string>().ToUnit(),
                task => RequestCustomization.Default(service.Tasks.Insert(task, taskList.Id)).ToObservable<Task>(),
                t => RequestCustomization.Default(service.Tasks.Get(taskList.Id, t.cloudId)).ToObservable<Task>(),
                t => RequestCustomization.Default(service.Tasks.Update(t.cloudEntity, taskList.Id, t.cloudId)).ToObservable<Task>(),
                e => e.Handled=MapAction.Delete.CustomSynchronization(e.Instance.cloudOfficeObject.ObjectSpace, e.Instance.localEntinty, null).Handled,
                t => MapAction.Insert.CustomSynchronization(null, t.source, t.target),
                t => MapAction.Update.CustomSynchronization(null, t.source, t.target)));

        private static GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)> CustomSynchronization(
            this MapAction mapAction,IObjectSpace objectSpace, ITask local, Task cloud){
            var e = new GenericEventArgs<(IObjectSpace objectSpace, ITask local, Task cloud, MapAction mapAction)>((objectSpace, local, cloud, mapAction));
            CustomizeSynchronizationSubject.OnNext(e);
            if (!e.Handled){
                if (e.Instance.mapAction!=MapAction.Delete){
                    cloud.Title = local.Subject;
                    cloud.Notes = local.Description;
                    cloud.Status = local.Status == TaskStatus.Completed ? "completed" : "needsAction";
                    if (local.DateCompleted != DateTime.MinValue){
                        cloud.Completed=local.DateCompleted.ToRfc3339String();

                    }
                    if (local.DueDate != DateTime.MinValue){
                        cloud.Due = local.DueDate.ToRfc3339String();
                    }
                }
            }

            return e;
        }

        public static IObservable<TaskList> GetTaskList(this TasksService tasksService, string title=null, bool createNew = false,bool returnDefault=false){
            if (returnDefault){
                return tasksService.Tasklists.Get(DefaultTasksListId).ToObservable();
            }
            var addNew = createNew.Observe().WhenNotDefault()
                .SelectMany(_ => tasksService.Tasklists.Insert(new TaskList() { Title = title }).ToObservable())
                .SelectMany(_ => tasksService.GetTaskList(title));
            return tasksService.Tasklists.List().ToObservable().SelectMany(list => list.Items).FirstOrDefaultAsync(entry => entry.Title == title)
                .SwitchIfDefault(addNew);
        }

        internal static IObservable<TSource> TraceGoogleTasksModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GoogleTasksModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => Observable.Defer(() => application.WhenViewOnFrame()
		            .When(_ => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google()
			            .Tasks().Items.Select(item => item.ObjectView)).Authorize())
	            .Retry(application)
                .SynchronizeCloud()
                .ToUnit()
                .Merge(manager.ConfigureModel()));

        private static IObservable<Unit> ConfigureModel(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes(modelApplication => modelApplication.BOModel)
                .Do(model => model.Application.OAuthGoogle().AddScopes(TasksService.Scope.Tasks)).ToUnit();

        private static IObservable<(Frame frame, UserCredential credential, TaskList taskList)> EnsureTasksList(this IObservable<(Frame frame, UserCredential credential)> source) 
            => source.Select(t => {
                    var defaultTaskListName = t.frame.View.AsObjectView().Application().Model
                        .ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().Tasks().DefaultTaskListName;
                    return Observable.Start(() => t.credential.NewService<TasksService>().GetTaskList(defaultTaskListName, true,defaultTaskListName==DefaultTasksListId)).Merge().Wait().Observe().AsObservable()
                        .Select(folder => (t.frame, t.credential, folder));
                }).Merge()
                .TraceGoogleTasksModule(folder => folder.folder.Title);

        private static IObservable<(Task serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<(Frame frame, UserCredential credential, TaskList taskList,IModelTasksItem modelTodoItem)> source) 
            => source.Select(t => t.credential.NewService<TasksService>().Observe().SynchronizeCloud(
                        t.modelTodoItem, t.frame.View.ObjectSpace, t.frame.View.AsObjectView().Application().CreateObjectSpace,t.taskList)
                    .TakeUntil(t.frame.View.WhenClosing())
                ).Switch()
                .Do(UpdatedSubject.OnNext)
                .TraceGoogleTasksModule(t => $"{t.mapAction} {t.serviceObject.Title}, {t.serviceObject.Status}, {t.serviceObject.Id}");
        
        public static IObservable<global::Google.Apis.Tasks.v1.Data.Tasks[]> ListTasks(this TasksService tasksService,string tasksListId, ICloudOfficeToken cloudOfficeToken = null,
            Action<ICloudOfficeToken> @finally = null,  int? maxResults = 100, Func<GoogleApiException, bool> repeat = null){
            if (maxResults < 20){
                throw new ArgumentOutOfRangeException($"{nameof(maxResults)} is less than 20");
            }

            return tasksService.Tasks.List(tasksListId).List(maxResults, cloudOfficeToken, @finally, repeat)
                .Select(tasks => tasks.Select(t => {
                    t.Items ??= new List<Task>();
                    return t;
                }).ToArray());
        }

        public static IObservable<string> DeleteAllTasks(this TasksService tasksService, string tasksListId) 
            => tasksService.ListTasks(tasksListId)
                .SelectMany(tasks => tasks)
                .Where(tasks => tasks.Items != null)
                .SelectMany(tasks => tasks.Items)
                .SelectMany(t => Observable.FromAsync(() => tasksService.Tasks.Delete(tasksListId, t.Id).ExecuteAsync()))
                .LastOrDefaultAsync();

        
        static IObservable<(Frame frame, UserCredential credential, TaskList taskList, IModelTasksItem modelTodoItem)> Authorize(this  IObservable<Frame> source) 
            => source.AuthorizeGoogle().Select(t => t).EnsureTasksList()
            .Publish().RefCount()
            .Do(tuple => CredentialsSubject.OnNext((tuple.frame,tuple.credential)))
            .SelectMany(t => t.frame.Application.Model
                .ToReactiveModule<IModelReactiveModuleOffice>().Office
                .Google().Tasks().Items.Where(item => item.ObjectView == t.frame.View.Model)
                .Select(item => (t.frame, t.credential, t.taskList, item)))
            .TraceGoogleTasksModule(t => t.frame.View.Id);
    }
}