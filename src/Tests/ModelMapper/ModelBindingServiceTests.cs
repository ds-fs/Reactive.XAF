﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.DashboardWin;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.Scheduler;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Persistent.BaseImpl;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Threading;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using ListView = DevExpress.ExpressApp.ListView;
using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [NonParallelizable]
    public class ModelMapperBinderServiceTests:ModelMapperCommonTest{
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Bind_Only_NullAble_Properties_That_are_not_Null(string platformName){
            var platform = GetPlatform(platformName);
            var typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService();

            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RwInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RwString = "shouldNotChange"};

            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RwInteger.ShouldBe(100);
            stringValueTypeProperties.RwString.ShouldBe("shouldNotChange");
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Do_not_bind_Disable_mode_nodes(string platformName){
            var platform = GetPlatform(platformName);
            var typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService();
            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RwInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RwString = "shouldNotChange"};

            modelModelMap.NodeDisabled = true;
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RwString.ShouldBe("shouldNotChange");
            stringValueTypeProperties.RwInteger.ShouldBe(0);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Do_not_throw_if_target_object_properties_do_not_exist(string platformName){
            var platform = GetPlatform(platformName);
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService();
            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.Index = 100;
            var stringValueTypeProperties = new StringValueTypeProperties();

            modelModelMap.BindTo(stringValueTypeProperties);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Bind_all_public_nullable_type_properties(string platformName){
            var platform = GetPlatform(platformName);
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService();
            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RwInteger),100);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.NullAbleRwInteger),200);
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RwInteger.ShouldBe(100);
            stringValueTypeProperties.NullAbleRwInteger.ShouldBe(200);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Bind_all_public_rw_string_properties(string platformName){
            var platform = GetPlatform(platformName);
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService();
            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RwString),"test");
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RwString.ShouldBe("test");
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void Bind_all_public_rw_nested_properties(string platformName){
            var platform = GetPlatform(platformName);
            var typeToMap=typeof(ReferenceTypeProperties);
            InitializeMapperService();
            using var module = typeToMap.Extend<IModelListView>();
            using var application = DefaultModelMapperModule( platform, module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.GetNode(nameof(ReferenceTypeProperties.RStringValueTypeProperties)).SetValue(nameof(StringValueTypeProperties.RwString),"test");
            var referenceTypeProperties = new ReferenceTypeProperties();

            modelModelMap.BindTo(referenceTypeProperties);
            referenceTypeProperties.RStringValueTypeProperties.RwString.ShouldBe("test");
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [Ignore(NotImplemented)]
        public void Apply_AllMapper_Contexts(string platformName){
            
        }

        [XpandTest(LongTimeout)]
        [TestCase(nameof(Platform.Win))]
        [Ignore(NotImplemented)]
        public void Apply_Root_Map_After_mapper_contexts(string platformName){
            
        }

        [XpandTest(LongTimeout)]
        // [Ignore("fail on .NET8")]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.XafLayoutControl },new[]{typeof(XafLayoutControl)},new Type[0],1)]
        
        public async Task Bind_DetailView_Maps(string platformName,PredefinedMap[] predefinedMaps,Type[] controlTypes,Type[] extraModules,int boundTypes) {
            var platform = GetPlatform(platformName);
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService(platform);
            

            using var module = predefinedMaps.Extend();
            
            var moduleBases = extraModules.Select(type => {
                    var instance = type.CreateInstance();
                    return instance;
                })
                .Cast<ModuleBase>()
                .Concat(new[]{module})
                .ToArray();
            using var application = DefaultModelMapperModule( platform, moduleBases).Application;
            var controlBound = ModelBindingService.ControlBind.Replay();
            using (controlBound.Connect()){
                for (int i = 0; i < 2; i++){
                    var detailView = application.NewObjectView<DetailView>(typeof(MM));
                    detailView.CreateControls();
                    if (boundTypes>0){
                        await controlBound.Take((i+1)*boundTypes).Timeout(Timeout).ToTaskWithoutConfigureAwait();
                    }
                    detailView.Close();
                }
            }
        }

        [Test]
        [XpandTest(LongTimeout)]
        public async Task Bind_Subsequent_Views(){
            Assembly.LoadFile(typeof(XafGridView).Assembly.Location);
            InitializeMapperService(Platform.Win);
            using var module = PredefinedMap.GridView.Extend();
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            var bound = ModelBindingService.ControlBind.TakeFirst().SubscribeReplay();
            application.Logon();
            application.CreateObjectSpace();
            application.EditorFactory=new EditorsFactory();
            var listView = application.NewObjectView<ListView>(typeof(MM));
            listView.CreateControls();
            await bound;
                    
            bound = ModelBindingService.ControlBind.TakeFirst().SubscribeReplay();
            listView = application.NewObjectView<ListView>(typeof(MM));
            listView.CreateControls();
            await bound;
        }


        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.GridColumn , PredefinedMap.GridView},new[]{typeof(XafGridView),typeof(GridColumn),typeof(GridListEditor)},new Type[0],3)]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.BandedGridColumn , PredefinedMap.AdvBandedGridView},new[]{typeof(XafAdvBandedGridView),typeof(BandedGridColumn),typeof(GridListEditor)},new Type[0],3)]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.LayoutViewColumn , PredefinedMap.LayoutView},new[]{typeof(XafLayoutView),typeof(LayoutViewColumn),typeof(CustomGridListEditor)},new Type[0],3)]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.SplitContainerControl },new[]{typeof(SplitContainerControl)},new Type[0],1)]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.TreeListColumn , PredefinedMap.TreeList},new[]{typeof(TreeList),typeof(TreeListColumn),typeof(TreeListEditor)},new[]{typeof(TreeListEditorsModuleBase),typeof(TreeListEditorsWindowsFormsModule)},3)]
        [TestCase(nameof(Platform.Win),new[]{PredefinedMap.DashboardDesigner },new[]{typeof(DashboardDesigner)},new[]{typeof(DashboardsModule),typeof(DashboardsWindowsFormsModule)},0)]
        [TestCase(nameof(Platform.Win), new[]{PredefinedMap.PivotGridField, PredefinedMap.PivotGridControl},new[]{typeof(PivotGridControl), typeof(PivotGridListEditor)},new[]{typeof(PivotGridModule), typeof(PivotGridWindowsFormsModule)}, 3)]
        [TestCase(nameof(Platform.Win), new[]{PredefinedMap.ChartControl},new[]{typeof(ChartControl), typeof(ChartListEditor)},new[]{typeof(ChartModule), typeof(ChartWindowsFormsModule)},1)]
        [TestCase(nameof(Platform.Win), new[]{PredefinedMap.SchedulerControl},new[]{typeof(SchedulerControl), typeof(SchedulerListEditor)},new[]{typeof(SchedulerModuleBase), typeof(SchedulerWindowsFormsModule)}, 1)]
        [XpandTest(LongTimeout)]
        public async Task Bind_ListView_Maps(string platformName,PredefinedMap[] predefinedMaps,Type[] controlTypes,Type[] extraModules,int boundTypes){
            var platform = GetPlatform(platformName);
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService(platform);
            var predefinedMap = predefinedMaps.Last();

            using var module = predefinedMaps.Extend();
            using var application = DefaultModelMapperModule( platform, extraModules.Select(t => {
                        var instance = t.CreateInstance();
                        if (instance is DashboardsModule dashboardsModule){
                            dashboardsModule.DashboardDataType = typeof(DashboardData);
                        }

                        return instance;
                    })
                    .Cast<ModuleBase>()
                    .Concat(new[]{module})
                    .ToArray())
                .Application;
            application.Logon();
            application.CreateObjectSpace();
            MockListEditor(platform, controlTypes, application, predefinedMap,null);
            var controlBound = ModelBindingService.ControlBind.Replay();
            using (controlBound.Connect()){
                for (int i = 0; i < 2; i++){
                        
                    
                    var listView = application.NewObjectView<ListView>(typeof(MM));
                    listView.Model.EditorType = controlTypes.Last();
                    listView.Model.BandsLayout.Enable = predefinedMaps.Contains(PredefinedMap.AdvBandedGridView);

                    listView.CreateControls();
                    
                    if (boundTypes>0){
                        await controlBound.Take((boundTypes*(i+1))).WithTimeOut(Timeout);
                    }
                }
            }
        }
        [XpandTest()]
        [TestCase(nameof(Platform.Win))]
        public void Bind_PropertyEditor_Control(string platformName){
            var platform = GetPlatform(platformName);
            var maps = EnumsNET.Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsPropertyEditor() && map.Attribute<MapPlatformAttribute>().Platform == platform);
            foreach (var predefinedMap in maps){
                try{
                    InitializeMapperService(platform);
                    var controlType = predefinedMap.TypeToMap();
                    using (var module = predefinedMap.Extend()){
                        using (var application = DefaultModelMapperModule( platform, predefinedMap.Modules()
                                .Select(type => {
                                    var instance = type.CreateInstance();
                                    if (instance is DashboardsModule dashboardsModule){
                                        dashboardsModule.DashboardDataType = typeof(DashboardData);
                                    }

                                    return instance;
                                })
                                .Cast<ModuleBase>()
                                .Concat(new[]{module})
                                .ToArray())
                            .Application){
                            var controlBound = ModelBindingService.ControlBind.Replay();
                            using (controlBound.Connect()){
                                var modelPropertyEditor = ((IModelPropertyEditor) application.Model.GetNodeByPath(MMDetailViewTestItemNodePath));
                                var mapNode = modelPropertyEditor.GetNode(ViewItemService.PropertyEditorControlMapName);
                                var propertyEditorControlType = mapNode.ModelListItemType().ToTypeInfo();
                                var typeInfo = propertyEditorControlType.Descendants.First(info => info.Type.Name==predefinedMap.ModelTypeName());
                                mapNode.AddNode(typeInfo.Type);
                                application.MockDetailViewEditor( modelPropertyEditor, controlType.CreateInstance());

                                var detailView = application.NewObjectView<DetailView>(typeof(MM));
            
                                detailView.DelayedItemsInitialization = false;
                                detailView.CreateControls();

                                this.Await(async () => await controlBound.Take(1).WithTimeOut(TimeSpan.FromSeconds(10)) );
                                application.Dispose();
                            }
                            
                        }
                    }

                    Dispose();
                }
                catch (Exception e){
                    throw new Exception(predefinedMap.ToString(), e);
                }
            }
            

        }
        [XpandTest]
        [TestCase(typeof(ListView))]
        [TestCase(typeof(DetailView))]
        public async Task Bind_RepositoryItems(Type viewType){
            var boundTypes = 2;
            var controlTypes =new[]{typeof(XafGridView),typeof(GridColumn)};
            var predefinedMaps = new[]{PredefinedMap.RepositoryItem,PredefinedMap.RepositoryItemTextEdit };
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService(Platform.Win);
            using var module = predefinedMaps.Extend();
            using var application = DefaultModelMapperModule( Platform.Win, module).Application;
            var controlBound = ModelBindingService.ControlBind.Take(boundTypes).Replay();
            using (controlBound.Connect()){
                var modelPropertyEditor = ((IModelPropertyEditor) application.Model.GetNodeByPath(MMDetailViewTestItemNodePath));
                var controlInstance = new StringEdit();
                var repositoryItemTextEdit = controlInstance.Properties;
                if (viewType == typeof(ListView)){
                    MockListEditor(Platform.Win, controlTypes, application, PredefinedMap.GridView,repositoryItemTextEdit);
                }
                else{
                    application.MockDetailViewEditor( modelPropertyEditor, controlInstance);
                }

                var objectView = application.NewObjectView(viewType, typeof(MM));
                var values = ConfigureModelRepositories(objectView);
                objectView.DelayedItemsInitialization = false;
                objectView.CreateControls();

                await controlBound.WithTimeOut(Timeout);

                Debug.Assert(repositoryItemTextEdit != null, nameof(repositoryItemTextEdit) + " != null");
                repositoryItemTextEdit.CharacterCasing.ShouldBe(values.concreteTypePropertyValue);
                repositoryItemTextEdit.AccessibleDescription.ShouldBe(values.basePropertyValue);
            }
        }

        private static (CharacterCasing concreteTypePropertyValue, string basePropertyValue) ConfigureModelRepositories(ObjectView objectView){
            var concreteTypePropertyValue = CharacterCasing.Upper;
            var basePropertyValue = "AccessibleDescription";
            var repositoryItemsNode = objectView.Model.CommonMemberViewItems(nameof(MM.Test)).Cast<IModelNode>().First().GetNode(ViewItemService.RepositoryItemsMapName);

            var modelListItemType = repositoryItemsNode.ModelListItemType();
            
            var descendants = modelListItemType.ToTypeInfo().Descendants.ToArray();
            var repositoryItemModelType = descendants.First().Type;
            var baseRepoNode = repositoryItemsNode.AddNode(repositoryItemModelType, "BaseRepo");
            (baseRepoNode is IModelModelMap).ShouldBeTrue();
            baseRepoNode.SetValue(nameof(RepositoryItem.AccessibleDescription), basePropertyValue);
            var repositoryTextItemModelType = descendants.Last();
            var textEditNode = repositoryItemsNode.AddNode(repositoryTextItemModelType);
            textEditNode.SetValue(nameof(RepositoryItemTextEdit.CharacterCasing), concreteTypePropertyValue);
            return (concreteTypePropertyValue, basePropertyValue);
        }


        private static void MockListEditor(Platform platform, Type[] controlTypes, XafApplication application,
            PredefinedMap predefinedMap, RepositoryItemTextEdit repositoryItemTextEdit){
            application.MockListEditor((view, _, collectionSource) => {
                ListEditor listEditor;
                if (new[]{PredefinedMap.PivotGridControl,PredefinedMap.ChartControl,PredefinedMap.SchedulerControl,PredefinedMap.TreeList, }.Any(map => map==predefinedMap)){
                    listEditor =
                        (ListEditor) Activator.CreateInstance(controlTypes.Last(), view);
                }
                else if (new[]{PredefinedMap.DashboardDesigner,PredefinedMap.SplitContainerControl}.Any(map => map == predefinedMap)){
                    return application.ListEditorMock(view).Object;
                }
                else
                    listEditor = platform == Platform.Win
                        ? (ListEditor) new CustomGridListEditor(view, controlTypes.First(),
                            controlTypes.Skip(1).First(),repositoryItemTextEdit)
                        : throw new NotImplementedException();

                if (listEditor is IComplexListEditor complexListEditor){
                    complexListEditor.Setup(collectionSource, application);
                }

                return listEditor;
            });
        }
    }

}