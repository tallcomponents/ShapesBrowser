using System.Collections.ObjectModel;
using System.Linq;
using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
{
    public class ShapeCollectionViewModel : BaseViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private readonly ShapesTreeViewModel _shapesTreeViewModel;

        public ShapeCollectionViewModel(Shape person, ShapesTreeViewModel shapesTreeViewModel) : this(person, null, shapesTreeViewModel)
        {
        }

        private ShapeCollectionViewModel(Shape shape, ShapeCollectionViewModel parent, ShapesTreeViewModel shapesTreeViewModel)
        {
            this._shapesTreeViewModel = shapesTreeViewModel;
            Shape = shape;
            Parent = parent;

            if (!(shape is ShapeCollection sc))
            {
                return;
            }

            Children = new ObservableCollection<ShapeCollectionViewModel>(
                (from child in sc select new ShapeCollectionViewModel(child, this, shapesTreeViewModel)).ToList());
        }

        public ObservableCollection<ShapeCollectionViewModel> Children { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                SetProperty(ref _isExpanded, value);
                // Expand all the way up to the root.
                if (_isExpanded && Parent != null) Parent.IsExpanded = true;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ShapeCollectionViewModel Parent { get; }
        public Shape Shape { get; }

        public string Text
        {
            get
            {
                if (Shape is ShapeCollection)
                {
                    return "Shape Collection " + Shape.ID;
                }

                var shapeType = Shape.ToString();
                shapeType = shapeType.Replace("TallComponents.PDF.Shapes.", "");
                return string.Format("{0} {1}", shapeType, Shape.ID);
            }
        }

        public void Select(Shape shape)
        {
            if (Shape == shape)
            {
                IsSelected = true;
                IsExpanded = true;
            }
            else
            {
                if (null == Children) return;
                foreach (var child in Children)
                {
                    child.Select(shape);
                }
            }
        }
    }
}