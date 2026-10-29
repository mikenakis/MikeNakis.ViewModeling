namespace MikeNakis.ViewModeling;

using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using SpecializedCollections = System.Collections.Specialized;
using Sys = System;
using SysObjModel = System.Collections.ObjectModel;
using SysReflect = System.Reflection;

public static class ObservableCollectionObserver
{
	public static ObservableCollectionObserver<T> Create<T>( SysObjModel.ObservableCollection<T> observableCollection, Sys.Action callback, string? itemChangedEventName )
	{
		return new ObservableCollectionObserver<T>( observableCollection, callback, itemChangedEventName );
	}
}

//TODO: implement IDisposable
public class ObservableCollectionObserver<T> //: IDisposable
{
	//private readonly DisposableGuard disposableGuard = DisposableGuard.Create();
	readonly Sys.Action callback;
	readonly string? itemChangedEventName;
	int suppressionCount;
	readonly SysReflect.EventInfo? eventInfo;
	readonly Sys.Delegate? onItemChangedDelegate;

	internal ObservableCollectionObserver( SysObjModel.ObservableCollection<T> observableCollection, Sys.Action callback, string? itemChangedEventName )
	{
		this.callback = callback;
		this.itemChangedEventName = itemChangedEventName;
		observableCollection.CollectionChanged += onCollectionChanged;
		if( itemChangedEventName != null )
		{
			eventInfo = typeof( T ).GetEvent( itemChangedEventName ).OrThrow();
			onItemChangedDelegate = new Sys.EventHandler( ( _, _ ) => invokeCallback() );
			foreach( T item in observableCollection )
				addEventHandler( item );
		}
	}

	void addEventHandler( T item )
	{
		Assert( eventInfo != null );
		eventInfo!.AddEventHandler( item, onItemChangedDelegate );
	}

	void removeEventHandler( T item )
	{
		Assert( eventInfo != null );
		eventInfo!.RemoveEventHandler( item, onItemChangedDelegate );
	}

	// public void Dispose()
	// {
	// 	Assert( disposableGuard.DisposedAssertion() );
	// 	observableCollection.CollectionChanged -= onCollectionChanged;
	// 	foreach( T item in observableCollection )
	// 		removeEventHandler.Invoke( item, onItemChanged );
	// 	disposableGuard.Dispose();
	// }

	public Sys.IDisposable NewSuppression()
	{
		suppressionCount++;
		return new MakeshiftDisposable( () => suppressionCount-- );
	}

	void onCollectionChanged( object? sender, SpecializedCollections.NotifyCollectionChangedEventArgs e )
	{
		switch( e.Action )
		{
			case SpecializedCollections.NotifyCollectionChangedAction.Add:
				break;
			case SpecializedCollections.NotifyCollectionChangedAction.Remove:
				break;
			case SpecializedCollections.NotifyCollectionChangedAction.Replace:
				break;
			case SpecializedCollections.NotifyCollectionChangedAction.Move:
				break;
			case SpecializedCollections.NotifyCollectionChangedAction.Reset:
				break;
			default:
				throw Failure();
		}
		if( itemChangedEventName != null )
		{
			if( e.OldItems != null )
				foreach( T item in e.OldItems )
					removeEventHandler( item );
			if( e.NewItems != null )
				foreach( T item in e.NewItems )
					addEventHandler( item );
		}
		invokeCallback();
	}

	void invokeCallback()
	{
		if( suppressionCount == 0 )
			callback.Invoke();
	}
}
