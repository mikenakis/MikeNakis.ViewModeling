namespace MikeNakis.ViewModeling.Fake;

using MikeNakis.ViewModeling;
using static MikeNakis.Kit.GlobalStatics;

sealed class FakeClipboard : Clipboard
{
	ClipboardFormat? clipboardFormat;
	string? clipboardContent;

	internal FakeClipboard()
	{ }

	public override bool Contains( ClipboardFormat clipboardFormat )
	{
		return clipboardFormat == this.clipboardFormat;
	}

	public override string GetContent( ClipboardFormat clipboardFormat )
	{
		Assert( clipboardFormat == this.clipboardFormat );
		Assert( clipboardContent != null );
		return clipboardContent!;
	}

	public override void PutContent( ClipboardFormat clipboardFormat, string content )
	{
		this.clipboardFormat = clipboardFormat;
		clipboardContent = content;
	}
}
