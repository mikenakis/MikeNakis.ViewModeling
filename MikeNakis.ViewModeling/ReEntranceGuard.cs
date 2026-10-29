namespace MikeNakis.ViewModeling;

using MikeNakis.Kit;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using SysDiag = System.Diagnostics;

public sealed class ReEntranceGuard<T> : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly MyProperty<T> property;

	public ReEntranceGuard( MyProperty<T> property )
	{
		this.property = property;
		if( property.ReEntranceCount > 50 )
			SysDiag.Debugger.Break();
		if( property.ReEntranceCount > 100 )
		{
			Breakpoint();
			return;
		}
		property.ReEntranceCount++;
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		property.ReEntranceCount--;
		lifeGuard.Dispose();
	}
}
