namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit.Extensions;

/// <summary>
/// Base class for ViewModels representing quantities such as Length, Angle, etc.
/// </summary>
public abstract class QuantityViewModel : ValidatedViewModel
{
	public string Title { get; }
	public double Amount { get => GetPropertyValue<double>(); set => SetPropertyValue( value ); }

	protected QuantityViewModel( Platform platform, string title )
			: base( platform )
	{
		Title = title;
	}

	protected override IEnumerable<string> ToStrings() => base.ToStrings().Concat( $"Amount={Amount}" );

	public void AddErratum( string message ) => AddErratum( nameof( Amount ), message );
}
