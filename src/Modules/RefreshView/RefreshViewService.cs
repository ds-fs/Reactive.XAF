﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.RefreshView{
    public static class RefreshViewService{
        internal static IObservable<TSource> TraceRefreshView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, RefreshViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);

        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.RefreshView());

        private static IObservable<Unit> RefreshView(this XafApplication application)
            => application.WhenFrame().SelectUntilViewClosed(frame => application.ReactiveModulesModel().RefreshViewModel()
                .SelectMany(model => model.Items.Where(_ => _.View == frame.View.Model && _.Interval != TimeSpan.Zero).ToNowObservable()
                    .SelectMany(item => item.Interval.Interval().ObserveOnContext()
                        .Do(l => frame.View?.RefreshDataSource())))).ToUnit();
        // => application.WhenViewOnFrame().CombineLatest(application.ReactiveModulesModel().RefreshViewModel(),
        //         (frame, model) => {
        //             var synchronizationContext = SynchronizationContext.Current;
        //             return model.Items
        //                 .Where(_ => _.View == frame.View.Model && _.Interval != TimeSpan.Zero)
        //                 .ToObservable()
        //                 .SelectMany(item => Observable.Interval(item.Interval)
        //                     .TakeUntil(frame.View.WhenClosing())
        //                     .ObserveOn(synchronizationContext!)
        //                     .Select(_ => {
        //                         frame.View?.RefreshDataSource();
        //                         return frame?.View;
        //                     })
        //                 );
        //         }).Merge()
        //     .TraceRefreshView(view => view.Id)
        //     .ToUnit();
    }
}