namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.CSharpTypeNames.Extensions;
using MikeNakis.Kit;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

sealed class Features : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly MutableBidictionary<string, Feature> mutableBiDictionary = new();
	public IEnumerable<Feature> All => mutableBiDictionary.Reverse.Keys;

	/// <summary>Constructor.</summary>
	public Features()
	{ }

	public void /*Sys.IDisposable.*/ Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		foreach( Feature feature in All )
			feature.Dispose();
		mutableBiDictionary.Clear();
		lifeGuard.Dispose();
	}

	public string GetFeatureName( Feature feature )
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return mutableBiDictionary.Reverse[feature];
	}

	public bool TryGetFeature<T>( string name, out T result ) where T : Feature
	{
		Assert( lifeGuard.IsAliveAssertion() );
		Feature? feature = mutableBiDictionary.Forward.TryGet( name );
		if( feature == null )
		{
			result = default!;
			return false;
		}
		Assert( feature is T, () => throw Failure( $"property '{name}' is {feature.GetType().GetCSharpName()} expected {typeof( T ).GetCSharpName()}" ) );
		result = (T)feature;
		return true;
	}

	public IEnumerable<T> GetFeatures<T>() where T : Feature => All.OfType<T>();

	public void AddFeature( string name, Feature feature )
	{
		Assert( lifeGuard.IsAliveAssertion() );
		mutableBiDictionary.Forward.Add( name, feature );
	}

	public void RemoveFeature( Feature feature )
	{
		Assert( lifeGuard.IsAliveAssertion() );
		Assert( mutableBiDictionary.Reverse.ContainsKey( feature ) );
		mutableBiDictionary.Reverse.Remove( feature );
		feature.Dispose();
	}

	public bool ContainsFeature( Feature feature )
	{
		Assert( lifeGuard.IsAliveAssertion() );
		return mutableBiDictionary.Reverse.ContainsKey( feature );
	}

	public override string ToString()
	{
		return mutableBiDictionary.Count + " features";
	}
}
