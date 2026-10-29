namespace MikeNakis.ViewModeling.Events;

using MikeNakis.Kit;
using Sys = System;

///<summary>Creates timers.</summary>
public abstract class TimerManager
{
	public Platform Platform { get; }

	protected TimerManager( Platform platform )
	{
		Platform = platform;
	}

	public abstract Clock Clock { get; }
	public abstract OneOffTimer NewOneOffTimer( string operationName, OperationType operationType, Sys.TimeSpan delay );

	public RepeatingTimer NewRepeatingTimer( string operationName, OperationType operationType, Sys.TimeSpan interval ) //
		=> new( this, operationName, operationType, interval );

	public RepeatingTimer NewRepeatingTimer( string operationName, OperationType operationType, Sys.TimeSpan interval, Sys.Action handler )
	{
		RepeatingTimer repeatingTimer = NewRepeatingTimer( operationName, operationType, interval );
		repeatingTimer.SetHandler( handler );
		return repeatingTimer;
	}

	public OneOffTimer NewOneOffTimer( string operationName, OperationType operationType, Sys.TimeSpan delay, Sys.Action handler )
	{
		OneOffTimer oneOffTimer = NewOneOffTimer( operationName, operationType, delay );
		oneOffTimer.SetHandler( handler );
		return oneOffTimer;
	}

	public void After( string operationName, OperationType operationType, Sys.TimeSpan interval, Sys.Action handler )
	{
		OneOffTimer oneOffTimer = NewOneOffTimer( operationName, operationType, interval );
		Platform.OnShutDown += onShutDown;
		oneOffTimer.SetHandler( () =>
			{
				oneOffTimer.Dispose();
				Platform.OnShutDown -= onShutDown;
				handler.Invoke();
			} );

		void onShutDown() => oneOffTimer.Dispose();
	}
}
