namespace MikeNakis.ViewModeling.Localization;

using System.Collections.Generic;
using MikeNakis.Kit.Extensions;
using Sys = System;
using SysText = System.Text;

public sealed class Translator
{
	readonly Sys.Func<Wording, string?> dictionary;
	readonly Sys.Action<TranslatorException> warningConsumer;

	public Translator( Sys.Func<Wording, string?> dictionary, Sys.Action<TranslatorException> warningConsumer )
	{
		this.dictionary = dictionary;
		this.warningConsumer = warningConsumer;
	}

	public string Translate( Wording wording, IReadOnlyList<string> parameters )
	{
		string? text = dictionary.Invoke( wording );
		if( text == null )
		{
			warningConsumer.Invoke( new WordingNotFoundTranslatorException( wording ) );
			return wording.Name;
		}

		if( !text.Contains2( '@' ) ) //optimization: avoid creating and populating a StringBuilder if the text will be unchanged.
			return text;

		SysText.StringBuilder stringBuilder = new();
		for( int i = 0; i < text.Length; )
		{
			char c = text[i++];
			if( c == '@' )
			{
				(string s, i) = lookup( text, i, parameters );
				stringBuilder.Append( s );
			}
			else
				stringBuilder.Append( c );
		}
		return stringBuilder.ToString();
	}

	(string, int) lookup( string text, int i, IReadOnlyList<string> parameters )
	{
		if( i >= text.Length )
			return fail( null, i, new IncompleteSequenceTranslatorException( text ) );
		char c = text[i++];
		if( c == '@' )
			return ("@@", i);
		int parameterIndex = "123456789".IndexOf2( c ); //for convenience, we only support single-digit parameter indexes. If you need more, split them over multiple wordings.
		if( parameterIndex == -1 )
			return fail( c, i, new InvalidSequenceTranslatorException( text, c ) );
		if( parameterIndex >= parameters.Count )
			return fail( c, i, new OutOfRangeSequenceTranslatorException( text, c, parameters.Count ) );
		return (parameters[parameterIndex], i);

		(string, int) fail( char? character, int i, TranslatorException sequenceTranslatorException )
		{
			warningConsumer.Invoke( sequenceTranslatorException );
			return ($"@{character}", i);
		}
	}
}
