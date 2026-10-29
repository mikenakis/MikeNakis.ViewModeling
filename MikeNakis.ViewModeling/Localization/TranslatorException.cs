namespace MikeNakis.ViewModeling.Localization;

using MikeNakis.Kit;

public abstract class TranslatorException : SaneException
{ }

public class WordingNotFoundTranslatorException : TranslatorException
{
	public Wording Wording { get; }

	public WordingNotFoundTranslatorException( Wording wording )
	{
		Wording = wording;
	}

	public override string Message => $"Wording not found: '{Wording.Name}'";
}

public abstract class SequenceTranslatorException : TranslatorException
{
	public string Text { get; }

	protected SequenceTranslatorException( string text )
	{
		Text = text;
	}

	public override string Message => $"{OnGetMessage()} in \"{Text}\"";

	protected abstract string OnGetMessage();
}

public class IncompleteSequenceTranslatorException : SequenceTranslatorException
{
	public IncompleteSequenceTranslatorException( string text )
			: base( text )
	{ }

	protected override string OnGetMessage() => "Incomplete sequence at end of text";
}

public class InvalidSequenceTranslatorException : SequenceTranslatorException
{
	public char Sequence { get; }

	public InvalidSequenceTranslatorException( string text, char sequence )
			: base( text )
	{
		Sequence = sequence;
	}

	protected override string OnGetMessage() => $"Invalid sequence '@{Sequence}'";
}

public class OutOfRangeSequenceTranslatorException : SequenceTranslatorException
{
	public char Sequence { get; }
	public int ParameterCount { get; }

	public OutOfRangeSequenceTranslatorException( string text, char sequence, int parameterCount )
			: base( text )
	{
		Sequence = sequence;
		ParameterCount = parameterCount;
	}

	protected override string OnGetMessage() => $"Sequence @{Sequence} is outside of range [1..{ParameterCount}]";
}
