using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
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
        static List<ShapeCollection> shapeCollections = new List<ShapeCollection>();

        internal static void Reset()
        {
            shapeCollections.Clear();
        }

        internal static ShapeCollection GetParent(Shape shape)
        {
            int index = 0;
            if (shape is ShapeCollection)
            {
                if (shape.ID == null || shape.ID[0] == ':') return null;
                index = int.Parse(shape.ID.Substring(0, shape.ID.IndexOf(':')));
            }
            else
            {
                index = int.Parse(shape.ID);
            }

            return shapeCollections[index];
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<object> items = new List<object>();

            foreach (object v in values)
            {
                if (v is ShapeCollection)
                {
                    ShapeCollection shapes = v as ShapeCollection;
                    shapeCollections.Add(shapes);
                    int id = shapeCollections.Count - 1;
                    // the ID of a ShapeCollection is formatted as "parent-id:self-id"
                    // the OD of all other shapes is formatted as "parent-id"
                    shapes.ID = string.Format("{0}:{1}", shapes.ID, id);

                    foreach (Shape shape in shapes)
                    {
                        shape.ID = id.ToString();
                    }

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
