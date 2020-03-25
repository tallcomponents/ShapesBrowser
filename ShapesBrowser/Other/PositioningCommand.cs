using System;
using System.Windows;
using System.Windows.Input;

namespace TallComponents.Samples.ShapesBrowser
{
    public class PositioningCommand : ICommand
    {
        private readonly Action<Point, Modifiers> _action;
        public PositioningCommand(Action<Point, Modifiers> action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            var mousePos = Mouse.GetPosition((IInputElement)parameter);
            Modifiers modifiers = Modifiers.None;
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Control | ModifierKeys.Shift:
                    modifiers = Modifiers.CtrlShift;
                    break;
                case ModifierKeys.Control:
                    modifiers = Modifiers.Ctrl;
                    break;
                case ModifierKeys.Shift:
                    modifiers = Modifiers.Shift;
                    break;
                case ModifierKeys.None:
                    modifiers = Modifiers.None;
                    break;
            }
            _action(mousePos, modifiers);
        }

        public bool CanExecute(object parameter) { return true; }

        public event EventHandler CanExecuteChanged;
    }
}
