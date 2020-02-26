using System;
using System.Windows.Input;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class RelayCommand : ICommand
    {
        private readonly Action _action;

        public RelayCommand(Action action)
        {
            _action = action;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _action();
    }

    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _action;

        public RelayCommand(Action<T> action)
        {
            _action = action;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _action((T) parameter);
    }
}