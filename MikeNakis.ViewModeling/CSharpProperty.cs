namespace MikeNakis.ViewModeling;

using MikeNakis.Kit;
using static MikeNakis.Kit.GlobalStatics;
using SysReflect = System.Reflection;

/// <summary>Encapsulates a C# Property.</summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class CSharpProperty<T>
{
	public static CSharpProperty<T> Create( object containingInstance, string propertyName )
	{
		SysReflect.PropertyInfo propertyInfo = ReflectionHelpers.GetPropertyInfo( containingInstance.GetType(), propertyName );
		Assert( propertyInfo.PropertyType == typeof( T ) );
		return new CSharpProperty<T>( containingInstance, propertyInfo );
	}

	public object ContainingInstance { get; }
	readonly SysReflect.PropertyInfo propertyInfo;

	public CSharpProperty( object containingInstance, SysReflect.PropertyInfo propertyInfo )
	{
		Assert( propertyInfo.PropertyType == typeof( T ) );
		ContainingInstance = containingInstance;
		this.propertyInfo = propertyInfo;
	}

	///<remarks>
	///PEARL: `PropertyInfo.SetValue()` will succeed in setting the value of a property even if the setter of the property is private!
	///This means that you may make the setter of your property private, and be under the impression that your property can only be set by the
	///declaring object, but but you will be wrong, because the property will in fact be settable by any other object that obtains the `PropertyInfo`
	///of your property and invokes `PropertyInfo.SetValue()` on it.
	///And unfortunately, that's precisely what <see cref="EchoingProperty{T}" /> does.
	///Luckily, `PropertyInfo` has a `GetSetMethod()` method which will return `null` if the setter of the property is private, so we can use it here to
	///guard against any attempts to set the value of a property whose setter is private.
	///</remarks>
	public T? Value
	{
		get => (T?)propertyInfo.GetValue( ContainingInstance )!;
		set
		{
			Assert( propertyInfo.GetSetMethod() != null, () => throw Failure( $"Property {ContainingInstance.GetType()}.{propertyInfo.Name} is not settable!" ) );
			propertyInfo.SetValue( ContainingInstance, value );
		}
	}

	public override string ToString() => ContainingInstance.GetType() + "." + propertyInfo.Name;
}
