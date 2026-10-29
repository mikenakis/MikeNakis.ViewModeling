namespace MikeNakis.ViewModeling;

public abstract class ModalDialogViewModel : DialogViewModel
{
	public bool? Result { get => GetPropertyValue( () => (bool?)null ); protected set => SetPropertyValue( value ); }

	/// <summary>Constructor.</summary>
	protected ModalDialogViewModel( Platform platform )
			: base( platform )
	{ }
}
