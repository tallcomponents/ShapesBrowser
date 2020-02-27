using System.Windows;
using Microsoft.Win32;
using TallComponents.Samples.ShapesBrowser.Other;

namespace TallComponents.Samples.ShapesBrowser.Views
{
    internal class DialogBoxService : IDialogBoxService
    {
        public string Filter { get; set; }

        public string OpenFileDialog(string defaultPath)
        {
            var dialog = new OpenFileDialog {Filter = Filter};
            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

        public string SaveFileDialog(string defaultPath)
        {
            var dialog = new SaveFileDialog {Filter = Filter};
            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}