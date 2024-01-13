﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Fasterflect;
using Hangfire;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public static class JobSchedulerTestExtensions {

        public static IObservable<JobState> Executed(this WorkerState lastState,Func<Job,bool> job=null) 
            => JobSchedulerService.JobState.TakeFirst(t => t.Fit(null, WorkerState.Enqueued)).IgnoreElements()
                .Concat(Observable.Defer(() => JobSchedulerService.JobState.TakeFirst(jobState => jobState.Fit(null, WorkerState.Processing)).IgnoreElements()))
                .Concat(Observable.Defer(() => JobSchedulerService.JobState.TakeFirst(jobState => jobState.Fit(job, lastState))))
                .TakeFirst();

        private static bool Fit(this JobState jobState, Func<Job, bool> job, WorkerState workerState) 
            => jobState.State == workerState && (job == null || job(jobState.JobWorker.Job));

        public static IObservable<GenericEventArgs<IObservable<Job>>> Handle(this IObservable<GenericEventArgs<IObservable<Job>>> source)
            => source.Do(e => e.Handled = true);

        public static Job CommitNewJob(this BlazorApplication application,Type testJobType=null,string methodName=null,Action<Job> modify=null) {
            testJobType ??= typeof(TestJobDI);
            methodName??=nameof(TestJob.TestJobId);
            var objectSpace = application.CreateObjectSpace();
            var job = objectSpace.CreateObject<Job>();
            job.JobType = new ObjectType(testJobType);
            job.JobMethod = new ObjectString(methodName);
            job.CronExpression = job.ObjectSpace.GetObjectsQuery<CronExpression>()
                .FirstOrDefault(expression => expression.Name == nameof(Cron.Minutely));
            job.Id = ScheduledJobId;
            modify?.Invoke(job);
            objectSpace.CommitChanges();
            return job;
        }
        
        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";


    }
}