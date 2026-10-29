namespace MikeNakis.ViewModeling.Events;

using System.Collections.Generic;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public abstract class BaseEventDriver : EventDriver
{
	readonly MutableList<Sys.Action> proceduresForNextIdle = new();

	protected BaseEventDriver()
	{ }

	protected void OnLogicIdle()
	{
		IEnumerable<Sys.Action> procedures = proceduresForNextIdle.AsReadOnlyList.Collect();
		proceduresForNextIdle.Clear();
		foreach( Sys.Action procedure in procedures )
			procedure.Invoke();
	}

	//TODO: see if this can be replaced with `Defer()`.
	public void OnNextLogicIdle( Sys.Action procedure )
	{
		Assert( InThreadAssertion() );
		proceduresForNextIdle.Add( procedure );
	}

	public abstract bool InThreadAssertion();

	public abstract EventDriverProxy Proxy { get; }

	public abstract void Defer( string operationName, OperationType operationType, Sys.Action procedure );
}
