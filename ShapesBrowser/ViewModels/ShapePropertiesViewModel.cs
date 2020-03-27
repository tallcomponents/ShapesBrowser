using System.Windows.Media;
using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class ShapePropertiesViewModel: BaseViewModel
    {
        private string _text;
        private double _x;
        private double _y;
        private Matrix _transform;

        public ShapePropertiesViewModel(Shape shape)
        {
            if (!(shape is ContentShape contentShape)) return;
            X = contentShape.X;
            Y = contentShape.Y;

            if (contentShape is TextShape textShape)
            {
                Text = textShape.Text;
            }

            var tr = contentShape.Transform.AsMatrixTransform;
            Transform = new Matrix(tr.ScaleX, tr.ShearY, tr.ShearX, tr.ScaleY, tr.OffsetX, tr.OffsetY);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public Matrix Transform
        {
            get => _transform;
            set => SetProperty(ref _transform, value);
        }
    }
}
