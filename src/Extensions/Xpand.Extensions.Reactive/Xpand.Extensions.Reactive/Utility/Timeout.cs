﻿using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        
        public static IObservable<TSource> Timeout<TSource>(
            this IObservable<TSource> source, TimeSpan dueTime, string timeoutMessage) 
            => source.Timeout(dueTime, new TimeoutException(timeoutMessage).Throw<TSource>());
        
        public static IObservable<TSource> Timeout<TSource>(
            this IObservable<TSource> source, TimeSpan dueTime, Exception exception) 
            => source.Timeout(dueTime, exception.Throw<TSource>());
        public static IObservable<TSource> SilentTimeout<TSource>(
            this IObservable<TSource> source, TimeSpan dueTime) 
            => source.Timeout(dueTime).CompleteOnTimeout();
        
    }
}