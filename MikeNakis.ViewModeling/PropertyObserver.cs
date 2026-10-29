namespace MikeNakis.ViewModeling;

using LinqExpr = System.Linq.Expressions;
using Sys = System;

public interface PropertyObserver
{
	PropertyObserver Observes<E>( LinqExpr.Expression<Sys.Func<E>> propertyExpression );
}
