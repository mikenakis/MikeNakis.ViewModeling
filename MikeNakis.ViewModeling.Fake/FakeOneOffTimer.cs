namespace MikeNakis.ViewModeling.Fake;

using MikeNakis.Kit.Extensions;
using MikeNakis.ViewModeling;
using Sys = System;

public class FakeOneOffTimer : OneOffTimer, Sys.IComparable<FakeOneOffTimer>
{
	static int idSeed;

	readonly int id = idSeed++;
	readonly FakeTimerManager fakeTimerManager;
	internal Sys.DateTime TimeToFire { get; }

	public FakeOneOffTimer( FakeTimerManager fakeTimerManager, Sys.DateTime timeToFire )
	{
		this.fakeTimerManager = fakeTimerManager;
		TimeToFire = timeToFire;
	}

	internal void Trigger()
	{
		OnTick();
	}

	public int CompareTo( FakeOneOffTimer? other )
	{
		if( this.ReferenceEquals( other ) )
			return 0;
		if( other == null )
			return -1;
		int d = TimeToFire.CompareTo( other.TimeToFire );
		if( d != 0 )
			return d;
		//make sure that two timers with identical time-to-fire are not considered equal.
		//(Otherwise the second one will vanish when they are both added to a set.)
		return id.CompareTo( other.id );
	}

	protected override void OnDispose()
	{
		fakeTimerManager.Remove( this );
		base.OnDispose();
	}
}
