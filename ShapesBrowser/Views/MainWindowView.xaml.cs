using System.Windows;

namespace TallComponents.Samples.ShapesBrowser
{
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}