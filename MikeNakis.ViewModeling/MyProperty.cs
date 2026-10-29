namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using MikeNakis.Kit;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using LinqExpr = System.Linq.Expressions;
using Sys = System;
using SysCompModel = System.ComponentModel;

public interface ReadableProperty
{
	string Name { get; }
	Sys.Type Type { get; }

	object? GetRawValue();
}

public interface WritableProperty : ReadableProperty
{
	void SetRawValue( object value );
}

public interface ReadableProperty<out T> : ReadableProperty
{
	T? GetValue();
}

public interface WritableProperty<T> : ReadableProperty<T>, WritableProperty
{
	void SetValue( T? value );
}

public abstract class MyProperty : Feature, ReadableProperty
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly OverridableGuard onDisposeOverridableGuard = new( nameof( OnDispose ) );
	public ViewModel ViewModel { get; }
	public string Name { get; }
	public Sys.Type Type { get; }

	protected MyProperty( ViewModel viewModel, string name, Sys.Type type )
	{
		ViewModel = viewModel;
		Name = name;
		Type = type;
	}

	protected virtual void OnDispose()
	{
		onDisposeOverridableGuard.InvocationComplete();
	}

	public abstract object? GetRawValue();

	public void Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		Sys.GC.SuppressFinalize( this );
		onDisposeOverridableGuard.Invoke( OnDispose );
		lifeGuard.Dispose();
	}

	protected bool NotDisposedAssertion()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return true;
	}
}

public abstract class MyProperty<T> : MyProperty, ReadableProperty<T>
{
	internal int ReEntranceCount { get; set; }

	protected MyProperty( ViewModel viewModel, string name )
			: base( viewModel, name, typeof( T ) )
	{ }

	public override object? GetRawValue() => GetValue();

	public abstract T? /*ReadableProperty<T>.*/ GetValue();

	protected void InternalSetValue( T? newValue )
	{
		Assert( Equals( newValue, default ) || Type.IsInstanceOfType( newValue ) );
		using( new ReEntranceGuard<T>( this ) )
		{
			OnSetValue( newValue );
			InvokeFeatureChanged();
		}
	}

	protected abstract void OnSetValue( T? newValue );

	protected void InvokeFeatureChanged() => ViewModel.FeatureChanged( this );

	public override string ToString()
	{
		string valueAsString;
		try
		{
			valueAsString = "" + GetValue();
		}
		catch( Sys.Exception exception )
		{
			valueAsString = KitHelpers.BuildShortExceptionMessage( $"Exception", exception );
		}
		return $"{Name}:{Type} = {valueAsString}";
	}
}

public class ContainingProperty<T> : MyProperty<T>, WritableProperty<T>
{
	T? value = default!;

	public WritableProperty<T> WritableProperty => this;

	public ContainingProperty( ViewModel viewModel, string name )
			: base( viewModel, name )
	{ }

	public override T? GetValue() => value;

	public void SetValue( T? newValue )
	{
		if( DotNetHelpers.Equals( GetValue(), newValue ) )
			return;
		InternalSetValue( newValue );
	}

	public void /*WritableProperty.*/ SetRawValue( object value )
	{
		SetValue( (T)value );
	}

	protected override void OnSetValue( T? newValue )
	{
		value = newValue;
	}

	public void SetValueInternal( T? value )
	{
		this.value = value; //realize value without triggering notification!
	}
}

public class EchoingProperty<T> : ContainingProperty<T>
{
	readonly SysCompModel.INotifyPropertyChanged targetViewModel;
	readonly CSharpProperty<T> targetProperty;
	readonly string targetPropertyName;

	public EchoingProperty( ViewModel viewModel, string name, SysCompModel.INotifyPropertyChanged targetViewModel, string targetPropertyName )
			: base( viewModel, name )
	{
		this.targetViewModel = targetViewModel;
		this.targetPropertyName = targetPropertyName;
		targetProperty = CSharpProperty<T>.Create( targetViewModel, targetPropertyName );
		Identity( targetProperty.Value ); //Obtain the property value once, to ensure that the property is created.
		targetViewModel.PropertyChanged += onTargetViewModelPropertyChanged;
	}

	protected override void OnDispose()
	{
		Assert( NotDisposedAssertion() );
		targetViewModel.PropertyChanged -= onTargetViewModelPropertyChanged;
		base.OnDispose();
	}

	public override T? GetValue() => targetProperty.Value;

	protected override void OnSetValue( T? newValue )
	{
		targetProperty.Value = newValue;
	}

	void onTargetViewModelPropertyChanged( object? sender, SysCompModel.PropertyChangedEventArgs e )
	{
		Assert( sender.ReferenceEquals( targetViewModel ) );
		if( e.PropertyName == targetPropertyName )
			InvokeFeatureChanged();
	}
}

public interface RelayingProperty
{
	object TargetEntity { get; }
	void Update();
}

public class RelayingProperty<T> : ContainingProperty<T>, RelayingProperty
{
	readonly CSharpProperty<T> targetProperty;

	public RelayingProperty( ViewModel viewModel, string name, object targetEntity, string targetPropertyName )
			: base( viewModel, name )
	{
		Assert( targetEntity is not SysCompModel.INotifyPropertyChanged ); //if you have an object which implements INotifyPropertyChanged, please use an Echoing property instead of a forwarding property.
		targetProperty = CSharpProperty<T>.Create( targetEntity, targetPropertyName );
		SetValueInternal( targetProperty.Value ); //realize target value without triggering property change!
	}

	bool assertUpToDate()
	{
		T? currentValue = base.GetValue();
		T? relayedValue = targetProperty.Value;
		Assert( Equals( currentValue, relayedValue ) );
		return true;
	}

	public override T? GetValue()
	{
		Assert( assertUpToDate() );
		return base.GetValue();
	}

	protected override void OnSetValue( T? newValue )
	{
		Assert( assertUpToDate() );
		targetProperty.Value = newValue;
		base.OnSetValue( newValue );
	}

	public object TargetEntity => targetProperty.ContainingInstance;

	public void Update()
	{
		T? relayedValue = targetProperty.Value;
		if( Equals( base.GetValue(), relayedValue ) )
			return;
		SetValue( relayedValue );
		InvokeFeatureChanged();
	}
}

public class CalculatedProperty<T> : MyProperty<T>
{
	readonly Sys.Func<T> computation;
	readonly HashSet<string> observedPropertiesExpressions = new();
	readonly MutableList<PropertyObservation> propertyObservers = new();
	T? value;

	public CalculatedProperty( ViewModel viewModel, string name, Sys.Func<T> computation )
			: base( viewModel, name )
	{
		this.computation = computation;
		value = computation.Invoke(); //realize value without triggering property change!
	}

	protected override void OnDispose()
	{
		foreach( PropertyObservation propertyObserver in propertyObservers )
			propertyObserver.Dispose();
		base.OnDispose();
	}

	public CalculatedProperty<T> ObservesProperty<E>( LinqExpr.Expression<Sys.Func<E>> propertyExpression )
	{
		bool added = observedPropertiesExpressions.Add( propertyExpression.ToString() );
		Assert( added );
		PropertyObservation propertyObserver = PropertyObservation.Create( propertyExpression, Update );
		propertyObservers.Add( propertyObserver );
		return this;
	}

	public override T? GetValue()
	{
		//NOTE: we cannot assert that the stored value is equal to the value we would obtain by re-evaluating the expression, because
		// we may be not-up-to-date under normal circumstances.
		//Scenario: property A is changed; as a result, calculated properties B and C will be recalculated, in this order.
		// The calculation of B depends on both A and C, so at the moment that B is recalculating itself, C has not had the chance to recalculate itself yet.
		// So, when B asks for the value of C, C discovers that it is not up-to-date. This is normal, therefore we cannot assert against it.
		//Question is then what to do when requesting the value:
		// 1. always update (i.e. re-evaluate)
		// 2. just return the stored value.
		// Although we cannot be sure that they are the same, re-evaluation may unnecessarily invoke time-consuming calculations.
		// Moreover, if the observed properties are set correctly then eventually the value will still be updated and dependencies notified.
		//Conclusion is thus to just return the stored value.
		return value;
	}

	protected override void OnSetValue( T? newValue )
	{
		value = newValue;
	}

	public void Update()
	{
		T newValue = computation.Invoke();
		if( DotNetHelpers.Equals( value, newValue ) )
			return;
		InternalSetValue( newValue );
	}
}
