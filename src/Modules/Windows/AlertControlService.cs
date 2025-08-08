﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.XtraBars.Alerter;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Harmony;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Windows{
    static class AlertControlService {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void Show(AlertControl __instance, Form owner, AlertInfo info) {
            var applicationModel = CaptionHelper.ApplicationModel;
            if (applicationModel!=null) {
                var modelWindows = applicationModel.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
                __instance.FormLocation = modelWindows.Alert.FormLocation;
                if (modelWindows.Alert.FormWidth != null) {
                    __instance.ProcessEvent<AlertFormWidthEventArgs>(nameof(AlertControl.GetDesiredAlertFormWidth)).Take(1)
                        .Subscribe(e => e.Width = modelWindows.Alert.FormWidth.Value);
                }
            }
            
        }

        static AlertControlService() 
            => new HarmonyMethod(typeof(AlertControlService), nameof(Show))
                .PreFix(typeof(AlertControl).Method(nameof(AlertControl.Show), [typeof(Form), typeof(AlertInfo)]),true);

        public static IObservable<Unit> ConnectAlertForm(this ApplicationModulesManager manager) => Observable.Empty<Unit>();
    }
}