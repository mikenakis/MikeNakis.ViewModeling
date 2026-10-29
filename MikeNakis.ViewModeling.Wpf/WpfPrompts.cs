namespace MikeNakis.ViewModeling.Wpf;

using System.Collections.Generic;
using System.Linq;
using MikeNakis.Kit;
using MikeNakis.Kit.Collections;
using MikeNakis.Kit.Extensions;
using MikeNakis.Kit.FileSystem;
using MikeNakis.ViewModeling.Localization;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using Wpf = System.Windows;
using WpfInput = System.Windows.Input;

sealed class WpfPrompts : Prompts
{
	readonly WpfPlatform wpfPlatform;
	readonly DialogWindowViewFactory viewFactory;
	readonly Translator translator;

	internal WpfPrompts( WpfPlatform wpfPlatform, DialogWindowViewFactory viewFactory, Translator translator )
	{
		this.wpfPlatform = wpfPlatform;
		this.viewFactory = viewFactory;
		this.translator = translator;
	}

	public Prompts Prompts => this;

	MessagePromptResponse Prompts.DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, Wording wording, IReadOnlyList<string> parameters )
	{
		string message = translator.Translate( wording, parameters );
		return Prompts.DoMessageBox( type, style, defaultResponse, message );
	}

	MessagePromptResponse Prompts.DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string message )
	{
		return exitLogic( () => doMessageBox( type, style, defaultResponse, message ) );
	}

	static MessagePromptResponse doMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string message )
	{
		string caption = DotNetHelpers.GetProductName();
		Log.Info( $"Message box: type={type}, style={style}, message=\"{message}\"" );
		// if( Wpf.Application.Current.Windows.OfType<MahApps.Metro.Controls.MetroWindow>().FirstOrDefault() != null )
		// 	return doMahMessageBox( type, style, defaultResponse, caption, message );
		return doWpfMessageBox( type, style, defaultResponse, caption, message );
	}

	// TODO: Make use of MessageBoxStyle to show an appropriate icon (Warning, Information, Question)
	// ReSharper disable once UnusedParameter.Local
	// private static MessagePromptResponse doMahMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string title, string message )
	// {
	// 	MahApps.Metro.Controls.MetroWindow parentMetroWindow = Wpf.Application.Current.Windows.OfType<MahApps.Metro.Controls.MetroWindow>().First();
	// 	var metroDialogSettings = new MahApps.Metro.Controls.Dialogs.MetroDialogSettings();
	// 	metroDialogSettings.ColorScheme = MahApps.Metro.Controls.Dialogs.MetroDialogColorScheme.Theme;
	// 	metroDialogSettings.DialogButtonFontSize = 14;
	// 	metroDialogSettings.DialogMessageFontSize = 16;
	// 	metroDialogSettings.DialogTitleFontSize = 84; //inapplicable; it is set by the template.
	// 	metroDialogSettings.OwnerCanCloseWithDialog = true; //does not appear to have any effect
	// 	metroDialogSettings.AnimateShow = true;
	// 	metroDialogSettings.AnimateHide = true;
	// 	metroDialogSettings.AffirmativeButtonText = mahAppsAffirmativeButtonTextFromMessageBoxType( type );
	// 	metroDialogSettings.NegativeButtonText = mahAppsNegativeButtonTextFromMessageBoxType( type );
	// 	metroDialogSettings.FirstAuxiliaryButtonText = mahAppsFirstAuxiliaryButtonTextFromMessageBoxType( type );
	// 	metroDialogSettings.DefaultButtonFocus = mahAppsDefaultButtonFocusFromMessageBoxTypeAndDefaultResponse( type, defaultResponse );
	// 	metroDialogSettings.DialogResultOnCancel = mahAppsDialogResultOnCancelFromMessageBoxType( type );
	// 	using( SysThreading.CancellationTokenSource tokenSource = new SysThreading.CancellationTokenSource() )
	// 	{
	// 		metroDialogSettings.CancellationToken = tokenSource.Token;
	// 		MahApps.Metro.Controls.Dialogs.MessageDialogStyle mahStyle = mahAppsStyleFromMessageBoxType( type );
	// 		SysTasks.Task<MahApps.Metro.Controls.Dialogs.MessageDialogResult> task = MahApps.Metro.Controls.Dialogs.DialogManager.ShowMessageAsync( parentMetroWindow, title, message, mahStyle, metroDialogSettings );
	// 		void cancelEventHandler( object? s, SysCompMod.CancelEventArgs e ) => tokenSource.Cancel();
	// 		parentMetroWindow.Closing += cancelEventHandler; //this is necessary in case the user tries to close the main window via the task bar.
	// 		//I am not sure what I was trying to do here, but it does not look right. TODO try replacing with task.Wait( tokenSource.Token )
	// 		while( !(task.IsCompleted || task.IsCanceled || task.IsFaulted) )
	// 			Wpf.Application.Current.Dispatcher.Invoke( WpfThreading.DispatcherPriority.Background, new Sys.Action( delegate { } ) );
	// 		parentMetroWindow.Closing -= cancelEventHandler;
	// 		return responseFromMahAppsMessageDialogResult( type, task.Result );
	// 	}
	// }
	//
	// private static MahApps.Metro.Controls.Dialogs.MessageDialogStyle mahAppsStyleFromMessageBoxType( MessageBoxType messageBoxType )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok: return MahApps.Metro.Controls.Dialogs.MessageDialogStyle.Affirmative;
	// 		case MessageBoxType.OkCancel: return MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative;
	// 		case MessageBoxType.YesNoCancel: return MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary;
	// 		default:
	// 			Assert( false, messageBoxType );
	// 			return MahApps.Metro.Controls.Dialogs.MessageDialogStyle.Affirmative;
	// 	}
	// }
	//
	// private static string mahAppsAffirmativeButtonTextFromMessageBoxType( MessageBoxType messageBoxType )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok: return "OK";
	// 		case MessageBoxType.OkCancel: return "OK";
	// 		case MessageBoxType.YesNoCancel: return "Yes";
	// 		default:
	// 			Assert( false, messageBoxType );
	// 			return "OK";
	// 	}
	// }
	//
	// private static string? mahAppsNegativeButtonTextFromMessageBoxType( MessageBoxType messageBoxType )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok: return null;
	// 		case MessageBoxType.OkCancel: return "Cancel";
	// 		case MessageBoxType.YesNoCancel: return "No";
	// 		default:
	// 			Assert( false, messageBoxType );
	// 			return "OK";
	// 	}
	// }
	//
	// private static string? mahAppsFirstAuxiliaryButtonTextFromMessageBoxType( MessageBoxType messageBoxType )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok: return null;
	// 		case MessageBoxType.OkCancel: return null;
	// 		case MessageBoxType.YesNoCancel: return "Cancel";
	// 		default:
	// 			Assert( false, messageBoxType );
	// 			return "OK";
	// 	}
	// }
	//
	// private static MahApps.Metro.Controls.Dialogs.MessageDialogResult? mahAppsDialogResultOnCancelFromMessageBoxType( MessageBoxType messageBoxType )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// 		case MessageBoxType.OkCancel: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative;
	// 		case MessageBoxType.YesNoCancel: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.FirstAuxiliary;
	// 		default:
	// 			Assert( false, messageBoxType );
	// 			break;
	// 	}
	// 	return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// }
	//
	// private static MahApps.Metro.Controls.Dialogs.MessageDialogResult mahAppsDefaultButtonFocusFromMessageBoxTypeAndDefaultResponse( MessageBoxType messageBoxType, MessagePromptResponse defaultResponse )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok:
	// 			Assert( defaultResponse == MessagePromptResponse.YesOk );
	// 			return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// 		case MessageBoxType.OkCancel:
	// 		{
	// 			switch( defaultResponse )
	// 			{
	// 				case MessagePromptResponse.YesOk: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// 				case MessagePromptResponse.Cancel: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative;
	// 			}
	// 			break;
	// 		}
	// 		case MessageBoxType.YesNoCancel:
	// 		{
	// 			switch( defaultResponse )
	// 			{
	// 				case MessagePromptResponse.YesOk: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// 				case MessagePromptResponse.No: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative;
	// 				case MessagePromptResponse.Cancel: return MahApps.Metro.Controls.Dialogs.MessageDialogResult.FirstAuxiliary;
	// 			}
	// 			break;
	// 		}
	// 	}
	// 	Assert( false, $"{messageBoxType} {defaultResponse}" );
	// 	return MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative;
	// }
	//
	// private static MessagePromptResponse responseFromMahAppsMessageDialogResult( MessageBoxType messageBoxType, MahApps.Metro.Controls.Dialogs.MessageDialogResult mahAppsMessageDialogResult )
	// {
	// 	switch( messageBoxType )
	// 	{
	// 		case MessageBoxType.Ok:
	// 		{
	// 			if( mahAppsMessageDialogResult == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative )
	// 				return MessagePromptResponse.YesOk;
	// 			break;
	// 		}
	// 		case MessageBoxType.OkCancel:
	// 		{
	// 			switch( mahAppsMessageDialogResult )
	// 			{
	// 				case MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative: return MessagePromptResponse.YesOk;
	// 				case MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative: return MessagePromptResponse.Cancel;
	// 			}
	// 			break;
	// 		}
	// 		case MessageBoxType.YesNoCancel:
	// 		{
	// 			switch( mahAppsMessageDialogResult )
	// 			{
	// 				case MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative: return MessagePromptResponse.YesOk;
	// 				case MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative: return MessagePromptResponse.No;
	// 				case MahApps.Metro.Controls.Dialogs.MessageDialogResult.FirstAuxiliary: return MessagePromptResponse.Cancel;
	// 			}
	// 			break;
	// 		}
	// 	}
	// 	Assert( false, $"{messageBoxType} {mahAppsMessageDialogResult}" );
	// 	return MessagePromptResponse.Cancel;
	// }

	static MessagePromptResponse doWpfMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string caption, string message )
	{
		Wpf.MessageBoxButton button = wpfMessageBoxButtonFromType( type );
		Wpf.MessageBoxImage image = wpfMessageBoxImageFromStyle( style );
		Wpf.MessageBoxResult defaultResult = wpfMessageBoxResultFromResponse( defaultResponse );
		Wpf.MessageBoxResult response = doWpfMessageBox( message, caption, button, image, defaultResult );
		return responseFromWpfMessageBoxResult( response );
	}

	/// PEARL: WPF does not have a MessageBox function that accepts an owner window which may be null, so we have to emulate this here.
	static Wpf.MessageBoxResult doWpfMessageBox( string message, string caption, Wpf.MessageBoxButton button, Wpf.MessageBoxImage image, Wpf.MessageBoxResult defaultResult )
	{
		Wpf.Window? mainWindow = Wpf.Application.Current.MainWindow;
		if( mainWindow == null )
			return Wpf.MessageBox.Show( message, caption, button, image, defaultResult, Wpf.MessageBoxOptions.DefaultDesktopOnly );
		return Wpf.MessageBox.Show( mainWindow, message, caption, button, image, defaultResult );
	}

	bool Prompts.DoModalDialog( ModalDialogViewModel viewModel )
	{
		Wpf.Window dialog = viewFactory.Invoke( viewModel );
		Assert( dialog.DataContext == null );
		dialog.DataContext = viewModel;
		Assert( dialog.Owner == null );
		dialog.Owner = Wpf.Application.Current.MainWindow;
		bool? result = exitLogic( dialog.ShowDialog );
		Assert( result.HasValue );
		return result.Value;
	}

	FileDialogPromptResult? Prompts.DoFileSaveDialog( SaveFileDialogPromptInfo info )
	{
		Assert( info.Filters.Count > 0 );
		Microsoft.Win32.SaveFileDialog fileDialog = new();
		fileDialog.Title = info.Title;
		fileDialog.Filter = info.Filters.Select( buildFilter ).MakeString( "|" );
		fileDialog.FilterIndex = 1;
		fileDialog.DefaultExt = info.Filters[0].Extension;
		fileDialog.ValidateNames = info.ValidateNames;
		fileDialog.CheckPathExists = info.CheckPathExists;
		fileDialog.CheckFileExists = info.CheckFileExists;
		fileDialog.AddExtension = info.AddExtension;
		if( info.CustomPlaces != null )
			fileDialog.CustomPlaces = new MutableList<Microsoft.Win32.FileDialogCustomPlace>( info.CustomPlaces.Select( s => new Microsoft.Win32.FileDialogCustomPlace( s ) ) ).AsReadOnlyList.ToArraySeriously();
		fileDialog.InitialDirectory = info.InitialDirectory;
		fileDialog.FileName = info.DefaultFilename;
		fileDialog.OverwritePrompt = info.OverwritePrompt;
		fileDialog.CreatePrompt = info.CreatePrompt;
		bool? result = exitLogic( fileDialog.ShowDialog );
		if( result == false )
			return null;
		return new FileDialogPromptResult( FilePath.FromAbsolutePath( fileDialog.FileName ), info.Filters[fileDialog.FilterIndex - 1] );
	}

	FileDialogPromptResult? Prompts.DoFileOpenDialog( OpenFileDialogPromptInfo info )
	{
		Assert( info.Filters.Count > 0 );
		Microsoft.Win32.OpenFileDialog fileDialog = new();
		fileDialog.Title = info.Title;
		fileDialog.Filter = info.Filters.Select( buildFilter ).MakeString( "|" );
		fileDialog.FilterIndex = 1;
		fileDialog.DefaultExt = info.Filters[0].Extension;
		fileDialog.ValidateNames = info.ValidateNames;
		fileDialog.CheckPathExists = info.CheckPathExists;
		fileDialog.CheckFileExists = info.CheckFileExists;
		fileDialog.AddExtension = info.AddExtension;
		if( info.CustomPlaces != null )
			fileDialog.CustomPlaces = new MutableList<Microsoft.Win32.FileDialogCustomPlace>( info.CustomPlaces.Select( s => new Microsoft.Win32.FileDialogCustomPlace( s ) ) ).AsReadOnlyList.ToArraySeriously();
		fileDialog.InitialDirectory = info.InitialDirectory;
		fileDialog.Multiselect = false;
		fileDialog.ShowReadOnly = info.ShowReadOnly;
		fileDialog.ReadOnlyChecked = info.ReadOnlyChecked;
		bool? result = exitLogic( fileDialog.ShowDialog );
		if( result == false )
			return null;
		return new FileDialogPromptResult( FilePath.FromAbsolutePath( fileDialog.FileName ), info.Filters[fileDialog.FilterIndex - 1] );
	}

	static Wpf.MessageBoxButton wpfMessageBoxButtonFromType( MessageBoxType type )
	{
		return type switch
		{
			MessageBoxType.Ok => Wpf.MessageBoxButton.OK,
			MessageBoxType.OkCancel => Wpf.MessageBoxButton.OKCancel,
			MessageBoxType.YesNoCancel => Wpf.MessageBoxButton.YesNoCancel,
			_ => throw new Sys.ArgumentException( KitHelpers.SafeToString( type ), nameof( type ) )
		};
	}

	static Wpf.MessageBoxImage wpfMessageBoxImageFromStyle( MessageBoxStyle style )
	{
		return style switch
		{
			MessageBoxStyle.None => Wpf.MessageBoxImage.None,
			MessageBoxStyle.Information => Wpf.MessageBoxImage.Information,
			MessageBoxStyle.Warning => Wpf.MessageBoxImage.Exclamation,
			MessageBoxStyle.Error => Wpf.MessageBoxImage.Error,
			MessageBoxStyle.Question => Wpf.MessageBoxImage.Question,
			_ => throw new Sys.ArgumentException( KitHelpers.SafeToString( style ), nameof( style ) )
		};
	}

	static Wpf.MessageBoxResult wpfMessageBoxResultFromResponse( MessagePromptResponse response )
	{
		return response switch
		{
			MessagePromptResponse.YesOk => Wpf.MessageBoxResult.OK,
			MessagePromptResponse.Cancel => Wpf.MessageBoxResult.Cancel,
			MessagePromptResponse.No => Wpf.MessageBoxResult.No,
			_ => throw new Sys.ArgumentException( KitHelpers.SafeToString( response ), nameof( response ) ),
		};
	}

	static MessagePromptResponse responseFromWpfMessageBoxResult( Wpf.MessageBoxResult messageBoxResult )
	{
		return messageBoxResult switch
		{
			Wpf.MessageBoxResult.OK => MessagePromptResponse.YesOk,
			Wpf.MessageBoxResult.Cancel => MessagePromptResponse.Cancel,
			Wpf.MessageBoxResult.Yes => MessagePromptResponse.YesOk,
			Wpf.MessageBoxResult.No => MessagePromptResponse.No,
			_ => throw new Sys.ArgumentException( KitHelpers.SafeToString( messageBoxResult ), nameof( messageBoxResult ) ),
		};
	}

	static string buildFilter( FileDialogFilter filter )
	{
		string extension = filter.Extension == "" ? "*" : filter.Extension;
		return $"{filter.Description} (*.{extension})|*.{extension}";
	}

	T exitLogic<T>( Sys.Func<T> function )
	{
		Assert( wpfPlatform.EventDriver.InThreadAssertion() );
		using( new WpfCursorConservator( WpfInput.Cursors.Arrow ) )
			return function.Invoke();
	}
}
