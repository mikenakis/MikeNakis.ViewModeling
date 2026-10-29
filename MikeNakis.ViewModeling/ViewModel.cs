namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using LinqExpr = System.Linq.Expressions;
using Sys = System;
using SysCompiler = System.Runtime.CompilerServices;
using SysCompModel = System.ComponentModel;
using SysReflect = System.Reflection;

public abstract class ViewModel : BindableBase, Sys.IDisposable, Feature
{
	readonly StatefulLifeGuard lifeGuard = StatefulLifeGuard.Create();
	readonly OverridableGuard onDisposeOverridableGuard = new( nameof( OnDispose ) );
	readonly Features features = new();
	public event Sys.Action? Disposed;
	public Platform Platform { get; }
	private protected IEnumerable<Feature> Features => features.All;

	/// <summary>Constructor.</summary>
	protected ViewModel( Platform platform )
	{
		Platform = platform;
	}

	public void /*Sys.IDisposable.*/ Dispose()
	{
		Assert( IsAliveAssertion() );
		Sys.GC.SuppressFinalize( this );
		onDisposeOverridableGuard.Invoke( OnDispose );
		Disposed?.Invoke();
		features.Dispose();
		lifeGuard.Dispose();
	}

	protected virtual void OnDispose()
	{
		Assert( IsAliveAssertion() );
		onDisposeOverridableGuard.InvocationComplete();
	}

	public string GetFeatureName( Feature feature ) => features.GetFeatureName( feature );

	protected T ObtainChildViewModel<T>( Sys.Func<T> factory, [SysCompiler.CallerMemberName] string propertyName = null! ) //
			where T : ViewModel
	{
		Assert( propertyName != null );
		if( features.TryGetFeature( propertyName, out T existingChildVieWModel ) )
			return existingChildVieWModel;
		Assert( ReflectionHelpers.TryGetPropertyInfo( GetType(), propertyName ) != null );
		T newChildViewModel = factory.Invoke();
		AddChildViewModel( propertyName, newChildViewModel );
		return newChildViewModel;
	}

	protected void AddChildViewModel<T>( string propertyName, T childViewModel ) //
			where T : ViewModel
	{
		childViewModel.PropertyChanged += onChildViewModelPropertyChanged;
		features.AddFeature( propertyName, childViewModel );
	}

	protected T SetChildViewModel<T>( T childViewModel, [SysCompiler.CallerMemberName] string propertyName = null! ) //
			where T : ValidatedViewModel
	{
		Assert( propertyName != null );
		if( features.TryGetFeature( propertyName, out ValidatedViewModel existingChildViewModel ) )
		{
			Assert( !childViewModel.ReferenceEquals( existingChildViewModel ) );
			RemoveChildViewModel( existingChildViewModel );
		}
		AddChildViewModel( propertyName, childViewModel );
		FeatureChanged( childViewModel );
		return childViewModel;
	}

	protected void RemoveChildViewModel( ValidatedViewModel childViewModel )
	{
		childViewModel.PropertyChanged -= onChildViewModelPropertyChanged;
		features.RemoveFeature( childViewModel );
	}

	protected Command Command( Sys.Action handler, LinqExpr.Expression<Sys.Func<bool>> isEnabledExpression, Sys.Action<PropertyObserver>? propertyObserverInitializer = null, OperationType operationType = OperationType.Major, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Sys.Func<bool> isEnabled = isEnabledExpression.Compile();
		return command( handler, isEnabled, propertyObserverInitializer ?? (c => { }), operationType, propertyName );
	}

	protected Command Command( Sys.Action handler, OperationType operationType = OperationType.Major, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		return command( handler, () => true, _ => { }, operationType, propertyName );
	}

	Command command( Sys.Action handler, Sys.Func<bool> isEnabled, Sys.Action<PropertyObserver> propertyObserverInitializer, OperationType operationType, string propertyName )
	{
		Assert( IsAliveAssertion() );
		Assert( handler != null );
		Assert( propertyName != null );
		if( features.TryGetFeature( propertyName, out Command existingCommand ) )
			return existingCommand;
		Command newCommand = Platform.NewCommand( this, operationType, handler, isEnabled );
		features.AddFeature( propertyName, newCommand );
		propertyObserverInitializer.Invoke( newCommand );
		return newCommand;
	}

	// protected Command Command( Procedure handler, Function<bool>? isEnabled = null, OperationType operationType = OperationType.Major, //
	// 		[SysCompiler.CallerMemberName] string propertyName = null! )
	// {
	// 	Assert( IsAliveAssertion() );
	// 	Assert( propertyName != null );
	// 	if( features.TryGetFeature( propertyName, out Command existingFeature ) )
	// 		return existingFeature;
	// 	Command newFeature = Platform.NewCommand( this, operationType, handler, isEnabled ?? (static () => true) );
	// 	features.AddFeature( propertyName, newFeature );
	// 	return newFeature;
	// }

	MyProperty? tryGetProperty( string propertyName )
	{
		if( !features.TryGetFeature( propertyName, out MyProperty existingProperty ) )
			return null;
		return existingProperty;
	}

	MyProperty<T>? tryGetProperty<T>( string propertyName )
	{
		Assert( assertCorrectPropertyType( typeof( T ), propertyName ) );
		MyProperty? myProperty = tryGetProperty( propertyName );
		return (MyProperty<T>?)myProperty;
	}

	T getPropertyValueUsingReflection<T>( string propertyName )
	{
		SysReflect.PropertyInfo propertyInfo = ReflectionHelpers.GetPropertyInfo( GetType(), propertyName );
		object? rawValue = propertyInfo.GetValue( this );
		return (T)rawValue!;
	}

	ContainingProperty<T> obtainContainingProperty<T>( string propertyName, Sys.Func<T> defaultValueSupplier )
	{
		ReadableProperty<T>? existingProperty = tryGetProperty<T>( propertyName );
		if( existingProperty != null )
			return (ContainingProperty<T>)existingProperty;
		ContainingProperty<T> newProperty = new( this, propertyName );
		features.AddFeature( propertyName, newProperty );
		newProperty.SetValueInternal( defaultValueSupplier.Invoke() );
		return newProperty;
	}

	CalculatedProperty<T> obtainCalculatedProperty<T>( string propertyName, Sys.Func<T> computation, Sys.Action<CalculatedProperty<T>> initializer )
	{
		ReadableProperty<T>? existingProperty = tryGetProperty<T>( propertyName );
		if( existingProperty != null )
			return (CalculatedProperty<T>)existingProperty;
		CalculatedProperty<T> newProperty = new( this, propertyName, computation );
		features.AddFeature( propertyName, newProperty );
		initializer.Invoke( newProperty );
		return newProperty;
	}

	protected T? GetCalculatedPropertyValue<T>( Sys.Func<T> computation, Sys.Action<CalculatedProperty<T>> initializer, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Assert( IsAliveAssertion() );
		Assert( propertyName != null );
		using( Platform.NewLogicEntrance( $"Get {propertyName}", OperationType.Minor ) )
		{
			CalculatedProperty<T> calculatedProperty = obtainCalculatedProperty( propertyName, computation, initializer );
			return calculatedProperty.GetValue();
		}
	}

	//NOTE: T should be immutable.
	protected T GetPropertyValue<T>( T defaultValue, [SysCompiler.CallerMemberName] string propertyName = null! ) //
			where T : struct
		=> GetPropertyValue( () => defaultValue, propertyName );

	protected T GetPropertyValue<T>( Sys.Func<T>? defaultValueSupplier = null, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Assert( IsAliveAssertion() );
		Assert( propertyName != null );
		using( Platform.NewLogicEntrance( $"Get {propertyName}", OperationType.Minor ) )
		{
			ContainingProperty<T> property = obtainContainingProperty( propertyName, defaultValueSupplier ?? (static () => default!) );
			return property.GetValue()!;
		}
	}

	protected void SetPropertyValue<T>( T value, OperationType operationType = OperationType.Major, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Assert( IsAliveAssertion() );
		Assert( propertyName != null );
		using( Platform.NewLogicEntrance( $"Set {propertyName} = {value}", operationType ) )
		{
			ContainingProperty<T> property = obtainContainingProperty( propertyName, () => getPropertyValueUsingReflection<T>( propertyName ) );
			property.SetValue( value );
		}
	}

	bool assertCorrectPropertyType( Sys.Type accessorPropertyType, string propertyName )
	{
		Assert( ReflectionHelpers.GetPropertyType( GetType(), propertyName ), actualPropertyType => actualPropertyType == accessorPropertyType, actualPropertyType => throw Failure( $"Property type mismatch: {GetType().Name}.{propertyName} is accessed as {accessorPropertyType}, expected {actualPropertyType}" ) );
		return true;
	}

	EchoingProperty<T> obtainEchoingProperty<T>( string propertyName, ViewModel targetViewModel, string targetPropertyName )
	{
		Assert( assertCorrectPropertyType( typeof( T ), propertyName ) );
		ReadableProperty<T>? existingProperty = tryGetProperty<T>( propertyName );
		if( existingProperty != null )
			return (EchoingProperty<T>)existingProperty;
		EchoingProperty<T> newProperty = new( this, propertyName, targetViewModel, targetPropertyName );
		features.AddFeature( propertyName, newProperty );
		return newProperty;
	}

	protected T? GetEchoingPropertyValue<T>( ViewModel targetViewModel, string targetPropertyName, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Assert( IsAliveAssertion() );
		Assert( propertyName != null );
		using( Platform.NewLogicEntrance( $"Get {propertyName}", OperationType.Minor ) )
		{
			EchoingProperty<T> property = obtainEchoingProperty<T>( propertyName, targetViewModel, targetPropertyName );
			return property.GetValue();
		}
	}

	protected void SetEchoingPropertyValue<T>( T value, ViewModel targetViewModel, string targetPropertyName, //
			OperationType operationType = OperationType.Major, [SysCompiler.CallerMemberName] string propertyName = null! )
	{
		Assert( IsAliveAssertion() );
		Assert( propertyName != null );
		using( Platform.NewLogicEntrance( $"Set {propertyName} = {value}", operationType ) )
		{
			EchoingProperty<T> property = obtainEchoingProperty<T>( propertyName, targetViewModel, targetPropertyName );
			property.SetValue( value );
		}
	}

	protected override IEnumerable<string> ToStrings() => base.ToStrings().Concat( lifeGuard.ToString()!, $"Type=\"{GetType().FullName}\"", $"Features={{{features}}}" );
	internal bool ContainsFeature( Feature feature ) => features.ContainsFeature( feature );

	protected bool IsAliveAssertion()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return true;
	}

	protected bool IsAlive => lifeGuard.IsAlive;

	internal void FeatureChanged( Feature feature )
	{
		if( !IsAlive )
			return;
		if( !ContainsFeature( feature ) ) //this may happen if the feature has not been created yet.
			return;
		//logger.Debug( $"FeatureChanged: {feature}" );	//Too spammy
		string name = GetFeatureName( feature );
		InvokeOnPropertyChanged( name );
	}

	public void ChildViewModelChanged( ViewModel childViewModel )
	{
		if( !IsAlive )
			return;
		if( !ContainsFeature( childViewModel ) )
			return;
		//Log.Debug( $"ChildViewModelChanged: {childViewModel}" );
		string name = GetFeatureName( childViewModel );
		InvokeOnPropertyChanged( name );
	}

	void onChildViewModelPropertyChanged( object? sender, SysCompModel.PropertyChangedEventArgs e )
	{
		ChildViewModelChanged( (ViewModel)sender! );
	}
}
