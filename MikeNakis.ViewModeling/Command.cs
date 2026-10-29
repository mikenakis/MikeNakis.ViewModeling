namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using MikeNakis.Kit;
using MikeNakis.Kit.Collections;
using static MikeNakis.Kit.GlobalStatics;
using LinqExpr = System.Linq.Expressions;
using Sys = System;

public class Command : Feature, PropertyObserver
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly OverridableGuard onDisposeOverridableGuard = new( nameof( OnDispose ) );
	readonly Platform platform;
	public ViewModel ViewModel { get; }
	public string PropertyName => ViewModel.GetFeatureName( this );
	readonly OperationType operationType;
	readonly Sys.Action handler;
	readonly Sys.Func<bool> enabledFunction;
	readonly MutableList<PropertyObservation> propertyObservers = new();
	readonly HashSet<string> observedPropertiesExpressions = new();
	bool isEnabled;

	public Command( ViewModel parent, OperationType operationType, Sys.Action handler, Sys.Func<bool> enabledFunction )
	{
		platform = parent.Platform;
		ViewModel = parent;
		this.operationType = operationType;
		this.handler = handler;
		this.enabledFunction = enabledFunction;
		IsEnabled = enabledFunction.Invoke();
	}

	protected virtual void OnDispose()
	{
		Assert( NotDisposedAssertion() );
		onDisposeOverridableGuard.InvocationComplete();
		foreach( PropertyObservation propertyObserver in propertyObservers )
			propertyObserver.Dispose();
	}

	public PropertyObserver /*PropertyObserver.*/ Observes<T>( LinqExpr.Expression<Sys.Func<T>> propertyExpression )
	{
		Assert( NotDisposedAssertion() );
		if( !observedPropertiesExpressions.Add( propertyExpression.ToString() ) )
			return this;
		PropertyObservation propertyObserver = PropertyObservation.Create( propertyExpression, () => IsEnabled = enabledFunction.Invoke() );
		propertyObservers.Add( propertyObserver );
		return this;
	}

	public void /*Command.*/ Execute( bool ignoreNotEnabled = false )
	{
		Assert( NotDisposedAssertion() );
		Assert( IsEnabled || ignoreNotEnabled );
		using( platform.NewLogicEntrance( PropertyName, operationType ) )
			handler.Invoke();
	}

	public bool /*Command.*/ IsEnabled
	{
		get
		{
			Assert( NotDisposedAssertion() );
			return isEnabled;
		}
		protected set
		{
			Assert( NotDisposedAssertion() );
			if( value == isEnabled )
				return;
			isEnabled = value;
			OnIsEnabledChanged();
		}
	}

	protected virtual void OnIsEnabledChanged()
	{
		Assert( NotDisposedAssertion() );
	}

	public void Dispose()
	{
		Sys.GC.SuppressFinalize( this );
		onDisposeOverridableGuard.Invoke( OnDispose );
		lifeGuard.Dispose();
	}

	protected bool NotDisposedAssertion()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return true;
	}

	public override string ToString() => $"{PropertyName} IsEnabled={IsEnabled}";
}
