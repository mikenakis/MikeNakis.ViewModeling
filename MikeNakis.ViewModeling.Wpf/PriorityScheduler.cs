namespace MikeNakis.ViewModeling.Wpf;

using System.Collections.Generic;
using ConcurrentCollections = System.Collections.Concurrent;
using Sys = System;
using SysTask = System.Threading.Tasks;
using SysThread = System.Threading;

// PEARL: If you use `Task.Factory.StartNew()` to launch a parallel task, the task will run in a thread with normal priority.
//        When you have threads running in normal priority, they adversely affect the responsiveness of the gui.
//        Unfortunately, I do not remember precisely wat happens, but it is something along the lines of preventing gui timers from working, preventing the
//        idle event from occurring, preventing the application from repainting, etc.
//        In order to have a fully responsive gui while running parallel tasks, these tasks must have a low priority.
//        Apparently, there is no way to launch tasks in any priority other than normal without writing a considerable amount of code, such as this class.
//        The code is taken from https://stackoverflow.com/a/9056702/773113
// PEARL: The Microsoft test framework runs tests inside an AppDomain, and when the tests end, the AppDomain is shut down.
//        When an AppDomain shuts down, any running threads in it are forcefully aborted.
//        In DotNet a thread is forcefully aborted by invoking thread.Abort(), which causes a ThreadAbortException to be thrown within the thread.
//        The ThreadAbortException is a very special kind of exception which GETS AUTOMAGICALLY RE-THROWN AT THE END OF EVERY BLOCK THAT CATCHES IT.
//        (If you hadn't learned something new today, now you have.)
// PEARL: Allegedly, you can use Thread.ResetAbort() to prevent ThreadAbortException from being automagically re-thrown. However, during AppDomain
//        shutdown, ResetAbort() takes MALICIOUS INACTION: it does nothing, and yields no error.
// PEARL: Despite configuring Visual Studio to NOT break when ThreadAbortException is user-unhandled, Visual Studio WILL NONETHELESS KEEP BREAKING
//        ON THIS EXCEPTION during AppDomain shutdown. And since this exception cannot be suppressed, this yields a very unworkable situation where every
//        single time the tests end, Visual Studio breaks.
//        The solution to this problem is the following piece of configuration magic:
//           Go to Tools -> Options... -> Debugging -> General
//           Uncheck the "Break when exceptions cross AppDomain or managed/native boundaries" option.
//        Alternatively, we could make this class disposable so that it can perform an orderly shutdown, so that no threads are left running, but we would
//        also have to do this in every other situation where we use threads, and #AinTNoBodyGotNoTimeFoDat.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public class PriorityScheduler : SysTask.TaskScheduler
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
	public static readonly PriorityScheduler AboveNormal = new( SysThread.ThreadPriority.AboveNormal );
	public static readonly PriorityScheduler BelowNormal = new( SysThread.ThreadPriority.BelowNormal );
	public static readonly PriorityScheduler Lowest = new( SysThread.ThreadPriority.Lowest );

	readonly ConcurrentCollections.BlockingCollection<SysTask.Task> tasks = new();
	SysThread.Thread[]? threads;
	readonly SysThread.ThreadPriority priority;
	readonly int maximumConcurrencyLevel = Sys.Environment.ProcessorCount;

	public PriorityScheduler( SysThread.ThreadPriority priority )
	{
		this.priority = priority;
	}

	public override int MaximumConcurrencyLevel => maximumConcurrencyLevel;

	protected override IEnumerable<SysTask.Task> GetScheduledTasks()
	{
		return tasks;
	}

	protected override void QueueTask( SysTask.Task task )
	{
		tasks.Add( task );
		if( threads == null )
			allocateAndRunThreads();
	}

	void allocateAndRunThreads()
	{
		threads = new SysThread.Thread[maximumConcurrencyLevel];
		for( int i = 0; i < threads.Length; i++ )
		{
			threads[i] = new SysThread.Thread( threadProcedure );
			threads[i].Name = $"PriorityScheduler: {i}";
			threads[i].Priority = priority;
			threads[i].IsBackground = true; //this means that this thread will be automagically terminated when the application exits.
			threads[i].Start();
		}
	}

	void threadProcedure()
	{
		foreach( SysTask.Task t in tasks.GetConsumingEnumerable() )
			TryExecuteTask( t );
	}

	protected override bool TryExecuteTaskInline( SysTask.Task task, bool taskWasPreviouslyQueued )
	{
		return false; // we might not want to execute task that should schedule as high or low priority inline
	}
}
