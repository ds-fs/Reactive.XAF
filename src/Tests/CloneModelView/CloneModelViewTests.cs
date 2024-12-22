﻿using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.CloneModelView.Tests.BOModel;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.CloneModelView.Tests{
	[NonParallelizable]
	public class CloneModelViewTests : BaseTest{
		[XpandTest]
		[TestCase(CloneViewType.LookupListView)]
		[TestCase(CloneViewType.ListView)]
		[TestCase(CloneViewType.DetailView)]
		public void Clone_Model_View(CloneViewType cloneViewType){
			var cloneViewId = $"{nameof(Clone_Model_View)}_{cloneViewType}";

			var application = DefaultCloneModelViewModule(info => {
				var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId);
				info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
			}).Application;
			((bool) application.GetPropertyValue("EnableModelCache")).ShouldBe(false);

			var modelView = application.Model.Views[cloneViewId];
			modelView.ShouldNotBeNull();
			modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
			modelView.Id.ShouldBe(cloneViewId);
			application.Dispose();
		}

		[XpandTest]
		[TestCase(CloneViewType.DetailView)]
		public void Keep_ModelGenerators(CloneViewType cloneViewType){
			var cloneViewId = $"{nameof(Keep_ModelGenerators)}_{cloneViewType}";
			var cloneModelViewModule = new CloneModelViewModule();
			cloneModelViewModule.RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
			using var application = DefaultCloneModelViewModule(cloneModelViewModule, info => {
					var typeInfo = info.FindTypeInfo(typeof(CMV));
					typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, cloneViewId));
					typeInfo.AddAttribute(new ModelMergedDifferencesAttribute(cloneViewId, $"CMV_{cloneViewType}"));
				}, Platform.Win)
				.Application;
			((IModelObjectViewMergedDifferences) application.Model.Views[cloneViewId]).MergedDifferences.Count.ShouldBe(1);
		}


		[Test()]
		[XpandTest]
		public void Clone_multiple_Model_Views(){
			var cloneViewId = $"{nameof(Clone_multiple_Model_Views)}_";
			var cloneViewTypes = Enum.GetValues(typeof(CloneViewType)).Cast<CloneViewType>();
			var application = DefaultCloneModelViewModule(info => {
				foreach (var cloneViewType in cloneViewTypes){
					var cloneModelViewAttribute =
						new CloneModelViewAttribute(cloneViewType, $"{cloneViewId}{cloneViewType}");
					info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
				}
			}).Application;
			foreach (var cloneViewType in cloneViewTypes){
				var viewId = $"{cloneViewId}{cloneViewType}";
				var modelView = application.Model.Views[viewId];
				modelView.ShouldNotBeNull();
				modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
				modelView.Id.ShouldBe(viewId);
			}

			application.Dispose();
		}


		[TestCase(CloneViewType.LookupListView)]
		[TestCase(CloneViewType.ListView)]
		[TestCase(CloneViewType.DetailView)]
		[XpandTest]
		public void Clone_Model_View_and_make_it_default(CloneViewType cloneViewType){
			var cloneViewId = $"{nameof(Clone_Model_View_and_make_it_default)}_{cloneViewType}";

			var application = DefaultCloneModelViewModule(info => {
				var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId, true);
				info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
			}).Application;
			var modelView = application.Model.Views[cloneViewId].AsObjectView;

			((IModelView) modelView.ModelClass.GetPropertyValue($"Default{cloneViewType}")).Id
				.ShouldBe(cloneViewId);
			application.Dispose();
		}


		[XpandTest]
		[TestCase(CloneViewType.LookupListView)]
		[TestCase(CloneViewType.ListView)]
		public void Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType){
			var cloneViewId = $"{nameof(Clone_Model_ListView_and_change_its_detailview)}_";
			var listViewId = $"{cloneViewId}{cloneViewType}";
			var detailViewId = $"{cloneViewType}DetailView";
			var application = DefaultCloneModelViewModule(info => {
				var typeInfo = info.FindTypeInfo(typeof(CMV));
				typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
				typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, listViewId)
					{DetailView = detailViewId});
			}).Application;
			var modelListView = (IModelListView) application.Model.Views[listViewId];
			modelListView.DetailView.Id.ShouldBe(detailViewId);
			application.Dispose();
		}

		[XpandTest]
		[Test()]
		public void WHen(){
			using var application = Platform.Win.NewApplication<CloneModelViewModule>();
			var testObserver = application.WhenApplicationModulesManager()
				.SelectMany(manager => manager.WhenGeneratingModelNodes(modelApplication => modelApplication.Views))
				.Do(_ => {})
				.Test();
			application.AddModule<TestCloneModelViewModule>();
			application.Model.Views.Count.ShouldBeGreaterThan(0);
			testObserver.ItemCount.ShouldBe(1);
		}

		private static CloneModelViewModule DefaultCloneModelViewModule(CloneModelViewModule cloneModelViewModule,
			Action<ITypesInfo> customizeTypesInfo, Platform platform){
			var application = platform.NewApplication<CloneModelViewModule>();
			application.Modules.Add(new ReactiveModule());
			application.WhenApplicationModulesManager().WhenCustomizeTypesInfo().TakeFirst(info => {
					customizeTypesInfo(info.TypesInfo);
					return true;
				})
				.Subscribe();


			cloneModelViewModule.RequiredModuleTypes.Add(typeof(ReactiveModule));
			return (CloneModelViewModule) application.AddModule(cloneModelViewModule,  additionalExportedTypes:typeof(CMV));
		}

		private static CloneModelViewModule DefaultCloneModelViewModule(Action<ITypesInfo> customizeTypesInfo,
			Platform platform = Platform.Win){
			var cloneModelViewModule = new CloneModelViewModule();
			return DefaultCloneModelViewModule(cloneModelViewModule, customizeTypesInfo, platform);
		}
	}
}