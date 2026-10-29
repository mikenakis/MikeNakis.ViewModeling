namespace MikeNakis.ViewModeling.Wpf;

using MikeNakis.Kit;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using WpfInput = System.Windows.Input;

public sealed class WpfCursorConservator : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly WpfInput.Cursor oldCursor;

	public WpfCursorConservator( WpfInput.Cursor temporaryCursor )
	{
		oldCursor = WpfInput.Mouse.OverrideCursor;
		WpfInput.Mouse.OverrideCursor = temporaryCursor;
	}

	public void /*IDisposable.*/ Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		WpfInput.Mouse.OverrideCursor = oldCursor;
		lifeGuard.Dispose();
	}
}
