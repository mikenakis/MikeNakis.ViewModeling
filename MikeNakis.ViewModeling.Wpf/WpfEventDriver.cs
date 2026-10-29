namespace MikeNakis.ViewModeling.Wpf;

using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using MikeNakis.ViewModeling.Events;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using SysThread = System.Threading;
using Wpf = System.Windows;
using WpfThreading = System.Windows.Threading;

public sealed class WpfEventDriver : BaseEventDriver, Sys.IDisposable
{
	readonly LogicEntranceFactory logicEntranceFactory;
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly MyProxy myProxy;
	public override EventDriverProxy Proxy => myProxy;
	bool willNeedLogicIdleEvent;
	public int WpfIdleEventCount { get; private set; }

	public WpfEventDriver( LogicEntranceFactory logicEntranceFactory )
	{
		this.logicEntranceFactory = logicEntranceFactory;
		myProxy = new MyProxy( post );
		WpfPlatformHelpers.Dispatcher.Hooks.DispatcherInactive += onDispatcherInactive;
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		WpfPlatformHelpers.Dispatcher.Hooks.DispatcherInactive -= onDispatcherInactive;
		myProxy.Dispose();
		lifeGuard.Dispose();
	}

	public override void Defer( string operationName, OperationType operationType, Sys.Action procedure )
	{
		Assert( InThreadAssertion() ); //please use Post() instead.
		WpfPlatformHelpers.Post( operationName, () =>
			{
				Assert( InThreadAssertion() );
				using( logicEntranceFactory.Invoke( operationName, operationType ) )
					procedure.Invoke();
			} );
	}

	public void WillNeedLogicIdleEvent()
	{
		willNeedLogicIdleEvent = true;
	}

	public override bool InThreadAssertion()
	{
		Assert( SysThread.Thread.CurrentThread.ReferenceEquals( WpfPlatformHelpers.Dispatcher.Thread ) );
		return true;
	}

	public void ProcessMessageBurst()
	{
		Assert( InThreadAssertion() );

		//https://stackoverflow.com/a/11899439/773113
		WpfThreading.DispatcherFrame frame = new();
		WpfThreading.Dispatcher.CurrentDispatcher.BeginInvoke( WpfThreading.DispatcherPriority.Background, new WpfThreading.DispatcherOperationCallback( f =>
			{
				((WpfThreading.DispatcherFrame)f).Continue = false;
				return null;
			} ), frame );
		WpfThreading.Dispatcher.PushFrame( frame );

		//https://stackoverflow.com/a/4502200/773113
		if( False )
			WpfPlatformHelpers.Dispatcher.Invoke( WpfThreading.DispatcherPriority.Background, () => { } );
	}

	void post( string operationName, OperationType operationType, Sys.Action procedure )
	{
		WpfPlatformHelpers.Post( operationName, () =>
			{
				using( logicEntranceFactory.Invoke( operationName, operationType ) )
					procedure.Invoke();
			} );
	}

	void onDispatcherInactive( object? sender, Sys.EventArgs e )
	{
		if( Wpf.Application.Current == null ) //may happen under certain rare conditions if application startup fails.
			return;
		Assert( sender.ReferenceEquals( WpfPlatformHelpers.Dispatcher ) );
		WpfIdleEventCount++;
		if( willNeedLogicIdleEvent )
		{
			willNeedLogicIdleEvent = false;
			using( logicEntranceFactory.Invoke( "OnLogicIdle", OperationType.Medium ) ) //using 'medium' type here so as to refrain from causing yet another idle event
				OnLogicIdle();
			DotNetHelpers.PerformGarbageCollectionAndWait();
		}
	}

	sealed class MyProxy : BaseEventDriverProxy, Sys.IDisposable
	{
		readonly Sys.Action<string, OperationType, Sys.Action> poster;
		readonly LifeGuard lifeGuard = LifeGuard.Create();

		public MyProxy( Sys.Action<string, OperationType, Sys.Action> poster )
		{
			this.poster = poster;
		}

		public void Dispose()
		{
			Assert( lifeGuard.IsAliveAssertion() );
			lifeGuard.Dispose();
		}

		protected override bool OutOfThreadAssertion()
		{
			Assert( !SysThread.Thread.CurrentThread.ReferenceEquals( WpfPlatformHelpers.Dispatcher.Thread ) );
			return true;
		}

		public override void Post( string operationName, OperationType operationType, Sys.Action procedure )
		{
			Assert( OutOfThreadAssertion() );
			poster.Invoke( operationName, operationType, procedure );
		}
	}
}
