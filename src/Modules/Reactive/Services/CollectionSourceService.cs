﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class CollectionSourceService {

        public static IObservable<CollectionSourceBase> WhenCriteriaApplied(this CollectionSourceBase collectionSourceBase)
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.CriteriaApplied))
                .TakeUntil(collectionSourceBase.WhenDisposed()).To(collectionSourceBase);
        public static IObservable<ProxyCollection> WhenProxyCollectionChanged(this CollectionSourceBase collectionSourceBase) 
            => collectionSourceBase.Collection is not ProxyCollection proxyCollection ? Observable.Empty<ProxyCollection>()
                : proxyCollection.ProcessEvent(nameof(ProxyCollection.ListChanged)).TakeUntil(collectionSourceBase.WhenDisposed())
                    .Select(pattern => pattern).Cast<ProxyCollection>().TraceRX(_ => collectionSourceBase.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionReloaded<T>(this T collection) where T:CollectionSourceBase 
            => collection.ProcessEvent(nameof(CollectionSourceBase.CollectionReloaded))
                .TakeUntil(collection.WhenDisposed())
                .TraceRX(c => c.ObjectTypeInfo.Type.FullName);

        public static IObservable<T> WhenCollectionChanged<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.CollectionChanged))
                .TakeUntil(collectionSourceBase.WhenDisposed());
        
        public static IObservable<FetchObjectsEventArgs> WhenFetchObjects<T>(this T collection) where T:DynamicCollection
            => collection.ProcessEvent<FetchObjectsEventArgs>(nameof(DynamicCollection.FetchObjects));
        
        public static IObservable<DynamicCollection> WhenLoaded(this DynamicCollection collection) 
            => collection.ProcessEvent(nameof(DynamicCollection.Loaded)).To(collection).TakeWhileInclusive(dynamicCollection => !dynamicCollection.IsDisposed);

        public static IObservable<T> WhenDisposed<T>(this T collectionSourceBase) where T:CollectionSourceBase 
            => collectionSourceBase.ProcessEvent(nameof(CollectionSourceBase.Disposed));

        public static NonPersistentPropertyCollectionSource NewSource(this CreateCustomPropertyCollectionSourceEventArgs e,object masterObject=null) 
            => new(e.ObjectSpace, e.MasterObjectType, masterObject??e.MasterObject, e.MemberInfo, e.DataAccessMode,e.Mode);
    }
    public class NonPersistentPropertyCollectionSource : PropertyCollectionSource{
        readonly Subject<GenericEventArgs<IEnumerable<object>>> _datasourceSubject=new();
        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceDataAccessMode dataAccessMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, dataAccessMode, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, bool isServerMode, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, isServerMode, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo, CollectionSourceMode mode) : base(objectSpace, masterObjectType, masterObject, memberInfo, mode){
        }

        public NonPersistentPropertyCollectionSource(IObjectSpace objectSpace, Type masterObjectType, object masterObject, IMemberInfo memberInfo) : base(objectSpace, masterObjectType, masterObject, memberInfo){
        }

        public IObservable<GenericEventArgs<IEnumerable<object>>> Datasource => _datasourceSubject.AsObservable();

        protected override object CreateCollection(){
            var handledEventArgs = new GenericEventArgs<IEnumerable<object>>();
            _datasourceSubject.OnNext(handledEventArgs);
            return handledEventArgs.Handled ? handledEventArgs.Instance : base.CreateCollection();
        }
    }


    public class ReactiveCollection<T>(IObjectSpace objectSpace)
        : DynamicCollection(objectSpace, typeof(T), null, null, false), IList<T> {
        public IEnumerator<T> GetEnumerator() => ((IEnumerable) this).GetEnumerator().Cast<T>();

        public void Add(T item) => base.Add(item);

        public bool Contains(T item) => base.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

        public bool Remove(T item) {
            if (Objects.IndexOf(item)>-1) {
                base.Remove(item);
                return true;
            }

            return false;
        }

        public int IndexOf(T item) => base.IndexOf(item);

        public void Insert(int index, T item) => base.Insert(index, item);

        T IList<T>.this[int index] {
            get => (T) base[index];
            set => base[index]=value;
        }
    }

}