namespace MikeNakis.ViewModeling.Wpf;

using System.Linq;
using MikeNakis.Kit;
using MikeNakis.ViewModeling.Events;
using MikeNakis.ViewModeling.Localization;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using SysTask = System.Threading.Tasks;
using SysThread = System.Threading;
using Wpf = System.Windows;
using WpfInput = System.Windows.Input;

public delegate Wpf.Window DialogWindowViewFactory( ValidatedViewModel dialogViewModel );

public delegate Sys.IDisposable LogicEntranceFactory( string operationName, OperationType operationType );

public sealed class WpfPlatform : Platform, Sys.IDisposable
{
	event Sys.Action? shutDown;
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly TimerManager timerManager;
	readonly Prompts prompts;
	readonly WpfEventDriver wpfEventDriver;
	public override EventDriver EventDriver => wpfEventDriver;
	int logicReEntranceCount; //used for counting major entrances so as to know when to switch to and from the busy cursor.
	WpfCursorConservator? wpfCursorConservator;
	readonly WpfClipboard wpfClipboard;

	public WpfPlatform( DialogWindowViewFactory viewFactory, Sys.Func<Wording, string?> translationDictionary )
	{
		timerManager = new WpfTimerManager( this );
		Translator translator = new( translationDictionary, static w => Log.Warn( w.Message ) );
		prompts = new WpfPrompts( this, viewFactory, translator );
		wpfEventDriver = new WpfEventDriver( NewLogicEntrance );
		wpfClipboard = new WpfClipboard();
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		wpfEventDriver.Dispose();
		shutDown?.Invoke();
		lifeGuard.Dispose();
	}

	public override event Sys.Action OnShutDown
	{
		add
		{
			Assert( lifeGuard.IsAliveAssertion() );
			shutDown += value;
		}
		remove
		{
			Assert( lifeGuard.IsAliveAssertion() );
			shutDown -= value;
		}
	}

	public override TimerManager TimerManager
	{
		get
		{
			Assert( lifeGuard.IsAliveAssertion() );
			return timerManager;
		}
	}

	public override Prompts Prompts
	{
		get
		{
			Assert( lifeGuard.IsAliveAssertion() );
			return prompts;
		}
	}

	public override Command NewCommand( ViewModel viewModel, OperationType operationType, Sys.Action handler, Sys.Func<bool> enabledFunction )
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return new WpfCommand( viewModel, operationType, handler, enabledFunction );
	}

	protected override void RunNonThrowingTask<T>( string producerOperationName, Sys.Func<Result<T>> nonThrowingProducer, string consumerOperationName, Sys.Action<Result<T>> consumer, bool asynchronously )
	{
		Assert( EventDriver.InThreadAssertion() );
		if( !asynchronously )
		{
			Result<T> result = nonThrowingProducer.Invoke();
			consumer.Invoke( result );
		}
		else
		{
			SysTask.Task task = SysTask.Task.Factory.StartNew( () =>
			{
				Result<T> result = nonThrowingProducer.Invoke();
				EventDriver.Proxy.Post( consumerOperationName, OperationType.Major, () => consumer.Invoke( result ) );
			}, SysThread.CancellationToken.None, SysTask.TaskCreationOptions.None, PriorityScheduler.Lowest );
			Assert( task != null );
		}
	}

	sealed class DummyLogicEntrance : LogicEntrance
	{
		public static readonly DummyLogicEntrance Instance = new();

		DummyLogicEntrance()
		{
		}

		public override void Dispose() { }
	}

	public override LogicEntrance NewLogicEntrance( string operationName, OperationType operationType )
	{
		Assert( wpfEventDriver.InThreadAssertion() );
		return operationType switch
		{
			OperationType.Minor => DummyLogicEntrance.Instance,
			OperationType.Medium => newLogicEntrance( operationName, false ),
			OperationType.Major => newLogicEntrance( operationName, true ),
			_ => throw Failure()
		};

		WpfLogicEntrance newLogicEntrance( string operationName, bool willNeedIdle )
		{
			if( willNeedIdle )
				wpfEventDriver.WillNeedLogicIdleEvent();
			return new WpfLogicEntrance( this, operationName );
		}
	}

	public override Clipboard Clipboard => wpfClipboard;

	sealed class WpfLogicEntrance : LogicEntrance
	{
		readonly WpfPlatform wpfPlatform;
		readonly string operationName;

		public WpfLogicEntrance( WpfPlatform wpfPlatform, string operationName )
		{
			Assert( wpfPlatform.wpfEventDriver.InThreadAssertion() );
			this.wpfPlatform = wpfPlatform;
			this.operationName = operationName;
			Log.Debug( $"{indentation} -> {operationName}" );
			Assert( wpfPlatform.wpfCursorConservator == null == (wpfPlatform.logicReEntranceCount == 0) );
			if( wpfPlatform.logicReEntranceCount == 0 )
				wpfPlatform.wpfCursorConservator = new WpfCursorConservator( WpfInput.Cursors.Wait );
			wpfPlatform.logicReEntranceCount++;
		}

		public override void Dispose()
		{
			Assert( wpfPlatform.wpfCursorConservator != null );
			wpfPlatform.logicReEntranceCount--;
			if( wpfPlatform.logicReEntranceCount == 0 )
			{
				wpfPlatform.wpfCursorConservator!.Dispose();
				wpfPlatform.wpfCursorConservator = null;
			}
			Log.Debug( $"{indentation} <- {operationName}" );
		}

		string indentation => string.Concat( Enumerable.Repeat( "    ", wpfPlatform.logicReEntranceCount ) );
	}
}
