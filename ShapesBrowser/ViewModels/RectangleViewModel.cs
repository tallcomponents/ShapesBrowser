using System.Windows.Media;

namespace TallComponents.Samples.ShapesBrowser
{
    public class RectangleViewModel : BaseViewModel
    {
        private double _left;

        public double Left
        {
            get => _left;
            set => SetProperty(ref _left, value);
        }

        public double Top { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public Transform RenderTransform { get; set; }
    }
}