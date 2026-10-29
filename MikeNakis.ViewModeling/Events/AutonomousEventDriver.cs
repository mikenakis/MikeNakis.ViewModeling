namespace MikeNakis.ViewModeling.Events;

using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using ConcurrentCollections = System.Collections.Concurrent;
using Sys = System;
using SysThread = System.Threading;

/// <summary> An <see cref="EventDriver" /> with its own message queue.</summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
//NO, this type should not be disposable just because ConcurrentCollections.BlockingCollection is disposable.
//    besides, ConcurrentCollections.BlockingCollection is disposable for no good reason: it has no use for disposability,
//    it is only disposable in order to accommodate derived classes that may need to be disposable.
public sealed class AutonomousEventDriver : BaseEventDriver
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
	readonly SysThread.Thread constructionThread;
	readonly ConcurrentCollections.BlockingCollection<Sys.Action> queue = new();
	bool terminationRequested;

	public override EventDriverProxy Proxy { get; }

	public AutonomousEventDriver()
	{
		constructionThread = SysThread.Thread.CurrentThread;
		Proxy = new MyProxy( constructionThread, queue.Add );
	}

	void requestTermination()
	{
		terminationRequested = true;
	}

	public void ProcessMessageBurst()
	{
		Assert( !terminationRequested ); //we assert against this here because the caller has no way of handling it.
		OnLogicIdle();
		while( processMessageBurst() )
			OnLogicIdle();
		Assert( !terminationRequested ); //we assert against this here because the caller has no way of handling it.
	}

	public void Run()
	{
		for( terminationRequested = false; !terminationRequested; )
		{
			Sys.Action procedure = queue.Take(); //this blocks waiting for a procedure to be placed in the queue.
			dispatch( procedure );
			processMessageBurst();
		}
		terminationRequested = false;
	}

	bool processMessageBurst()
	{
		bool any = false;
		while( queue.TryTake( out Sys.Action? procedure ) )
		{
			dispatch( procedure );
			any = true;
		}
		return any;
	}

	static void dispatch( Sys.Action procedure )
	{
		procedure.Invoke();
	}

	public void Stop()
	{
		Assert( !terminationRequested ); //we are already stopped, or we have not even started.
		queue.Add( requestTermination );
	}

	public override void Defer( string operationName, OperationType operationType, Sys.Action procedure )
	{
		Assert( InThreadAssertion() ); //please use Post() instead.
		queue.Add( procedure );
	}

	public override bool InThreadAssertion()
	{
		Assert( SysThread.Thread.CurrentThread.ReferenceEquals( constructionThread ) );
		return true;
	}

	sealed class MyProxy : BaseEventDriverProxy
	{
		readonly SysThread.Thread constructionThread;
		readonly Sys.Action<Sys.Action> poster;

		public MyProxy( SysThread.Thread constructionThread, Sys.Action<Sys.Action> poster )
		{
			this.constructionThread = constructionThread;
			this.poster = poster;
		}

		protected override bool OutOfThreadAssertion()
		{
			Assert( !SysThread.Thread.CurrentThread.ReferenceEquals( constructionThread ) );
			return true;
		}

		public override void Post( string operationName, OperationType operationType, Sys.Action procedure )
		{
			Assert( OutOfThreadAssertion() );
			poster.Invoke( procedure );
		}
	}
}
