namespace MikeNakis.ViewModeling.Localization;

using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using SysCompiler = System.Runtime.CompilerServices;

public class Wording
{
	public string Name { get; }
	public int ParameterCount { get; }

	public Wording( int parameterCount, string name )
	{
		Assert( name.Trim() == name );
		Name = name;
		ParameterCount = parameterCount;
	}

	public bool Equals( Wording other ) => Name == other.Name;
	public sealed override bool Equals( object? other ) => other is Wording kin && Equals( kin );
	public override int GetHashCode() => Name.GetHashCode2();
	public override string ToString() => $"{Name} ({ParameterCount} parameters)";
	public static bool operator ==( Wording a, Wording b ) => Equals( a, b );
	public static bool operator !=( Wording a, Wording b ) => !Equals( a, b );
}

public class Wording0 : Wording
{
	public Wording0( [SysCompiler.CallerMemberName] string? name = null )
			: base( 0, name.OrThrow() )
	{ }
}

public class Wording1 : Wording
{
	public Wording1( [SysCompiler.CallerMemberName] string? name = null )
			: base( 1, name.OrThrow() )
	{ }
}

public class Wording2 : Wording
{
	public Wording2( [SysCompiler.CallerMemberName] string? name = null )
			: base( 2, name.OrThrow() )
	{ }
}

public class Wording3 : Wording
{
	public Wording3( [SysCompiler.CallerMemberName] string? name = null )
			: base( 3, name.OrThrow() )
	{ }
}
