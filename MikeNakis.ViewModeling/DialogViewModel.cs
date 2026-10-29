namespace MikeNakis.ViewModeling;

// TODO: Do something with this.
// As it stands right now, it is empty, so it is unneeded, but a good use for it would be to move into it all duplicated functionality from dismissible
// ViewModels, add a corresponding DialogView, and use the two of them in every place where we have views with OK and Cancel buttons.
public abstract class DialogViewModel : ValidatedViewModel
{
	/// <summary>Constructor.</summary>
	protected DialogViewModel( Platform platform )
			: base( platform )
	{ }
}
