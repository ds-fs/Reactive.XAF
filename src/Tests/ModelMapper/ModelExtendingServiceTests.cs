﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.DashboardWin;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Utils;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using EnumsNET;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;


namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [NonParallelizable]
    public class ModelMapperExtenderServiceTests : ModelMapperCommonTest{
        [XpandTest]
        [TestCase(typeof(TestModelMapper),nameof(Platform.Win))]
        [TestCase(typeof(RootType),nameof(Platform.Win))]
        public void ExtendModel_Any_Type(Type typeToMap,string platformName) {
            var platform = GetPlatform(platformName);
            InitializeMapperService(platform);

            var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            AssertExtendedListViewModel(typeToMap, application, MMListViewNodePath);
        }

        [Test]
        [XpandTest]
        public void Get_PredefinedModelNode(){
            InitializeMapperService(Platform.Win);

            using var module = PredefinedMap.GridView.Extend();
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            application.Model.Views.OfType<IModelListView>().First().GetNode(PredefinedMap.GridView).ShouldNotBeNull();
        }

        [Test]
        [XpandTest]
        public void Get_PredefinedViewItemMergedModelNode(){
            InitializeMapperService(Platform.Win);

            using var module = new[]{PredefinedMap.RepositoryItem, PredefinedMap.RepositoryFieldPicker, PredefinedMap.RepositoryItemBlobBaseEdit}.Extend();
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            var modelColumn = application.Model.GetNodeByPath(MMListViewTestItemNodePath);
            var repositoryItemModel = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryItem);
            repositoryItemModel.SetValue("Name","Base");
            repositoryItemModel.SetValue("AccessibleName","AccessibleNameBase");
            var repositoryFieldPickerModel = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryFieldPicker);
            repositoryFieldPickerModel.SetValue("Name","Derived");
            repositoryFieldPickerModel.SetValue("AccessibleName","AccessibleName");
            var repositoryItemBlobBaseEdit = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryItemBlobBaseEdit);
            repositoryItemBlobBaseEdit.SetValue("Name","NotMerged");

            var finalNode = modelColumn.GetRepositoryItemNode(PredefinedMap.RepositoryFieldPicker);
            finalNode.GetValue<string>("Name").ShouldBe("Derived");
            finalNode.GetValue<string>("AccessibleName").ShouldBe("AccessibleName");

            modelColumn.GetRepositoryItemNode(PredefinedMap.RepositoryItemBlobBaseEdit).GetValue<string>("Name").ShouldBe("NotMerged");
        }

        [Test]
        [XpandTest]
        public void Customize_PredefineMaps_TargetInterface(){
            InitializeMapperService(Platform.Win);

            using var module = PredefinedMap.DashboardDesigner.Extend(null, configuration => {
                configuration.TargetInterfaceTypes.Clear();
                configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
            });
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            application.Model.Options.GetNode(PredefinedMap.DashboardDesigner).ShouldNotBeNull();
        }

        [XpandTest(LongTimeout)]
        [TestCase(PredefinedMap.GridColumn, typeof(GridColumn),nameof(Platform.Win),MMListViewNodePath+"/Columns/Test")]
        [TestCase(PredefinedMap.GridView, typeof(GridView),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.SchedulerControl, typeof(SchedulerControl),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.PivotGridControl, typeof(PivotGridControl),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.ChartControl, typeof(ChartControl),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.PivotGridField, typeof(PivotGridField),nameof(Platform.Win),MMListViewNodePath+"/Columns/Test")]
        [TestCase(PredefinedMap.LayoutViewColumn, typeof(LayoutViewColumn),nameof(Platform.Win),MMListViewNodePath+"/Columns/Test")]
        [TestCase(PredefinedMap.LayoutView, typeof(LayoutView),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.BandedGridColumn, typeof(BandedGridColumn),nameof(Platform.Win),MMListViewNodePath+"/Columns/Test")]
        [TestCase(PredefinedMap.AdvBandedGridView, typeof(AdvBandedGridView),nameof(Platform.Win),MMListViewNodePath)]
        [TestCase(PredefinedMap.TreeList, typeof(TreeList),nameof(Platform.Win),MMListViewNodePath+",NavigationItems")]
        [TestCase(PredefinedMap.TreeListColumn, typeof(TreeListColumn),nameof(Platform.Win),MMListViewTestItemNodePath)]
        [TestCase(PredefinedMap.XafLayoutControl, typeof(XafLayoutControl),nameof(Platform.Win),MMDetailViewNodePath)]
        [TestCase(PredefinedMap.SplitContainerControl, typeof(SplitContainerControl),nameof(Platform.Win),MMListViewNodePath+"/SplitLayout")]
        [TestCase(PredefinedMap.DashboardDesigner, typeof(DashboardDesigner),nameof(Platform.Win),MMDetailViewTestItemNodePath)]
        public void ExtendModel_Predefined_Type(PredefinedMap configuration,Type typeToMap,string platformName,string nodePath){

            var platform = GetPlatform(platformName);
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService(platform);
            using var module = configuration.Extend();
            using var application = DefaultModelMapperModule( platform, module).Application;
            AssertExtendedListViewModel(typeToMap, application,nodePath);
        }

        private void AssertExtendedListViewModel(Type typeToMap, XafApplication application,string nodePath){
            var mapName = typeToMap.ModelTypeName();
            foreach (var s in nodePath.Split(',')){
                var modelNode = application.Model.GetNodeByPath(s);
                modelNode.GetNode(typeToMap.Name).ShouldNotBeNull();
            }

            var typeInfos = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.ToArray();
            var typeInfo = typeInfos.FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe(mapName);
            var defaultContext =((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(typeToMap.Name);
            modelMapper.ShouldNotBeNull();
        }

        [TestCaseSource(nameof(RepositoryItems))]
        [XpandTest()]
        public void Extend_PredefinedRepositoryItems(PredefinedMap predefinedMap){
            InitializeMapperService( predefinedMap.Platform());
            Extend_Predefined_ViewItem(predefinedMap, ViewItemService.RepositoryItemsMapName,true);
        }

        [XpandTest()]
        [Test]
        public void Extend_LayoutControlGroup(){
            
            var layoutControlGroup = PredefinedMap.LayoutControlGroup;
            var platform = layoutControlGroup.Platform();
            InitializeMapperService( platform);
            using var module = layoutControlGroup.Extend();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelViewLayout = application.Model.BOModel.GetClass(typeof(MM)).DefaultDetailView.Layout;
            modelViewLayout.Nodes().First().ShouldBeAssignableTo<IModelModelMappersContextDependency>();
        }

        protected static object[] RepositoryItems(){
            return Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsRepositoryItem()&&map.Platform()==Platform.Win).Cast<object>().ToArray();
        }

        protected static object[] WinPropertyEditorControls(){
            return Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsPropertyEditor()&&map.Platform()==Platform.Win).Cast<object>().ToArray();
        }


        [XpandTest()]
        [TestCaseSource(nameof(WinPropertyEditorControls))]
        public void Extend_Predefined_PropertyEditorControls(PredefinedMap predefinedMap){
            InitializeMapperService( predefinedMap.Platform());
            Extend_Predefined_ViewItem(predefinedMap, ViewItemService.PropertyEditorControlMapName);
        }

        private void Extend_Predefined_ViewItem(PredefinedMap predefinedMap, string mapPropertyName,
            bool checkListViewColumns = false) {
            using var module = predefinedMap.Extend();
            var connectableObservable = TypeMappingService.MappedTypes.Replay();
            connectableObservable.Connect();
            using var application = DefaultModelMapperModule(predefinedMap.Platform(), module).Application;
            var typeToMap = predefinedMap.TypeToMap();

            var modelNode = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
            modelNode.GetNode(mapPropertyName).ShouldNotBeNull();
            if (checkListViewColumns){
                modelNode = application.Model.GetNodeByPath(MMListViewTestItemNodePath);
                modelNode.GetNode(mapPropertyName).ShouldNotBeNull();
            }

            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants
                .FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe(typeToMap.ModelTypeName());

            var defaultContext =
                ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(
                    ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(predefinedMap.DisplayName());
            modelMapper.ShouldNotBeNull();
        }


        [XpandTest]
        [TestCase(PredefinedMap.ChartControlRadarDiagram, typeof(RadarDiagram),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlPolarDiagram, typeof(PolarDiagram),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlXYDiagram2D, typeof(XYDiagram2D),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlXYDiagram, typeof(XYDiagram),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlSwiftPlotDiagram, typeof(SwiftPlotDiagram),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlGanttDiagram, typeof(GanttDiagram),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlFunnelDiagram3D, typeof(FunnelDiagram3D),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlDiagram3D, typeof(Diagram3D),nameof(Platform.Win))]
        [TestCase(PredefinedMap.ChartControlSimpleDiagram3D, typeof(SimpleDiagram3D),nameof(Platform.Win))]
        public void ExtendModel_PredefinedChartDiagram(PredefinedMap configuration,Type typeToMap,string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService(platform);

            using var module = PredefinedMap.ChartControl.Extend();
            configuration.Extend(module);
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelNode = modelListView.GetNode(PredefinedMap.ChartControl);
            modelNode= modelNode.GetNode("Diagrams");

            var diagramType = modelNode.ModelListItemType();
            var targetType = diagramType.Assembly.GetType(configuration.ModelTypeName());
            diagramType.IsAssignableFrom(targetType).ShouldBeTrue();
        }

        [Test]
        [XpandTest]
        public void ExtendModel_All_PredefinedChartDiagram(){
            Assembly.LoadFile(typeof(ChartControl).Assembly.Location);
            Assembly.LoadFile(typeof(Diagram).Assembly.Location);
            InitializeMapperService(Platform.Win);

            using var module = PredefinedMap.ChartControl.Extend();
            var diagrams = Enums.GetMembers<PredefinedMap>().Where(member =>
                member.Name.StartsWith(PredefinedMap.ChartControl.ToString()) &&
                member.Value != PredefinedMap.ChartControl&&member.Value != PredefinedMap.ChartControlDiagram).Select(member => member.Value).ToArray();
            diagrams.Extend(module);
            using (DefaultModelMapperModule( Platform.Win, module).Application){
            }
        }

        [XpandTest(LongTimeout)]
        [TestCase(nameof(Platform.Win))]
        public void ExtendModel_All_Predefined_Maps(string platformName){
            
            var platform = GetPlatform(platformName);
            InitializeMapperService(platform);
            var values = Enums.GetValues<PredefinedMap>().Where(map =>
                    map != PredefinedMap.None && map.Platform() == platform)
                .ToArray();

            using var module = values.ToArray().Extend();
            using (DefaultModelMapperModule( platform, module).Application){

            }
        }

        [Test]
        [XpandTest]
        public void Extend_Existing_PredefinedMap(){
            InitializeMapperService(Platform.Win);
            using var module = new[]{PredefinedMap.PivotGridControl, PredefinedMap.GridView}.Extend();
            module.ApplicationModulesManager
                .TakeFirst()
                .SelectMany(t => t.manager.ExtendMap(PredefinedMap.GridView))
                .Subscribe(t => {
                    t.extenders.Add(t.targetInterface,typeof(IModelPredefinedMapExtension));
                });
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelNode = modelListView.GetNode(nameof(GridView));

            (modelNode is IModelPredefinedMapExtension).ShouldBeTrue();
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win),PredefinedMap.RichEditControl)]
        [TestCase(nameof(Platform.Win),PredefinedMap.RepositoryItem)]
        public void Extend_Existing_ViewItemMap(string platformName,PredefinedMap predefinedMap){
            var platform = GetPlatform(platformName);
            var mapPropertyName=predefinedMap.IsRepositoryItem()?ViewItemService.RepositoryItemsMapName:ViewItemService.PropertyEditorControlMapName;
            InitializeMapperService(platform);
            using var module = new[]{predefinedMap}.Extend();
            module.ApplicationModulesManager
                .TakeFirst()
                .SelectMany(t => t.manager.ExtendMap(predefinedMap))
                .Subscribe(t => {
                    t.extenders.Add(t.targetInterface,typeof(IModelPredefinedMapExtension));
                });
            using var application = DefaultModelMapperModule( platform, module).Application;
            var nodeByPath = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
            nodeByPath.ShouldNotBeNull();
            
            var listNode = nodeByPath.GetNode(mapPropertyName);
            listNode.ShouldNotBeNull();
            var baseType = listNode.ModelListItemType();
            var modelType = baseType.ToTypeInfo().Descendants.First().Type;

            (listNode.AddNode(modelType) is IModelPredefinedMapExtension).ShouldBeTrue();
        }

        [Test]
        [XpandTest]
        public void Extend_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService();

            using var module = new ModelMapperTestModule();
            typeToMap1.Extend<IModelListView>(module);
            typeToMap2.Extend<IModelColumn>(module);

            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            var appearanceCell = application.Model.GetNodeByPath($@"{MMListViewNodePath}/Columns/Test/{nameof(TestModelMapperCommonType2)}/{nameof(TestModelMapperCommonType2.AppearanceCell)}");
            appearanceCell.ShouldNotBeNull();
            appearanceCell.GetNodeByPath($"{nameof(AppearanceObjectEx.TextOptions)}");
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void ModelMapperContexts(string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelModelMappers = ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.First();
            modelModelMappers.Id().ShouldBe(ModelMapperContextNodeGenerator.Default);
            modelModelMappers.First().Id().ShouldBe(typeToMap.Name);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Container_ModelMapperContexts(string platformName){
            var platform = GetPlatform(platformName);
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.Name;
            var modelMappersNode = modelListView.GetNode(mapName).GetNode(TypeMappingService.ModelMappersNodeName);
            modelMappersNode.ShouldNotBeNull();
            modelMappersNode.Index.ShouldBe(0);
            var defaultContext = modelMappersNode.GetNode(ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelModelMappers =
                defaultContext.GetValue<IModelModelMappers>(nameof(IModelMapperContextContainer.Context));
            modelModelMappers.ShouldNotBeNull();
            modelModelMappers.ShouldBe(
                ((IModelApplicationModelMapper) modelModelMappers.Application).ModelMapper.MapperContexts[
                    ModelMapperContextNodeGenerator.Default]);
        }

        [XpandTest]
        [TestCase("Parent.AllowEdit=?", true,false,null)]
        [TestCase("Parent.AllowEdit=?", false,true,null)]
        [TestCase(VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType, true,typeof(WinColumnsListEditor),"Parent.")]
        [TestCase("Parent."+nameof(IModelListView.EditorType)+"=?", true,typeof(GridListEditor),null)]
        [TestCase(null, true,null,null)]
        public void Container_Visibility(object leftOperand, bool visibility,object rightOperand,string path){
            
            var visibilityCriteria = $"{CriteriaOperator.Parse($"{leftOperand}", rightOperand)}";
            if (leftOperand is VisibilityCriteriaLeftOperand visibilityCriteriaLeftOperand){
                var propertyExistsCriteria = VisibilityCriteriaLeftOperand.PropertyExists.GetVisibilityCriteria("EditorType","Parent");
                visibilityCriteria = visibilityCriteriaLeftOperand.GetVisibilityCriteria(rightOperand,path);
                visibilityCriteria = $"{propertyExistsCriteria} and {visibilityCriteria}";
            }

            var platform = Platform.Win;
            InitializeMapperService();
            var typeToMap = typeof(TestModelMapper);

            using var module = typeToMap.Extend(configuration => {
                configuration.VisibilityCriteria = visibilityCriteria;
                configuration.TargetInterfaceTypes.Add(typeof(IModelListView));
                if (leftOperand is VisibilityCriteriaLeftOperand){
                    configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
                }
            });
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelMapName = typeToMap.Name;
            modelListView.IsPropertyVisible(modelMapName).ShouldBe(visibility);
            if (leftOperand is VisibilityCriteriaLeftOperand){
                application.Model.Options.IsPropertyVisible(modelMapName).ShouldBe(false);
            }
        }
    }
}
