namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using LegacyCollections = System.Collections;
using Sys = System;
using SysCompModel = System.ComponentModel;

///<summary>A <see cref="ViewModel" /> with Validation.</summary>
///<remarks>Note: it implements <see cref="Feature" /> because a <see cref="ValidatedViewModel" /> can be a child of another <see cref="ValidatedViewModel" />.</remarks>
public abstract class ValidatedViewModel : ViewModel, SysCompModel.INotifyDataErrorInfo
{
	readonly MutableList<Erratum> errata = new();
	Sys.EventHandler<SysCompModel.DataErrorsChangedEventArgs>? errorsChanged;
	ValidationContext? validationContext;
	readonly OverridableGuard onValidateOverridableGuard = new( nameof( OnValidate ) );

	/// <summary>Constructor.</summary>
	protected ValidatedViewModel( Platform platform )
			: base( platform )
	{ }

	protected internal bool IsValidatingAssertion()
	{
		Assert( IsValidating );
		return true;
	}

	public virtual bool IsEnabled => true;

	public bool HasErrors => errata.Count > 0;

	protected internal bool IsValidating => validationContext != null;

	protected bool HasErrorMessages => GetErrorMessages().Count > 0;

	protected ISet<string> GetErrorMessages()
	{
		HashSet<string> errorMessages = new();
		getErrorMessagesRecursive( errorMessages );
		return errorMessages;
	}

	void getErrorMessagesRecursive( ICollection<string> collection )
	{
		collection.AddRange( errata.AsEnumerable.Select( erratum => erratum.Message ) );
		foreach( ValidatedViewModel childViewModel in Features.OfType<ValidatedViewModel>() )
			childViewModel.getErrorMessagesRecursive( collection );
	}

	protected override void OnPropertyChanged( string propertyName )
	{
		PerformValidation();
		base.OnPropertyChanged( propertyName );
	}

	protected sealed class ValidationContext : Sys.IDisposable
	{
		readonly LifeGuard lifeGuard = LifeGuard.Create();
		readonly ValidatedViewModel viewModel;
		readonly IReadOnlyCollection<Erratum> oldErrata;
		readonly Dictionary<string, ValidationContext> childValidationContexts;

		public ValidationContext( ValidatedViewModel viewModel )
		{
			Assert( viewModel.validationContext == null );
			this.viewModel = viewModel;

			childValidationContexts = new Dictionary<string, ValidationContext>();
			foreach( ValidatedViewModel childViewModel in viewModel.Features.OfType<ValidatedViewModel>() )
			{
				ValidationContext childValidationContext = new( childViewModel );
				string propertyName = viewModel.GetFeatureName( childViewModel );
				childValidationContexts.Add( propertyName, childValidationContext );
			}
			oldErrata = viewModel.errata.AsReadOnlyList.Collect();
			viewModel.errata.Clear();
			viewModel.validationContext = this;
		}

		public void Dispose()
		{
			Assert( lifeGuard.IsAliveAssertion() );
			Assert( viewModel.validationContext == this );
			foreach( ValidationContext childValidationContext in childValidationContexts.Values )
				childValidationContext.Dispose();
			if( oldErrata.Count != viewModel.errata.Count )
			{
				Log.Debug( $"old: {oldErrata.Count} errata; new: {viewModel.errata.Count}" );
				foreach( string propertyName in xor( getDistinctPropertyNames( oldErrata ), getDistinctPropertyNames( viewModel.errata.AsEnumerable ) ) )
				{
					Log.Debug( $"Errata changed for {viewModel}.{propertyName}" );
					viewModel.errorsChanged?.Invoke( viewModel, new SysCompModel.DataErrorsChangedEventArgs( propertyName ) );
				}
			}
			lifeGuard.Dispose();
			viewModel.validationContext = null;
		}

		static IEnumerable<string> getDistinctPropertyNames( IEnumerable<Erratum> errata ) //
			=> errata.Select( erratum => erratum.PropertyName ).Distinct();
	}

	public void PerformValidation()
	{
		//logger.Debug( $"{GetType().FullName}.PerformValidation()" );
		using( new ValidationContext( this ) )
			PerformValidation0();
	}

	protected void PerformValidation0()
	{
		onValidateOverridableGuard.Invoke( OnValidate );
	}

	protected virtual void OnValidate()
	{
		Assert( IsValidatingAssertion() );
		onValidateOverridableGuard.InvocationComplete();
		foreach( ValidatedViewModel childViewModel in Features.OfType<ValidatedViewModel>() )
			childViewModel.PerformValidation0();
	}

	protected void AddErratum( string propertyName, string message )
	{
		if( !IsValidating ) //may happen if this child viewmodel has not been created yet. Can be avoided if child view models do their own validation (have each child viewmodel contain the range to validate itself against.)
			return;
		Assert( IsValidatingAssertion() );
		errata.Add( new Erratum( propertyName, message ) );
	}

	protected void ValidateRange( string propertyName, string fieldLabel, double value, double min, double max )
	{
		if( value < min || value > max )
		{
			string message = $"{fieldLabel} must be between {min} and {max}";
			Log.Debug( $"{message}" );
			AddErratum( propertyName, message );
		}
	}

	protected void ValidateMin( string propertyName, string fieldLabel, double value, double min )
	{
		if( value < min )
		{
			string message = $"{fieldLabel} must be greater than or equal to {min}";
			AddErratum( propertyName, message );
		}
	}

	static IEnumerable<E> xor<E>( IEnumerable<E> a, IEnumerable<E> b )
	{
		SortedSet<E> aSet = new( a );
		SortedSet<E> bSet = new( b );
		foreach( E element in aSet.Where( e => !bSet.Contains( e ) ) )
			yield return element;
		foreach( E element in bSet.Where( e => !aSet.Contains( e ) ) )
			yield return element;
	}

	public bool ValidateAndPrompt()
	{
		PerformValidation();
		ISet<string> errorMessages = GetErrorMessages();
		if( errorMessages.Count > 0 )
		{
			string message = string.Join( "\n", errorMessages.Sorted() );
			Platform.Prompts.OkErrorPrompt( message );
			return false;
		}
		return true;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////
	/// CompModel.INotifyDataErrorInfo

	bool SysCompModel.INotifyDataErrorInfo.HasErrors => errata.Count > 0;

	public event Sys.EventHandler<SysCompModel.DataErrorsChangedEventArgs>? /* CompModel.INotifyDataErrorInfo. */ ErrorsChanged { add => errorsChanged += value; remove => errorsChanged += value; }

	public LegacyCollections.IEnumerable /* CompModel.INotifyDataErrorInfo. */ GetErrors( string? propertyName ) => getErrorMessages( propertyName );

	IEnumerable<string> getErrorMessages( string? propertyName )
	{
		IEnumerable<string> result = (string.IsNullOrEmpty( propertyName ) ? errata.AsEnumerable : errata.AsEnumerable.Where( erratum => erratum.PropertyName == propertyName )) //
				.Select( erratum => erratum.Message )
				.Distinct();
		if( DebugMode )
			result = result.Collect();
		return result;
	}
}
