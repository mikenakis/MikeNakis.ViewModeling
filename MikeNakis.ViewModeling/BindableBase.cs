namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using SysCompiler = System.Runtime.CompilerServices;
using SysCompModel = System.ComponentModel;
using SysDiag = System.Diagnostics;

[SysDiag.DebuggerDisplay( "{ToString(),nq}" )]
public abstract class BindableBase : SysCompModel.INotifyPropertyChanged
{
	public override string ToString() => ToStrings().Where( s => s.Length != 0 ).MakeString( " " );
	readonly ThreadGuard threadGuard = ThreadGuard.Create();
	readonly OverridableGuard onPropertyChangedOverridableGuard = new( nameof( OnPropertyChanged ) );
	public event SysCompModel.PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Constructor.</summary>
	protected BindableBase()
	{ }

	protected internal void InvokeOnPropertyChanged( [SysCompiler.CallerMemberName] string? propertyName = null )
	{
		Assert( threadGuard.InThreadAssertion() );
		Assert( propertyName != null );
		onPropertyChangedOverridableGuard.Invoke( () => OnPropertyChanged( propertyName ) );
	}

	protected virtual void OnPropertyChanged( string propertyName )
	{
		onPropertyChangedOverridableGuard.InvocationComplete();
		PropertyChanged?.Invoke( this, new SysCompModel.PropertyChangedEventArgs( propertyName ) );
	}

	protected virtual IEnumerable<string> ToStrings() => Enumerable.Empty<string>().Concat( $"{GetType().Name}:" );
	public sealed override bool Equals( object? other ) => this.ReferenceEquals( other );
	public sealed override int GetHashCode() => SysCompiler.RuntimeHelpers.GetHashCode( this );
}
