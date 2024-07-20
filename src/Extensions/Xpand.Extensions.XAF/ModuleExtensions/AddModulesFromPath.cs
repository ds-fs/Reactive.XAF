﻿using System;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.AppDomainExtensions;

namespace Xpand.Extensions.XAF.ModuleExtensions{
    public static partial class ModuleBaseExtensions{
        public static void AddModulesFromPath(this ModuleBase module,string pattern){
            var moduleTypes = Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), pattern)
                .Select(System.Reflection.Assembly.LoadFile)
                .SelectMany(assembly => assembly.GetTypes()).Where(type =>!type.IsAbstract&& typeof(ModuleBase).IsAssignableFrom(type));
            module.RequiredModuleTypes.AddRange(moduleTypes);
        }
    }

}