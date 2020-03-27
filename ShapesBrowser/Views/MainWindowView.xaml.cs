using System.Windows;
using TallComponents.Samples.ShapesBrowser.Other;
using TallComponents.Samples.ShapesBrowser.Views;

namespace TallComponents.Samples.ShapesBrowser
{
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            IDialogBoxService dialogBoxService = new DialogBoxService();
            dialogBoxService.Register<ShapePropertiesViewModel, ShapePropertiesView>();
            InitializeComponent();
            DataContext = new MainWindowViewModel(dialogBoxService);
        }
    }
}