namespace MikeNakis.ViewModeling.Wpf;

using Sys = System;
using WpfInput = System.Windows.Input;

public sealed class WpfCommand : Command, WpfInput.ICommand
{
	event Sys.EventHandler? canExecuteChanged;

	public WpfCommand( ViewModel parent, OperationType operationType, Sys.Action handler, Sys.Func<bool> isEnabled )
			: base( parent, operationType, handler, isEnabled )
	{ }

	bool WpfInput.ICommand.CanExecute( object? parameter )
	{
		return IsEnabled;
	}

	void WpfInput.ICommand.Execute( object? parameter )
	{
		Execute();
	}

	event Sys.EventHandler? WpfInput.ICommand.CanExecuteChanged { add => canExecuteChanged += value; remove => canExecuteChanged -= value; }

	protected override void OnIsEnabledChanged()
	{
		base.OnIsEnabledChanged();
		canExecuteChanged?.Invoke( this, Sys.EventArgs.Empty );
	}
}
