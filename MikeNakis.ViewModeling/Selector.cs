namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit.Extensions;
using Sys = System;

//TODO implement Equals() in this class! BindableBase should probably not implement Equals() and GetHashCode().
#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
public abstract class Selector : ValidatedViewModel, Sys.IEquatable<Selector>
#pragma warning restore CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
{
	public string Title { get; }
	public abstract bool IsVisible { get; }
	public abstract bool IsSelected { get; set; }

	protected Selector( Platform platform, string title )
			: base( platform )
	{
		Title = title;
	}

	public bool Equals( Selector? other ) => Title == other?.Title;

	// [Sys.Obsolete] public sealed override bool Equals( object? other ) => other is Selector kin && Equals( kin );
	// public override int GetHashCode() => Sys.HashCode.Combine( Title );

	protected override IEnumerable<string> ToStrings() => base.ToStrings().Concat( $"'{Title}' ({(IsEnabled ? "enabled" : "disabled")}, {(IsVisible ? "visible" : "hidden")})" );
}
