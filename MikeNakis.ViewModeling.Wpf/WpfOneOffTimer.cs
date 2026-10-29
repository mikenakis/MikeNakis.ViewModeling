namespace MikeNakis.ViewModeling.Wpf;

using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using WpfThreading = System.Windows.Threading;

public sealed class WpfOneOffTimer : OneOffTimer
{
	readonly Platform platform;
	readonly string operationName;
	readonly OperationType operationType;

	//PEARL: The parameterless constructor of the WPF timer uses DispatcherPriority.Background!
	//       We fix this here by explicitly setting the priority to Normal.
	readonly WpfThreading.DispatcherTimer wpfTimer = new( WpfThreading.DispatcherPriority.Normal );

	bool fired;

	public WpfOneOffTimer( Platform platform, Sys.TimeSpan delay, string operationName, OperationType operationType )
	{
		this.platform = platform;
		this.operationName = operationName;
		this.operationType = operationType;
		wpfTimer.Interval = delay;
		wpfTimer.Tick += onWpfTimerTick;
		wpfTimer.Start();
	}

	void onWpfTimerTick( object? sender, Sys.EventArgs e )
	{
		Assert( sender == wpfTimer );
		Assert( wpfTimer.IsEnabled );
		Assert( !fired );
		fired = true;
		wpfTimer.Stop();
		using( platform.NewLogicEntrance( operationName, operationType ) )
			OnTick();
	}

	protected override void OnDispose()
	{
		Assert( fired == !wpfTimer.IsEnabled );
		if( wpfTimer.IsEnabled )
			wpfTimer.Stop();
		base.OnDispose();
	}
}
