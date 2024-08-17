﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static IModelListView FindModelListView(this XafApplication application, System.Type objectType) => (IModelListView) application
            .Model.Views[application.FindListViewId(objectType)];
    }
}