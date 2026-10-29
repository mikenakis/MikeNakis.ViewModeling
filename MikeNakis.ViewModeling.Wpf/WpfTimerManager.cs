namespace MikeNakis.ViewModeling.Wpf;

using MikeNakis.Kit;
using MikeNakis.ViewModeling.Events;
using Sys = System;

public class WpfTimerManager : TimerManager
{
	public WpfTimerManager( Platform platform )
			: base( platform )
	{ }

	public override Clock Clock => DotNetClock.Instance;

	public override OneOffTimer NewOneOffTimer( string operationName, OperationType operationType, Sys.TimeSpan delay ) //
		=> new WpfOneOffTimer( Platform, delay, operationName, operationType );
}
