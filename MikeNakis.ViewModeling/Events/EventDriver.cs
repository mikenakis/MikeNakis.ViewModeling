namespace MikeNakis.ViewModeling.Events;

using Sys = System;

///<summary>Represents an event-driven system.</summary>
public interface EventDriver
{
	///<summary>Asserts that the current thread is the <see cref="EventDriver" />'s thread.</summary>
	bool InThreadAssertion();

	/// <summary>Sets a handler to be invoked on the next logic-idle event.</summary>
	/// <remarks>May only be invoked from within the <see cref="EventDriver" />'s thread.</remarks>
	/// <remarks>
	/// The logic-idle event occurs far less frequently than the gui-idle event:
	/// <br />- The gui-idle event occurs after every single gui-input or gui-timer event;
	/// <br />- The logic-idle event only occurs if as a result of the gui events the logic was given some work to do.
	/// </remarks>
	/// <remarks>See also: <seealso cref="Defer" /></remarks>
	void OnNextLogicIdle( Sys.Action procedure );

	///<summary>
	///Obtains the <see cref="EventDriverProxy" /> of this <see cref="EventDriver" />, which can be used from outside the <see cref="EventDriver" />'s thread to invoke methods within the <see cref="EventDriver" />'s thread.
	///</summary>
	///<remarks>See also: <seealso cref="EventDriverProxy" /> </remarks>
	EventDriverProxy Proxy { get; }

	///<summary>Sets a handler to be invoked at the earliest time possible, that is, on the next gui idle event. (Not the next logic idle event.)</summary>
	///<remarks>May only be invoked from within the <see cref="EventDriver" />'s thread. </remarks>
	///<remarks>See also: <seealso cref="OnNextLogicIdle" /></remarks>
	void Defer( string operationName, OperationType operationType, Sys.Action procedure );
}
