using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TallComponents.Samples.ShapesBrowser
{
    public class MouseTrackerDecorator : Decorator
    {
        private static readonly DependencyProperty MousePositionProperty;

        static MouseTrackerDecorator()
        {
            MousePositionProperty =
                DependencyProperty.Register("MousePosition", typeof(Point), typeof(MouseTrackerDecorator));
        }

        public override UIElement Child
        {
            get => base.Child;
            set
            {
                if (base.Child != null)
                    base.Child.MouseMove -= OnMouseMove;
                base.Child = value;
                base.Child.MouseMove += OnMouseMove;
            }
        }

        public Point MousePosition
        {
            get => (Point) GetValue(MousePositionProperty);
            set => SetValue(MousePositionProperty, value);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(base.Child);
            MousePosition = p;
        }
    }
}