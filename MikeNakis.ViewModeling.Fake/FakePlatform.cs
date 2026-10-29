namespace MikeNakis.ViewModeling.Fake;

using MikeNakis.Kit;
using MikeNakis.ViewModeling;
using MikeNakis.ViewModeling.Events;
using Sys = System;

public sealed class FakePlatform : Platform
{
	public FakeTimerManager FakeTimerManager { get; }
	readonly AutonomousEventDriver eventDriver = new();
	public FakePrompts FakePrompts { get; } = new();
	public override event Sys.Action? OnShutDown;
	public override TimerManager TimerManager => FakeTimerManager;
	public override Prompts Prompts => FakePrompts;
	public override EventDriver EventDriver => eventDriver;
	readonly FakeClipboard fakeClipboard = new();

	public FakePlatform()
	{
		FakeTimerManager = new FakeTimerManager( this );
	}

	public override Command NewCommand( ViewModel viewModel, OperationType operationType, Sys.Action handler, Sys.Func<bool> isEnabled )
	{
		return new Command( viewModel, operationType, handler, isEnabled );
	}

	public void ProcessMessageBurst()
	{
		FakeTimerManager.Advance();
		eventDriver.ProcessMessageBurst();
	}

	public void Dispose() //This is not used. It exists simply in order to access the ShutDown event, to avoid a warning.
	{
		OnShutDown?.Invoke();
	}

	protected override void RunNonThrowingTask<T>( string producerOperationName, Sys.Func<Result<T>> nonThrowingProducer, string consumerOperationName, Sys.Action<Result<T>> consumer, bool asynchronously )
	{
		Result<T> result = nonThrowingProducer.Invoke();
		consumer.Invoke( result );
	}

	sealed class FakeLogicEntrance : LogicEntrance
	{
		public static readonly FakeLogicEntrance Instance = new();

		FakeLogicEntrance()
		{
		}

		public override void Dispose() { }
	}

	public override LogicEntrance NewLogicEntrance( string operationName, OperationType operationType )
	{
		return FakeLogicEntrance.Instance;
	}

	public override Clipboard Clipboard => fakeClipboard;
}
