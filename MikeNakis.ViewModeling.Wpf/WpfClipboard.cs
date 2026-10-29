namespace MikeNakis.ViewModeling.Wpf;

using Wpf = System.Windows;

sealed class WpfClipboard : Clipboard
{
	internal WpfClipboard()
	{ }

	public override bool Contains( ClipboardFormat clipboardFormat )
	{
		return Wpf.Clipboard.ContainsData( clipboardFormat.Value );
	}

	public override string? GetContent( ClipboardFormat clipboardFormat )
	{
		return (string?)Wpf.Clipboard.GetData( clipboardFormat.Value );
	}

	public override void PutContent( ClipboardFormat clipboardFormat, string content )
	{
		Wpf.Clipboard.SetData( clipboardFormat.Value, content );
	}
}
