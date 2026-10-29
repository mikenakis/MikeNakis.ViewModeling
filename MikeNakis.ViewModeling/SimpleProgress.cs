namespace MikeNakis.ViewModeling;

using System.Linq;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;

public class SimpleProgress
{
	readonly struct ChildProgressInfo
	{
		public readonly SimpleProgress Child { get; }
		public readonly double Weight { get; }

		public ChildProgressInfo( SimpleProgress child, double weight )
		{
			Child = child;
			Weight = weight;
		}
	}

	double totalWeights;
	double ratio; //0.0 = nothing done yet; 1.0 = job is complete.
	bool isCancelled;
	readonly object lockObject = new();
	readonly SimpleProgress? parent;
	readonly MutableList<ChildProgressInfo> children = new();
	readonly bool allowChildren;

	public SimpleProgress( SimpleProgress? parent = null, bool allowChildren = true )
	{
		this.parent = parent;
		this.allowChildren = allowChildren;
	}

	public SimpleProgress CreateChildProgress( double weight )
	{
		SimpleProgress child = new( this );
		AddChildProgress( child, weight );
		return child;
	}

	public void AddChildProgress( SimpleProgress child, double weight )
	{
		Assert( weight > 0 );
		Assert( ratio == 0 ); // progress should not have been used before if it is used as a distributor
		Assert( allowChildren );
		Assert( child.parent.ReferenceEquals( this ) );
		totalWeights += weight;
		lock( lockObject )
			children.Add( new ChildProgressInfo( child, weight ) );
	}

	public virtual double Ratio
	{
		get
		{
			lock( lockObject )
				return children.IsEmpty() ? ratio : children.AsReadOnlyList.Select( c => c.Child.Ratio * c.Weight ).Sum() / totalWeights;
		}
		set
		{
			Assert( value is >= 0.0 and <= 1.0, () => throw Failure( $"value={value}" ) );
			Assert( !HasChildren );
			if( value.NearlyEquals( ratio ) )
				return;
			Assert( value >= ratio );
			ratio = value;
		}
	}

	public bool HasChildren
	{
		get
		{
			lock( lockObject )
				return children.Count > 0;
		}
	}

	public bool SetRatioAndGetIsCancelled( double ratio )
	{
		Ratio = ratio;
		return IsCancelled;
	}

	public void Complete()
	{
		lock( lockObject )
			if( children.Count > 0 )
				foreach( ChildProgressInfo ci in children )
					ci.Child.Complete();
			else
				Ratio = 1.0;
	}

	public bool IsCancelled { get => parent?.IsCancelled ?? isCancelled; private set => isCancelled = value; }

	public void Cancel()
	{
		Assert( !isCancelled ); //already cancelled
		IsCancelled = true;
	}

	public void Clear()
	{
		ResetRatio();
		IsCancelled = false;
	}

	public void ResetRatio()
	{
		ratio = -1.0; //before setting Ratio to 0, directly set ratio to a smaller value in order to temporarily suppress validation.
		Ratio = 0.0;
	}
}
