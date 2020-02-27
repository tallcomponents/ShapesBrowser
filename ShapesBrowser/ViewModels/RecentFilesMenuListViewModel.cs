using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using TallComponents.Samples.ShapesBrowser.MenuViewModel;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class RecentFilesMenuListViewModel: BaseViewModel
    {
        private readonly List<string> _filePaths;
        private readonly int _numFilePaths;
        private ObservableCollection<MenuItemViewModel> _menuItems;

        public RecentFilesMenuListViewModel(int numFiles)
        {
            _numFilePaths = numFiles;
            _filePaths = new List<string>();

            MenuItems = new ObservableCollection<MenuItemViewModel>();
            for (var i = 0; i < _numFilePaths; i++)
            {
                MenuItems.Add(new MenuItemViewModel
                {
                    Visibility = Visibility.Collapsed, OpenCommand = new RelayCommand<string>(OnFilePathClicked)
                });
            }

            LoadFilePaths();
            ShowFilePaths();
        }

        public delegate void FileSelectedEventHandler(string filePath);

        public event FileSelectedEventHandler OnFilePathSelected;
        public ObservableCollection<MenuItemViewModel> MenuItems
        {
            get => _menuItems;
            private set => SetProperty(ref _menuItems, value);
        }

        public void AddFilePath(string filePath)
        {
            _filePaths.Remove(filePath);
            _filePaths.Insert(0, filePath);
            if (_filePaths.Count > _numFilePaths)
            {
                _filePaths.RemoveAt(_numFilePaths);
            }

            ShowFilePaths();
            SaveFilePaths();
        }

        public void RemoveFile(string filePath)
        {
            _filePaths.Remove(filePath);
            ShowFilePaths();
            SaveFilePaths();
        }

        private static RegistryKey PrepareRegKey()
        {
            var regKey = Registry.CurrentUser.OpenSubKey("Software", true);
            var title =
                Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as
                    AssemblyTitleAttribute;
            var subKey = regKey.CreateSubKey(title.Title);
            return subKey;
        }

        private void LoadFilePaths()
        {
            for (var i = 0; i < _numFilePaths; i++)
            {
                var filePath = (string) PrepareRegKey().GetValue("FilePath" + i);
                if (!string.IsNullOrEmpty(filePath))
                {
                    _filePaths.Add(filePath);
                }
            }
        }

        private void OnFilePathClicked(string filePath)
        {
            OnFilePathSelected?.Invoke(filePath);
        }

        private void SaveFilePaths()
        {
            for (var i = 0; i < _numFilePaths; i++)
            {
                var regValue = "FilePath" + i;
                if (null != PrepareRegKey().GetValue(regValue))
                {
                    PrepareRegKey().DeleteValue(regValue);
                }
            }

            var index = 0;
            foreach (var filePath in _filePaths)
            {
                PrepareRegKey().SetValue("FilePath" + index, filePath);
                index++;
            }
        }

        private void ShowFilePaths()
        {
            for (var i = 0; i < _filePaths.Count; i++)
            {
                var menuItem = MenuItems[i];
                menuItem.Text = $"{i + 1} {_filePaths[i]}";
                menuItem.Visibility = Visibility.Visible;
                menuItem.OpenCommand = new RelayCommand<string>(OnFilePathClicked);
                menuItem.CommandParameter = _filePaths[i];
            }

            for (var i = _filePaths.Count; i < _numFilePaths; i++)
            {
                var menuItem = MenuItems[i];
                menuItem.Visibility = Visibility.Collapsed;
            }
        }
    }
}