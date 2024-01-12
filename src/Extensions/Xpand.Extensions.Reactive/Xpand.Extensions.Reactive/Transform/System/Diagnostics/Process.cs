﻿using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.System.Diagnostics{
    public static class ProcessEx{
        
        public static IObservable<float> PrivateBytes(this Process process,IObservable<Unit> signal) 
            => Observable.Using(() => new PerformanceCounter("Process", "Private Bytes", process.ProcessName),counter => signal
                .Select(_ => counter.NextValue()).Select(sample => sample));

        public static bool StartWithEvents(this Process process,bool outputDataReceived=true,bool outputErrorReceived=true,bool enableRaisingEvents=true,bool createNoWindow=true){
            process.StartInfo.RedirectStandardOutput = outputDataReceived;
            process.StartInfo.RedirectStandardError = outputErrorReceived;
            if (outputDataReceived||outputErrorReceived){
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = createNoWindow;
            }
            var start = process.Start();
            process.EnableRaisingEvents = enableRaisingEvents;
            if (start&&outputDataReceived){
                process.BeginOutputReadLine();    
            }
            if (start&&outputErrorReceived){
                process.BeginErrorReadLine();    
            }
            return start;
        }

        public static IObservable<string> WhenOutputDataReceived(this Process process)
            => process.WhenEvent<DataReceivedEventArgs>(nameof(Process.OutputDataReceived))
                // .TakeUntil(process.WhenExited())
                .Select(pattern => pattern.Data);

        public static IObservable<string> WhenErrorDataReceived(this Process process)
            => process.WhenEvent<DataReceivedEventArgs>(nameof(Process.ErrorDataReceived))
                .Select(pattern => pattern.Data);

        public static IObservable<Process> WhenExited(this Process process) 
            => process.WhenEvent(nameof(Process.Exited)).Take(1).To(process);
    }
}