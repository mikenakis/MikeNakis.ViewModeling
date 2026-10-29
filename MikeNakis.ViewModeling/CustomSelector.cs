namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using MikeNakis.Kit.Collections;
using static MikeNakis.Kit.GlobalStatics;
using LinqExpr = System.Linq.Expressions;
using Sys = System;

public class CustomSelector : Selector
{
	readonly Sys.Func<bool> enabledFunction, visibleFunction, selectedFunction;
	readonly Sys.Action<Selector> setSelectedProcedure;
	readonly MutableList<PropertyObservation> propertyObservers = new();
	readonly HashSet<string> observedPropertiesExpressions = new();
	public override bool IsEnabled => GetPropertyValue( true );
	public override bool IsVisible => GetPropertyValue( true );

	// FIXME: use a more elegant way to provide this class with a means to represent and specify whether it is selected.
	public override bool IsSelected
	{
		get => GetPropertyValue( false );
		// ReSharper disable once ValueParameterNotUsed
		set
		{
			SetPropertyValue( true );
			setSelectedProcedure( this );
		}
	}

	public CustomSelector( Platform platform, string title, LinqExpr.Expression<Sys.Func<Selector?>> currentSelector, Sys.Action<Selector> setSelectedProcedure, Sys.Func<bool>? enabledFunction = null, Sys.Func<bool>? visibleFunction = null )
			: base( platform, title )
	{
		this.enabledFunction = enabledFunction ?? (static () => true);
		this.visibleFunction = visibleFunction ?? (static () => true);
		Sys.Func<Selector?> compiledSelector = currentSelector.Compile();
		selectedFunction = () => Equals( compiledSelector.Invoke(), this );
		this.setSelectedProcedure = setSelectedProcedure;
		updateProperties();
		ObservesProperty( currentSelector );
	}

	protected override void OnDispose()
	{
		Assert( IsAliveAssertion() );
		foreach( PropertyObservation propertyObserver in propertyObservers )
			propertyObserver.Dispose();
		base.OnDispose();
	}

	void updateProperties()
	{
		SetPropertyValue( enabledFunction.Invoke(), propertyName: nameof( IsEnabled ) );
		SetPropertyValue( visibleFunction.Invoke(), propertyName: nameof( IsVisible ) );
		SetPropertyValue( selectedFunction.Invoke(), propertyName: nameof( IsSelected ) );
	}

	public CustomSelector ObservesProperty<T>( LinqExpr.Expression<Sys.Func<T>> propertyExpression )
	{
		if( !observedPropertiesExpressions.Add( propertyExpression.ToString() ) )
			return this;
		PropertyObservation propertyObserver = PropertyObservation.Create( propertyExpression, updateProperties );
		propertyObservers.Add( propertyObserver );
		return this;
	}
}
