namespace MikeNakis.ViewModeling.Fake;

using System.Collections.Generic;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using MikeNakis.ViewModeling;
using MikeNakis.ViewModeling.Localization;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;

public sealed class FakePrompts : Prompts
{
	public FakePrompts()
	{ }

	///////////////////////////////////////////////

	public delegate MessagePromptResponse MessagePromptHandler( MessageFakePromptParameters fakePromptParameters );

	public static readonly MessagePromptHandler YesOkMessagePromptHandler = static _ => MessagePromptResponse.YesOk;

	public MessagePromptHandler? CurrentMessagePromptHandler { get; private set; }

	public Sys.IDisposable FakeMessagePrompt( MessagePromptHandler newPromptHandler )
	{
		MessagePromptHandler? oldPromptHandler = CurrentMessagePromptHandler;
		CurrentMessagePromptHandler = newPromptHandler;
		return new MakeshiftDisposable( () => CurrentMessagePromptHandler = oldPromptHandler );
	}

	///////////////////////////////////////////////

	public delegate bool ModalDialogHandler( ModalFakePromptParameters fakePromptParameters );

	public static readonly ModalDialogHandler EmptyModalDialogHandler = static _ =>
		{
			Assert( false ); //a prompt was shown but no handler has been set.
			return default;
		};

	public static readonly ModalDialogHandler AffirmativeModalDialogHandler = static _ => true;
	public static readonly ModalDialogHandler NegativeModalDialogHandler = static _ => false;

	public ModalDialogHandler CurrentModalDialogHandler { get; private set; } = EmptyModalDialogHandler;

	public Sys.IDisposable FakeDialogPrompt( ModalDialogHandler newPromptHandler )
	{
		ModalDialogHandler oldPromptHandler = CurrentModalDialogHandler;
		CurrentModalDialogHandler = newPromptHandler;
		return new MakeshiftDisposable( () => CurrentModalDialogHandler = oldPromptHandler );
	}

	///////////////////////////////////////////////

	public delegate FileDialogPromptResult? FileSavePromptHandler( FileSaveFakePromptParameters fakePromptParameters );

	public static readonly FileSavePromptHandler EmptyFileSavePromptHandler = static _ =>
		{
			Assert( false ); //a prompt was shown but no handler has been set.
			return default;
		};

	public FileSavePromptHandler CurrentFileSavePromptHandler { get; private set; } = EmptyFileSavePromptHandler;

	public Sys.IDisposable FakeFileSavePrompt( FileSavePromptHandler newPromptHandler )
	{
		FileSavePromptHandler oldPromptHandler = CurrentFileSavePromptHandler;
		CurrentFileSavePromptHandler = newPromptHandler;
		return new MakeshiftDisposable( () => CurrentFileSavePromptHandler = oldPromptHandler );
	}

	///////////////////////////////////////////////

	public delegate FileDialogPromptResult? FileOpenPromptHandler( FileOpenFakePromptParameters fakePromptParameters );

	public static readonly FileOpenPromptHandler EmptyFileOpenPromptHandler = static _ =>
		{
			Assert( false ); //a prompt was shown but no handler has been set.
			return default;
		};

	public FileOpenPromptHandler CurrentFileOpenPromptHandler { get; private set; } = EmptyFileOpenPromptHandler;

	public Sys.IDisposable FakeFileOpenPrompt( FileOpenPromptHandler newPromptHandler )
	{
		FileOpenPromptHandler oldPromptHandler = CurrentFileOpenPromptHandler;
		CurrentFileOpenPromptHandler = newPromptHandler;
		return new MakeshiftDisposable( () => CurrentFileOpenPromptHandler = oldPromptHandler );
	}

	////////////////////////////////////////////////

	MessagePromptResponse Prompts.DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, Wording wording, IReadOnlyList<string> parameters )
	{
		Assert( CurrentMessagePromptHandler != null, () => throw Failure( $"a prompt is being shown but no handler has been set for {wording}" ) );
		return CurrentMessagePromptHandler!.Invoke( new MessageFakePromptParameters( wording, parameters, type, style, defaultResponse ) );
	}

	MessagePromptResponse Prompts.DoMessageBox( MessageBoxType type, MessageBoxStyle style, MessagePromptResponse defaultResponse, string message )
	{
		Assert( CurrentMessagePromptHandler != null, () => throw Failure( message ) ); //a prompt is being shown but no handler has been set.
		Assert( false, () => throw Failure( message ) ); //any code exercised by tests should be using message boxes of the other form which accept a Wording instead of a string for the message.
		return MessagePromptResponse.Cancel;
	}

	bool Prompts.DoModalDialog( ModalDialogViewModel viewModel )
	{
		return CurrentModalDialogHandler.Invoke( new ModalFakePromptParameters( viewModel ) );
	}

	FileDialogPromptResult? Prompts.DoFileSaveDialog( SaveFileDialogPromptInfo saveFileDialogPromptInfo )
	{
		return CurrentFileSavePromptHandler.Invoke( new FileSaveFakePromptParameters( saveFileDialogPromptInfo ) );
	}

	FileDialogPromptResult? Prompts.DoFileOpenDialog( OpenFileDialogPromptInfo openFileDialogPromptInfo )
	{
		return CurrentFileOpenPromptHandler.Invoke( new FileOpenFakePromptParameters( openFileDialogPromptInfo ) );
	}
}

public class FakePromptParameters
{ }

public class FileOpenFakePromptParameters : FakePromptParameters
{
	public OpenFileDialogPromptInfo OpenFileDialogPromptInfo { get; }

	public FileOpenFakePromptParameters( OpenFileDialogPromptInfo openFileDialogPromptInfo )
	{
		OpenFileDialogPromptInfo = openFileDialogPromptInfo;
	}
}

public class FileSaveFakePromptParameters : FakePromptParameters
{
	public SaveFileDialogPromptInfo SaveFileDialogPromptInfo { get; }

	public FileSaveFakePromptParameters( SaveFileDialogPromptInfo saveFileDialogPromptInfo )
	{
		SaveFileDialogPromptInfo = saveFileDialogPromptInfo;
	}
}

public class ModalFakePromptParameters : FakePromptParameters
{
	public DialogViewModel ViewModel { get; }

	public ModalFakePromptParameters( DialogViewModel viewModel )
	{
		ViewModel = viewModel;
	}
}

public class MessageFakePromptParameters : FakePromptParameters
{
	public Wording Wording { get; }
	public IReadOnlyList<string> Parameters { get; }
	public MessageBoxType Type { get; }
	public MessageBoxStyle Style { get; }
	public MessagePromptResponse? DefaultResponse { get; }

	public MessageFakePromptParameters( Wording wording, IReadOnlyList<string> parameters, MessageBoxType type, MessageBoxStyle style, MessagePromptResponse? defaultResponse )
	{
		Wording = wording;
		Parameters = parameters;
		Type = type;
		Style = style;
		DefaultResponse = defaultResponse;
	}

	public override string ToString()
	{
		return $"{Wording.Name} {Type} {Style} {Parameters.MakeString( ", " )}";
	}
}
