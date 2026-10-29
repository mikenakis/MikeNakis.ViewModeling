namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using LinqExpr = System.Linq.Expressions;
using Sys = System;
using SysCompModel = System.ComponentModel;
using SysReflect = System.Reflection;

/// Provides a way to observe property changes of INotifyPropertyChanged objects and invokes a custom action when the PropertyChanged event is fired.
/// Originally from the "Prism" library.
public sealed class PropertyObservation : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly Node rootNode;

	PropertyObservation( LinqExpr.Expression propertyExpression, Sys.Action action )
	{
		Stack<SysReflect.MemberInfo> memberInfoStack = new();
		while( propertyExpression is LinqExpr.MemberExpression memberExpression ) // Gets the root of the property chain.
		{
			propertyExpression = memberExpression.Expression.OrThrow();
			memberInfoStack.Push( memberExpression.Member ); // Records the member info as property info
		}

		Assert( propertyExpression is LinqExpr.ConstantExpression ); // Operation not supported for the given expression type. Only MemberExpression and ConstantExpression are currently supported.
		LinqExpr.ConstantExpression constantExpression = (LinqExpr.ConstantExpression)propertyExpression;

		rootNode = newNode( memberInfoStack.Pop(), action );
		Node previousNode = rootNode;
		foreach( SysReflect.MemberInfo memberInfo in memberInfoStack ) // Create a node chain that corresponds to the property chain.
		{
			Node currentNode = newNode( memberInfo, action );
			previousNode.Next = currentNode;
			previousNode = currentNode;
		}

		Assert( constantExpression.Value is SysCompModel.INotifyPropertyChanged ); // Trying to subscribe PropertyChanged listener in object that owns '{rootNode.MemberInfo.Name}' property, but the object does not implements INotifyPropertyChanged.
		SysCompModel.INotifyPropertyChanged notifyPropertyChanged = (SysCompModel.INotifyPropertyChanged)constantExpression.Value.OrThrow();
		rootNode.SubscribeListenerFor( notifyPropertyChanged );
	}

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		rootNode.UnsubscribeListener();
		lifeGuard.Dispose();
	}

	static Node newNode( SysReflect.MemberInfo memberInfo, Sys.Action action )
	{
		return memberInfo.MemberType switch
		{
			SysReflect.MemberTypes.Field => new FieldNode( (SysReflect.FieldInfo)memberInfo, action ),
			SysReflect.MemberTypes.Property => new PropertyNode( (SysReflect.PropertyInfo)memberInfo, action ),
			_ => throw Failure()
		};
	}

	/// <summary>
	/// Observes a property that implements INotifyPropertyChanged, and automatically calls a custom action on
	/// property changed notifications. The given expression must be in this form: "() =&gt; Prop.NestedProp.PropToObserve".
	/// </summary>
	/// <param name="propertyExpression">Expression representing property to be observed. Ex.: "() =&gt; Prop.NestedProp.PropToObserve".</param>
	/// <param name="action">Action to be invoked when PropertyChanged event occurs.</param>
	public static PropertyObservation Create<T>( LinqExpr.Expression<Sys.Func<T>> propertyExpression, Sys.Action action )
	{
		propertyExpression.Compile().Invoke(); //Execute the expression once, to ensure that the property gets created.
		return new PropertyObservation( propertyExpression.Body, action );
	}

	/// <summary>
	/// Represents each node of nested properties expression and takes care of
	/// subscribing/unsubscribing INotifyPropertyChanged.PropertyChanged listeners on it.
	/// </summary>
	abstract class Node
	{
		readonly Sys.Action action;
		protected SysCompModel.INotifyPropertyChanged? NotifyingObject { get; private set; }

		public Node? Next { get; set; }

		protected Node( Sys.Action action )
		{
			this.action = action;
		}

		public void SubscribeListenerFor( SysCompModel.INotifyPropertyChanged notifyingObject )
		{
			Assert( NotifyingObject == null ); //experiment: is this ok to assert?
			NotifyingObject = notifyingObject;
			NotifyingObject.PropertyChanged += onPropertyChanged;
			if( Next == null )
				return;
			generateNextNode();
		}

		void generateNextNode()
		{
			object? obj = GetValue();
			if( obj == null )
				return;
			Assert( Next != null );
			Assert( obj is SysCompModel.INotifyPropertyChanged, () => throw Failure( $"Cannot observe property {obj.GetType()}.{Next.MemberInfo.Name} because {obj.GetType()} does not implement INotifyPropertyChanged." ) );
			SysCompModel.INotifyPropertyChanged notifyPropertyChanged = (SysCompModel.INotifyPropertyChanged)obj;
			Next.SubscribeListenerFor( notifyPropertyChanged );
		}

		public abstract SysReflect.MemberInfo MemberInfo { get; }

		protected abstract object? GetValue();

		public void UnsubscribeListener()
		{
			Assert( NotifyingObject != null );
			NotifyingObject!.PropertyChanged -= onPropertyChanged;
			Next?.UnsubscribeListener();
		}

		void onPropertyChanged( object? sender, SysCompModel.PropertyChangedEventArgs e )
		{
			if( e.PropertyName != MemberInfo.Name && e.PropertyName != null )
				return;
			action.Invoke();
			if( Next == null )
				return;
			Next.UnsubscribeListener();
			generateNextNode();
		}
	}

	sealed class PropertyNode : Node
	{
		readonly SysReflect.PropertyInfo propertyInfo;

		public PropertyNode( SysReflect.PropertyInfo propertyInfo, Sys.Action action )
				: base( action )
		{
			Assert( propertyInfo != null );
			this.propertyInfo = propertyInfo;
		}

		protected override object? GetValue()
		{
			return propertyInfo.GetValue( NotifyingObject );
		}

		public override SysReflect.MemberInfo MemberInfo => propertyInfo;
	}

	sealed class FieldNode : Node
	{
		readonly SysReflect.FieldInfo fieldInfo;

		public FieldNode( SysReflect.FieldInfo memberInfo, Sys.Action action )
				: base( action )
		{
			Assert( memberInfo != null );
			fieldInfo = memberInfo;
		}

		protected override object? GetValue()
		{
			return fieldInfo.GetValue( NotifyingObject );
		}

		public override SysReflect.MemberInfo MemberInfo => fieldInfo;
	}
}
