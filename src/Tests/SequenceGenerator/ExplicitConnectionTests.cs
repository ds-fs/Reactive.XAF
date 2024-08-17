﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DevExpress.Xpo;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Xpo.SessionExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    public class ExplicitConnectionTests:SequenceGeneratorTestsCommonTests{
        [XpandTest()]
        [Test()][Apartment(ApartmentState.MTA)]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void Throws_if_another_connection_exists_with_same_DataLayer() {
            using var application = SequenceGeneratorModule().Application;
            using var defaultDataLayer = NewSimpleDataLayer(application);
            var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer);
            var testObject = new TestObject(explicitUnitOfWork);
            explicitUnitOfWork.FlushChanges();
            explicitUnitOfWork.GetObjectByKey<TestObject>(testObject.Oid).ShouldNotBeNull();

            var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer);

                    
            new TestObject2(explicitUnitOfWork1);

            Should.Throw<InvalidOperationException>(() => explicitUnitOfWork1.FlushChanges(), SequenceGeneratorService.ParallelTransactionExceptionMessage);

            explicitUnitOfWork.Close();
            explicitUnitOfWork1.Close();
        }

        [Test][Ignore("Passed in the past, maybe some side effect of xpo")][Apartment(ApartmentState.MTA)]
        public void LocksOnlyModifiedTablesWhenDifferentDataLayer() {
            using var application = SequenceGeneratorModule().Application;
            var defaultDataLayer1 = NewSimpleDataLayer(application);
            var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer1);
            var testObject = new TestObject(explicitUnitOfWork);
            explicitUnitOfWork.FlushChanges();

            var defaultDataLayer2 = NewSimpleDataLayer(application);
            var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer2);
            var testObject2 = new TestObject2(explicitUnitOfWork1);
            explicitUnitOfWork1.FlushChanges();
            
            Should.Throw<ShouldCompleteInException>(() => Should.CompleteIn(
                () => explicitUnitOfWork.GetObjectByKey<TestObject2>(testObject2.Oid).ShouldNotBeNull(),
                TimeSpan.FromSeconds(1)));
            Should.Throw<ShouldCompleteInException>(() => Should.CompleteIn(
                () => explicitUnitOfWork1.GetObjectByKey<TestObject>(testObject.Oid).ShouldNotBeNull(),
                TimeSpan.FromSeconds(1)));
            explicitUnitOfWork.Close();
            defaultDataLayer1.Dispose();
            explicitUnitOfWork1.Close();
            defaultDataLayer2.Dispose();
        }

        [Test]
        [XpandTest()]
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")][Apartment(ApartmentState.MTA)]
        public void UnLocks_current_Record_When_Commit_Changes() {
            using var application = SequenceGeneratorModule().Application;
            var defaultDataLayer1 =  application.ObjectSpaceProvider.CreateObjectSpace().SequenceGeneratorDataLayer();
                
            var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer1);
            
            explicitUnitOfWork.SetSequence<TestObject>( o => o.SequentialNumber);
            explicitUnitOfWork.FlushChanges();

            var defaultDataLayer2 = NewSimpleDataLayer(application);
            var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer2);

            explicitUnitOfWork.CommitChanges();
            explicitUnitOfWork1.GetSequenceStorage(typeof(TestObject));
            
            explicitUnitOfWork.Close();
            defaultDataLayer1.Dispose();
            explicitUnitOfWork1.Close();
            defaultDataLayer2.Dispose();
        }
        
    }
}