namespace MikeNakis.ViewModeling.Fake;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using MikeNakis.ViewModeling;
using MikeNakis.ViewModeling.Events;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public class FakeTimerManager : TimerManager
{
	readonly SortedSet<FakeOneOffTimer> timers = new();
	readonly FakePlatform fakePlatform;
	readonly FakeClock fakeClock = new();

	public FakeTimerManager( FakePlatform fakePlatform )
			: base( fakePlatform )
	{
		this.fakePlatform = fakePlatform;
	}

	public override Clock Clock => fakeClock;

	public override OneOffTimer NewOneOffTimer( string operationName, OperationType operationType, Sys.TimeSpan delay )
	{
		Sys.DateTime timeToFire = fakeClock.GetUniversalTime() + delay;
		FakeOneOffTimer timer = new( this, timeToFire );
		bool ok = timers.Add( timer );
		Assert( ok );
		return timer;
	}

	internal void Advance()
	{
		while( true )
		{
			FakeOneOffTimer? timer = timers.FirstOrDefault();
			if( timer == null )
				break;
			if( timer.TimeToFire >= fakeClock.GetUniversalTime() )
				break;
			timer.Trigger();
			Assert( !timers.Contains( timer ) ); //the timer should have called us to remove it.
		}
	}

	internal void Remove( FakeOneOffTimer timer )
	{
		timers.DoRemove( timer );
	}

	public void SetCurrentTime( Sys.DateTime value )
	{
		fakeClock.SetCurrentTime( value );
		Advance();
		fakePlatform.ProcessMessageBurst();
	}
}
