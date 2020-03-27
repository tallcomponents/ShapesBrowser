namespace TallComponents.Samples.ShapesBrowser.Other
{
    public interface IDialogBoxService
    {
        string Filter { get; set; }
        string OpenFileDialog(string defaultPath);
        string SaveFileDialog(string defaultPath);
        void ShowMessage(string message);
        void ShowWindow(object viewModel);
        void Register<TViewModel, TView>();
    }
}