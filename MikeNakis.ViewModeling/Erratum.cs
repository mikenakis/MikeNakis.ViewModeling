namespace MikeNakis.ViewModeling;

using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;

public sealed class Erratum
{
	public string PropertyName { get; }
	public string Message { get; }

	public Erratum( string propertyName, string message )
	{
		Assert( !string.IsNullOrEmpty( propertyName ) );
		Assert( !string.IsNullOrEmpty( message ) );
		PropertyName = propertyName;
		Message = message;
	}

	// ReSharper disable once MemberCanBePrivate.Local
	public bool Equals( Erratum other )
	{
		return PropertyName == other.PropertyName && Message == other.Message;
	}

	public override bool Equals( object? obj ) => obj is Erratum kin && Equals( kin );

	public override int GetHashCode() //TODO replace with ReflectionHelpers.MemberwiseHashCode()
	{
		unchecked
		{
			return (PropertyName.GetHashCode2() * 397) ^ Message.GetHashCode2();
		}
	}

	public override string ToString()
	{
		return $"{PropertyName} \"{Message}\"";
	}
}
