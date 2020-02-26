using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace TallComponents.Samples.ShapesBrowser.MenuViewModel
{
    public class MenuItemViewModel : BaseViewModel
    {
        private string _header;
        private string _commandParameter;
        private ICommand _openCommand;
        private Visibility _visibility;

        public MenuItemViewModel()
        {
            MenuItems = new ObservableCollection<MenuItemViewModel>();
        }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; }

        public string CommandParameter
        {
            get => _commandParameter;
            set => SetProperty(ref _commandParameter, value);
        }

        public ICommand OpenCommand
        {
            get => _openCommand;
            set => SetProperty(ref _openCommand, value);
        }

        public string Text
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public Visibility Visibility
        {
            get => _visibility;
            set => SetProperty(ref _visibility, value);
        }
    }
}