namespace MikeNakis.ViewModeling;

public abstract class Clipboard
{
	public abstract bool Contains( ClipboardFormat clipboardFormat );
	public abstract string? GetContent( ClipboardFormat clipboardFormat );
	public abstract void PutContent( ClipboardFormat clipboardFormat, string content );
}
