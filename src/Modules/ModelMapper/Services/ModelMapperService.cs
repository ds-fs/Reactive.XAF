﻿using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    internal static class ModelMapperService{
        internal static IObservable<TSource> TraceModelMapper<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ModelMapperModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);

    }
}