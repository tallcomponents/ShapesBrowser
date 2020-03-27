using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using TallComponents.Samples.ShapesBrowser.Other;

namespace TallComponents.Samples.ShapesBrowser.Views
{
    internal class DialogBoxService : IDialogBoxService
    {
        public string Filter { get; set; }
        public Dictionary<Type, Type> Mappings;

        public DialogBoxService()
        {
            Mappings = new Dictionary<Type, Type>();
        }
        public void Register<TViewModel, TView>()
        {
            if (Mappings.ContainsKey(typeof(TViewModel)))
            {
                throw new ArgumentException($"Type {typeof(TViewModel)} is already mapped to type {typeof(TView)}");
            }

            Mappings.Add(typeof(TViewModel), typeof(TView));
        }

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

        public void ShowWindow(object viewModel)
        {
            var view = Mappings[viewModel.GetType()];
            var window = (Window)Activator.CreateInstance(view);
            window.DataContext = viewModel;
            window.Show();
        }
    }
}