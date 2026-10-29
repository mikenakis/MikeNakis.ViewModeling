namespace MikeNakis.ViewModeling;

using MikeNakis.Kit;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public abstract class OneOffTimer : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly OverridableGuard onDisposeOverridableGuard = new( nameof( OnDispose ) );
	Sys.Action? handler;

	protected OneOffTimer()
	{ }

	public void SetHandler( Sys.Action handler )
	{
		Assert( handler != null );
		Assert( this.handler == null );
		this.handler = handler;
	}

	protected void OnTick()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		//logger.Debug( "Tick!" );
		Assert( handler != null ); //the handler must be set immediately after construction!
		handler.Invoke();
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		Sys.GC.SuppressFinalize( this );
		onDisposeOverridableGuard.Invoke( OnDispose );
		lifeGuard.Dispose();
	}

	protected virtual void OnDispose()
	{
		onDisposeOverridableGuard.InvocationComplete();
	}
}
