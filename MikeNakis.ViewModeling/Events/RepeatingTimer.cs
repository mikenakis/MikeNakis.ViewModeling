namespace MikeNakis.ViewModeling.Events;

using MikeNakis.Kit;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

///<summary>A repeating timer based on the one-off timers offered by a <see cref="TimerManager"/>.</summary>
public sealed class RepeatingTimer : Sys.IDisposable
{
	readonly StatefulLifeGuard lifeGuard = StatefulLifeGuard.Create();
	readonly TimerManager timerManager;
	readonly string operationName;
	readonly OperationType operationType;
	readonly Sys.TimeSpan interval;
	Sys.Action? handler;
	OneOffTimer? oneOffTimer;

	internal RepeatingTimer( TimerManager timerManager, string operationName, OperationType operationType, Sys.TimeSpan interval )
	{
		Assert( interval.CompareTo( Sys.TimeSpan.Zero ) > 0 );
		this.timerManager = timerManager;
		this.operationName = operationName;
		this.operationType = operationType;
		this.interval = interval;
		setNextOneOffTimer();
	}

	void setNextOneOffTimer()
	{
		Assert( oneOffTimer == null );
		Assert( lifeGuard.IsAlive );
		oneOffTimer = timerManager.NewOneOffTimer( operationName, operationType, interval, oneOffTimerHandler );
	}

	void oneOffTimerHandler()
	{
		Assert( handler != null ); //the handler should have been set immediately after construction!
		if( !lifeGuard.IsAlive )
			return;
		oneOffTimer!.Dispose();
		oneOffTimer = null;
		handler.Invoke();
		//I am not sure why I used to do this:
		//timer_manager.Platform.EventDriver.Defer( "Set Next One-Off Timer", OperationType.Minor, () => if( life_guard.IsAlive() ) set_next_one_off_timer() );
		if( !lifeGuard.IsAlive )
			return;
		setNextOneOffTimer();
	}

	public void SetHandler( Sys.Action handler )
	{
		Assert( handler != null );
		Assert( this.handler == null );
		this.handler = handler;
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		oneOffTimer?.Dispose();
		lifeGuard.Dispose();
	}
}
