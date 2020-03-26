using System;
using System.Windows;
using System.Windows.Input;

namespace TallComponents.Samples.ShapesBrowser
{
    public class PositioningCommand : ICommand
    {
        private readonly Action<Point> _action;
        public PositioningCommand(Action<Point> action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            var mousePos = Mouse.GetPosition((IInputElement)parameter);
            _action(mousePos);
        }

        public bool CanExecute(object parameter) { return true; }

        public event EventHandler CanExecuteChanged;
    }
}
