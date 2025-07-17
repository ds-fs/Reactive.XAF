﻿using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Logger {
    
    public sealed class ReactiveLoggerModule : ReactiveModuleBase{
        
        public const string CategoryName = "Xpand.XAF.Modules.Reactive.Logger";

        static ReactiveLoggerModule(){
            EnumProcessingHelper.RegisterEnum(typeof(RXAction),"Xpand.XAF.Modules.Reactive.Logger.RXAction");
            TraceSource=new ReactiveTraceSource(nameof(ReactiveLoggerModule));
        }
        public ReactiveLoggerModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public static ReactiveTraceSource TraceSource{ get; set; }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new TraceEventAppearanceRulesGenerator());
        }


        public override void Setup(ApplicationModulesManager manager){
            base.Setup(manager);
            manager.Connect().Subscribe(this);
        }
 
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            
            extenders.Add<IModelReactiveModules,IModelReactiveModuleLogger>();
            
        }
    }

}
