using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using TallComponents.PDF.Shapes;

namespace WpfApplication1
{
    public class ChildShapes
    {
        public ChildShapes(ShapeCollection shapes)
        {
            Shapes = new List<Shape>(shapes);
        }

        public List<Shape> Shapes { get; set;  }

        public string Display { get { return string.Format("{0} child shapes", Shapes.Count); } }
    }

    public class SimpleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new List<object>(values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShapeCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<object> items = new List<object>();

            foreach (object v in values)
            {
                if (v is ShapeCollection)
                {
                    items.Add(new ChildShapes(v as ShapeCollection));
                }
                else
                {
                    items.Add(v);
                }
            }

            return items;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
