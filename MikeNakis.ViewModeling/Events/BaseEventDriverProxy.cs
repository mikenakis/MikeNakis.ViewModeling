namespace MikeNakis.ViewModeling.Events;

using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public abstract class BaseEventDriverProxy : EventDriverProxy
{
	protected abstract bool OutOfThreadAssertion();

	public abstract void Post( string operationName, OperationType operationType, Sys.Action procedure );

	public void Call( string operationName, OperationType operationType, Sys.Action procedure )
	{
		Assert( OutOfThreadAssertion() );
		using( Sys.Threading.ManualResetEvent manualResetEvent = new( false ) )
		{
			Post( operationName, operationType, () =>
			{
				procedure.Invoke();
				manualResetEvent.Set();
			} );
			manualResetEvent.WaitOne();
		}
	}

	public T Call<T>( string operationName, OperationType operationType, Sys.Func<T> function )
	{
		Assert( OutOfThreadAssertion() );
		T? result = default;
		Call( operationName, operationType, () => { result = function.Invoke(); } );
		return result!;
	}
}
