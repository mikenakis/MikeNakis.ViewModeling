namespace MikeNakis.ViewModeling;

using MikeNakis.Kit.Codecs;

public readonly record struct ClipboardFormat( string Value )
{
	public static readonly Codec<ClipboardFormat> Codec = new StringRepresentationCodec<ClipboardFormat>( s => new( s ), f => f.Value );
}
