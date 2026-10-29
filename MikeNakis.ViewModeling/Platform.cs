namespace MikeNakis.ViewModeling;

using MikeNakis.Kit;
using MikeNakis.ViewModeling.Events;
using Sys = System;
using SysDiag = System.Diagnostics;

///<summary>Makes various kinds of environment-specific functionality available to the Application Logic.</summary>
///<remarks>This abstract base class exists for the purpose of having two different implementations, one for testing and
///one for operating in a GUI application.</remarks>
public abstract class Platform
{
	///<summary>Raised when the <see cref="Platform"/> shuts down</summary>
	public abstract event Sys.Action OnShutDown;

	///<summary>Creates timers, and provides the <see cref="Clock"/> that they go by.</summary>
	public abstract TimerManager TimerManager { get; }

	///<summary>Shows message boxes, modal dialogs and file dialogs.</summary>
	public abstract Prompts Prompts { get; }

	///<summary>Creates a <see cref="Command"/> on a given view model, which invokes the handler within a logic entrance of the given <see cref="OperationType"/>.</summary>
	///<remarks>The command is enabled when <paramref name="isEnabled"/> returns true; the GUI implementation also makes it usable by WPF bindings.</remarks>
	public abstract Command NewCommand( ViewModel viewModel, OperationType operationType, Sys.Action handler, Sys.Func<bool> isEnabled );

	///<summary>The <see cref="EventDriver"/> of the current thread.</summary>
	public abstract EventDriver EventDriver { get; }

	///<summary>Runs a producer that either returns <see cref="Result{T}"/> or throws, and passes its result to the consumer.</summary>
	///<remarks>If the producer throws, the consumer receives a failure <see cref="Result{T}"/> with an <see cref="ExceptionError"/>.</remarks>
	public void RunResultTask<T>( string producerOperationName, Sys.Func<Result<T>> producer, string consumerOperationName, Sys.Action<Result<T>> consumer, bool asynchronously = true ) //
	{
		Result<T> nonThrowingProducer()
		{
			if( SysDiag.Debugger.IsAttached && !KitHelpers.FailureTesting.Value )
				return producer.Invoke();
			try
			{
				return producer.Invoke();
			}
			catch( Sys.Exception exception )
			{
				return new ExceptionError( exception );
			}
		}
		RunNonThrowingTask( producerOperationName, nonThrowingProducer, consumerOperationName, consumer, asynchronously );
		return;

	}

	///<summary>Runs a producer that either returns a value or throws, and passes its result to the consumer.</summary>
	///<remarks>If the producer throws, the consumer receives a failure <see cref="Result{T}"/> with an <see cref="ExceptionError"/>.</remarks>
	public void RunTask<T>( string producerOperationName, Sys.Func<T> producer, string consumerOperationName, Sys.Action<Result<T>> consumer, bool asynchronously = true ) //
	{
		Result<T> resultProducer()
		{
			T value = producer.Invoke();
			return Result.Success( value );
		}
		RunResultTask( producerOperationName, resultProducer, consumerOperationName, consumer, asynchronously );
	}

	///<summary>Runs a producer that never throws, and passes its result to the consumer.</summary>
	protected abstract void RunNonThrowingTask<T>( string producerOperationName, Sys.Func<Result<T>> nonThrowingProducer, string consumerOperationName, Sys.Action<Result<T>> consumer, bool asynchronously );

	public abstract class LogicEntrance : Sys.IDisposable
	{
		public abstract void Dispose();
	}

	///<summary>Creates an object marking the entry of control into the Application Logic.</summary>
	///<remarks>A <see cref="OperationType.Minor"/> operation needs no logic entrance; a <see cref="OperationType.Medium"/> one should get a logic entrance;
	///a <see cref="OperationType.Major"/> also causes a logic-idle event afterwards.
	///The GUI implementation shows the busy cursor while any logic entrance is active.</remarks>
	public abstract LogicEntrance NewLogicEntrance( string operationName, OperationType operationType );

	///<summary>Provides access to the clipboard.</summary>
	public abstract Clipboard Clipboard { get; }
}
