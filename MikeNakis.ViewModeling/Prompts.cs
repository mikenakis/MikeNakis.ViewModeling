namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using Localization;
using MikeNakis.Kit.FileSystem;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public enum MessageBoxType
{
	Ok,
	OkCancel,
	YesNoCancel,
}

public enum MessageBoxStyle
{
	None,
	Information,
	Question,
	Warning,
	Error
}

public enum MessagePromptResponse
{
	YesOk,
	Cancel,
	No
}

public class FileDialogFilter
{
	public string Description { get; }
	public string Extension { get; }

	public FileDialogFilter( string description, string extension )
	{
		Assert( !string.IsNullOrWhiteSpace( description ) );
		Assert( extension == "" || extension[0] != ' ' && extension[0] != '.' && extension[0] != '*' );
		Description = description;
		Extension = extension;
	}
}

public class FileDialogPromptInfo
{
	public string Title { get; }
	public IReadOnlyList<FileDialogFilter> Filters { get; }
	public IReadOnlyList<string>? CustomPlaces { get; }
	public string InitialDirectory { get; }
	public string DefaultFilename { get; }
	public bool ValidateNames { get; }
	public bool CheckPathExists { get; }
	public bool CheckFileExists { get; }
	public bool AddExtension { get; }

	public FileDialogPromptInfo( string title, IReadOnlyList<FileDialogFilter> filters, IReadOnlyList<string>? customPlaces = null, //
			string initialDirectory = "", string defaultFilename = "", bool validateNames = true, bool checkPathExists = true, bool checkFileExists = false, //
			bool addExtension = true )
	{
		Title = title;
		Filters = filters;
		CustomPlaces = customPlaces;
		InitialDirectory = initialDirectory;
		DefaultFilename = defaultFilename;
		ValidateNames = validateNames;
		CheckPathExists = checkPathExists;
		CheckFileExists = checkFileExists;
		AddExtension = addExtension;
	}
}

public class FileDialogPromptResult
{
	public FilePath PathName { get; }
	public FileDialogFilter SelectedFilter { get; }

	public FileDialogPromptResult( FilePath pathName, FileDialogFilter selectedFilter )
	{
		PathName = pathName;
		SelectedFilter = selectedFilter;
	}
}

public class SaveFileDialogPromptInfo : FileDialogPromptInfo
{
	public bool OverwritePrompt { get; }
	public bool CreatePrompt { get; }

	public SaveFileDialogPromptInfo( string title, IReadOnlyList<FileDialogFilter> filters, IReadOnlyList<string>? customPlaces = null, //
			string initialDirectory = "", string defaultFilename = "", bool validateNames = true, bool checkPathExists = true, bool checkFileExists = false, bool addExtension = true, //
			bool overwritePrompt = true, bool createPrompt = false )
			: base( title, filters, customPlaces, initialDirectory, defaultFilename, validateNames, checkPathExists, checkFileExists, addExtension )
	{
		OverwritePrompt = overwritePrompt;
		CreatePrompt = createPrompt;
	}
}

public class OpenFileDialogPromptInfo : FileDialogPromptInfo
{
	public bool ShowReadOnly { get; }
	public bool ReadOnlyChecked { get; }

	public OpenFileDialogPromptInfo( string title, IReadOnlyList<FileDialogFilter> filters, IReadOnlyList<string>? customPlaces = null, //
			string initialDirectory = "", string defaultFilename = "", bool validateNames = true, bool checkPathExists = true, bool checkFileExists = false, bool addExtension = true, //
			bool showReadOnly = false, bool readOnlyChecked = false )
			: base( title, filters, customPlaces, initialDirectory, defaultFilename, validateNames, checkPathExists, checkFileExists, addExtension )
	{
		ShowReadOnly = showReadOnly;
		ReadOnlyChecked = readOnlyChecked;
	}
}

public interface Prompts
{
	MessagePromptResponse DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, Wording wording, IReadOnlyList<string> parameters );
	MessagePromptResponse DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string message );

	bool DoModalDialog( ModalDialogViewModel viewModel );

	FileDialogPromptResult? DoFileSaveDialog( SaveFileDialogPromptInfo saveFileDialogPromptInfo );

	FileDialogPromptResult? DoFileOpenDialog( OpenFileDialogPromptInfo openFileDialogPromptInfo );
}

///<summary>Default Extensions for <see cref="Prompts"/>.</summary>
public static class PromptsDefaultExtensions
{
	/// <summary>
	/// NOTE: This code was written back when we were using traditional messageBox which has an icon in it.
	/// The Metro-style messageBox that we are using now does not have icons, but we should be add them one day.<br/>
	/// 4 icons are needed:<br/>
	/// Error:       "x" in a red circle        (a.k.a. "Hand", "Stop")<br/>
	/// Question:    "?" in a blue circle<br/>
	/// Warning:     "!" in a yellow triangle.  (a.k.a. "Exclamation")<br/>
	/// Information: "i" in a blue circle.      (a.k.a. "Asterisk")<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/api/system.windows.messageboximage?view=netframework-4.8
	/// </summary>
	public static void OkInformationPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Information, MessagePromptResponse.YesOk, message );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkInformationPrompt( this Prompts self, Wording wording, params string[] parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Information, MessagePromptResponse.YesOk, wording, parameters );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkWarningPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Warning, MessagePromptResponse.YesOk, message );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkWarningPrompt( this Prompts self, Wording wording, params string[] parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Warning, MessagePromptResponse.YesOk, wording, parameters );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkErrorPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Error, MessagePromptResponse.YesOk, message );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkErrorPrompt( this Prompts self, Wording wording, IReadOnlyList<string> parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.Ok, MessageBoxStyle.Error, MessagePromptResponse.YesOk, wording, parameters );
		Assert( response == MessagePromptResponse.YesOk );
	}

	public static void OkErrorPrompt( this Prompts self, Wording wording, params string[] parameters )
	{
		self.OkErrorPrompt( wording, (IReadOnlyList<string>)parameters );
	}

	public static bool OkCancelQuestionPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.OkCancel, MessageBoxStyle.Question, MessagePromptResponse.Cancel, message );
		Assert( response is MessagePromptResponse.YesOk or MessagePromptResponse.Cancel );
		return response == MessagePromptResponse.YesOk;
	}

	public static bool OkCancelQuestionPrompt( this Prompts self, Wording text, params string[] parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.OkCancel, MessageBoxStyle.Question, MessagePromptResponse.Cancel, text, parameters );
		Assert( response is MessagePromptResponse.YesOk or MessagePromptResponse.Cancel );
		return response == MessagePromptResponse.YesOk;
	}

	public static bool OkCancelWarningPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.OkCancel, MessageBoxStyle.Warning, MessagePromptResponse.Cancel, message );
		Assert( response is MessagePromptResponse.YesOk or MessagePromptResponse.Cancel );
		return response == MessagePromptResponse.YesOk;
	}

	public static bool OkCancelWarningPrompt( this Prompts self, Wording wording, params string[] parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.OkCancel, MessageBoxStyle.Warning, MessagePromptResponse.Cancel, wording, parameters );
		Assert( response is MessagePromptResponse.YesOk or MessagePromptResponse.Cancel );
		return response == MessagePromptResponse.YesOk;
	}

	public static bool? YesNoCancelQuestionPrompt( this Prompts self, string message )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.YesNoCancel, MessageBoxStyle.Question, MessagePromptResponse.Cancel, message );
		switch( response )
		{
			case MessagePromptResponse.YesOk:
				return true;
			case MessagePromptResponse.No:
				return false;
			case MessagePromptResponse.Cancel:
				return null;
			default:
				Assert( false );
				return null;
		}
	}

	public static bool? YesNoCancelQuestionPrompt( this Prompts self, Wording text, params string[] parameters )
	{
		MessagePromptResponse response = self.DoMessageBox( MessageBoxType.YesNoCancel, MessageBoxStyle.Question, MessagePromptResponse.Cancel, text, parameters );
		switch( response )
		{
			case MessagePromptResponse.YesOk:
				return true;
			case MessagePromptResponse.No:
				return false;
			case MessagePromptResponse.Cancel:
				return null;
			default:
				Assert( false );
				return null;
		}
	}

	public static FileDialogPromptResult? DoFileSaveDialog( this Prompts self, string title, IReadOnlyList<FileDialogFilter> filters, //
			IReadOnlyList<string>? customPlaces = null, string initialDirectory = "", string defaultFilename = "", bool validateNames = true, bool checkPathExists = true, //
			bool checkFileExists = false, bool addExtension = true, bool overwritePrompt = true, bool createPrompt = false )
	{
		SaveFileDialogPromptInfo info = new( title, filters, customPlaces, initialDirectory, defaultFilename, validateNames, checkPathExists, checkFileExists, addExtension, //
				overwritePrompt, createPrompt );
		return self.DoFileSaveDialog( info );
	}

	public static FileDialogPromptResult? DoFileOpenDialog( this Prompts self, string title, IReadOnlyList<FileDialogFilter> filters, //
			IReadOnlyList<string>? customPlaces = null, string initialDirectory = "", string defaultFilename = "", bool validateNames = true, bool checkPathExists = true, //
			bool checkFileExists = false, bool addExtension = true, bool showReadOnly = false, bool readOnlyChecked = false )
	{
		OpenFileDialogPromptInfo info = new( title, filters, customPlaces, initialDirectory, defaultFilename, validateNames, checkPathExists, checkFileExists, addExtension, //
				showReadOnly, readOnlyChecked );
		return self.DoFileOpenDialog( info );
	}

	public static T? Try<T>( this Prompts self, string operationName, Sys.Func<T> operation ) where T : class
	{
		try
		{
			return operation.Invoke();
		}
		catch( Sys.Exception exception )
		{
			self.OkErrorPrompt( $"{operationName} failed:\r\n{exception.Message}" );
			return null;
		}
	}
}
