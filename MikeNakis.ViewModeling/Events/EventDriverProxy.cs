namespace MikeNakis.ViewModeling.Events;

using Sys = System;

///<summary>Exposes methods that can be invoked from outside of the <see cref="EventDriver" />'s thread to run methods inside the <see cref="EventDriver" />'s thread.) </summary>
///<remarks>See also: <seealso cref="EventDriver" /></remarks>
public interface EventDriverProxy
{
	///<summary>Posts an operation to the <see cref="EventDriver" />. </summary>
	///<remarks>May only be invoked from outside of the <see cref="EventDriver" />'s thread. </remarks>
	///<remarks>See also: <seealso cref="Call" /></remarks>
	///<remarks>See also: <seealso cref="Call{T}" /></remarks>
	///<param name="operationName"> The name of the operation, for logging/debugging purposes. </param>
	///<param name="operationType"> The <see cref="OperationType" />, controls whether a logic-idle event should be triggered after this operation is performed. </param>
	///<param name="procedure"> The <see cref="Sys.Action" /> to invoke. </param>
	void Post( string operationName, OperationType operationType, Sys.Action procedure );

	///<summary>Posts an operation to the <see cref="EventDriver" /> and waits until the operation completes. </summary>
	///<remarks>May only be invoked from outside of the <see cref="EventDriver" />'s thread. </remarks>
	///<remarks>See also: <seealso cref="Post" /></remarks>
	///<remarks>See also: <seealso cref="Call{T}" /></remarks>
	///<param name="operationName"> The name of the operation, for logging/debugging purposes. </param>
	///<param name="operationType"> The <see cref="OperationType" />, controls whether a logic-idle event should be triggered after this operation is performed. </param>
	///<param name="procedure"> The <see cref="Sys.Action" /> to invoke. </param>
	void Call( string operationName, OperationType operationType, Sys.Action procedure );

	///<summary>Posts an operation to the <see cref="EventDriver" />, waits until the operation completes, and returns the result. </summary>
	///<remarks>May only be invoked from outside of the <see cref="EventDriver" />'s thread. </remarks>
	///<remarks>See also: <seealso cref="Post" /></remarks>
	///<remarks>See also: <seealso cref="Call" /></remarks>
	///<param name="operationName"> The name of the operation, for logging/debugging purposes. </param>
	///<param name="operationType"> The <see cref="OperationType" />, controls whether a logic-idle event should be triggered after this operation is performed. </param>
	///<param name="function"> The <see cref="Sys.Func{R}" /> to invoke. </param>
	T Call<T>( string operationName, OperationType operationType, Sys.Func<T> function );
}
