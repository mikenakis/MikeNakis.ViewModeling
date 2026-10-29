namespace MikeNakis.ViewModeling.Wpf;

using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using SysCompiler = System.Runtime.CompilerServices;
using SysDiag = System.Diagnostics;
using Wpf = System.Windows;
using WpfThreading = System.Windows.Threading;

public static class WpfPlatformHelpers
{
	public static void ErrorMessageBox( string message )
	{
		string caption = DotNetHelpers.GetProductName();
		// WPF PEARL: if we do not currently have a main window, the message box does not show and no error is generated!
		// Workaround is to add the MessageBoxOptions.DefaultDesktopOnly option.
		// See https://stackoverflow.com/a/36050732/773113
		Wpf.MessageBox.Show( message, caption, Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Error, Wpf.MessageBoxResult.OK, Wpf.MessageBoxOptions.DefaultDesktopOnly );
	}

	public static void ReportError( string message, [SysCompiler.CallerFilePath] string file = "", [SysCompiler.CallerLineNumber] int line = 0 )
	{
		Log.Info( $"Error reported to user: {message}", file, line );
		ErrorMessageBox( message );
	}

	public static T InvokeSafe<T>( string operationName, Sys.Func<T> function )
	{
		if( SysDiag.Debugger.IsAttached )
			return function.Invoke();
		try
		{
			return function.Invoke();
		}
		catch( Sys.Exception exception )
		{
			string message = $"Operation '{operationName}' failed";
			Log.Error( message, exception );
			ReportError( KitHelpers.BuildShortExceptionMessage( message, exception ) );
			return default!; //to keep the compiler happy
		}
	}

	public static WpfThreading.Dispatcher Dispatcher => Wpf.Application.Current.Dispatcher.OrThrow();

	public static void Post( string operationName, Sys.Action procedure )
	{
		Dispatcher.BeginInvoke( WpfThreading.DispatcherPriority.Background, () =>
		{
			object? result = InvokeSafe<object?>( operationName, () =>
			{
				procedure.Invoke();
				return null;
			} );
			Assert( result == null );
		} );
	}
}
