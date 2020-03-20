using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TallComponents.PDF;
using TallComponents.PDF.Shapes;
using Shape = TallComponents.PDF.Shapes.Shape;

namespace TallComponents.Samples.ShapesBrowser
{
    public class ShapesTreeViewModel : BaseViewModel
    {
        private ShapeCollectionViewModel _rootShapeCollection;
        private ObservableCollection<RectangleViewModel> _overlay;
        private ShapeCollectionViewModel _selectedItem;
        private readonly List<ShapeCollection> _shapeCollections = new List<ShapeCollection>();
        private TagsTreeViewModel _tagsTreeViewModel;
        private ObservableCollection<ShapeCollectionViewModel> _viewItems;

        public ObservableCollection<ShapeCollectionViewModel> ViewItems
        {
            get => _viewItems;
            private set => SetProperty(ref _viewItems, value);
        }

        public static ShapeCollection FindRoot(ShapeCollection sc)
        {
            if (sc == null) return null;
            while (sc.Parent != null)
            {
                sc = sc.Parent;
            }

            return sc;
        }

        public static int GetID(Shape shape)
        {
            var index = -1;
            if (shape is ShapeCollection)
            {
                var pos = shape.ID.IndexOf(':');
                if (pos < 0) index = int.Parse(shape.ID);
                else index = int.Parse(shape.ID.Substring(pos + 1, shape.ID.Length - pos - 1));
            }

            return index;
        }

        public static int GetParentID(Shape shape)
        {
            var index = -1;
            if (shape is ShapeCollection)
            {
                var pos = shape.ID.IndexOf(':');
                if (pos >= 0) index = int.Parse(shape.ID.Substring(0, pos));
            }
            else index = int.Parse(shape.ID);

            return index;
        }

        public void Add(ShapeCollection shapeCollection)
        {
            if (null == shapeCollection) return;
            _shapeCollections.Add(shapeCollection);
        }

        public void Deselect()
        {
            if (_selectedItem == null) return;
            _selectedItem.IsSelected = false;
        }

        public ContentShape FindShape(Shape shape, Matrix parentTransform, Point position)
        {
            ContentShape shapeFound = null;
            if (_shapeCollections.Count == 0) return null;
            if (null == shape || null == parentTransform)
            {
                shape = _shapeCollections[0];
                parentTransform = ComputeAbsoluteMatrix(shape as ShapeCollection, parentTransform);
            }

            switch (shape)
            {
                case ShapeCollection shapeCollection:
                    {
                        var transform = ComputeAbsoluteMatrix(shapeCollection, parentTransform);
                        foreach (var child in shapeCollection)
                        {
                            shapeFound = FindShape(child, transform, position);
                            if (null != shapeFound)
                            {
                                if (null == shapeFound.ParentTag && null != shapeCollection.ParentTag)
                                    shapeFound = shapeCollection;
                                return shapeFound;
                            }
                        }

                        break;
                    }
                case TextShape text:
                    {
                        var transform = ComputeAbsoluteMatrix(text, parentTransform);
                        var rect = new Rect(new Size(text.MeasuredWidth, text.MeasuredHeight));
                        rect.Transform(transform);
                        if (rect.Contains(position)) shapeFound = text;
                        break;
                    }
                case ImageShape image:
                    {
                        var transform = ComputeAbsoluteMatrix(image, parentTransform);
                        var rect = new Rect(new Size(image.Width, image.Height));
                        rect.Transform(transform);
                        if (rect.Contains(position)) shapeFound = image;
                        break;
                    }
                case FreeHandShape freehand:
                    {
                        var transform = ComputeAbsoluteMatrix(freehand, parentTransform);
                        foreach (var freehandPath in freehand.Paths)
                        {
                            var geometry = new PathGeometry();
                            var figure = new PathFigure { IsClosed = freehandPath.Closed };
                            foreach (var segment in freehandPath.Segments)
                            {
                                if (segment is FreeHandStartSegment)
                                {
                                    var start = segment as FreeHandStartSegment;
                                    figure.StartPoint = new Point(start.X, start.Y);
                                }
                                else if (segment is FreeHandLineSegment)
                                {
                                    var line = segment as FreeHandLineSegment;
                                    figure.Segments.Add(new LineSegment(new Point(line.X1, line.Y1), true));
                                }
                                else if (segment is FreeHandBezierSegment)
                                {
                                    var bezier = segment as FreeHandBezierSegment;
                                    figure.Segments.Add(new BezierSegment(new Point(bezier.X1, bezier.Y1),
                                        new Point(bezier.X2, bezier.Y2), new Point(bezier.X3, bezier.Y3), true));
                                }
                            }

                            geometry.Figures.Add(figure);
                            var bounds = geometry.Bounds;
                            bounds.Transform(transform);
                            if (bounds.Contains(position))
                            {
                                shapeFound = freehand;
                                break;
                            }
                        }

                        break;
                    }
            }

            return shapeFound;
        }

        public int GetNextShapeCollectionID()
        {
            return _shapeCollections.Count;
        }

        public Shape GetSelectedShape()
        {
            return _selectedItem.Shape;
        }

        public void Initialize(Page page)
        {
            _shapeCollections.Clear();
            var shapes = page.CreateShapes();
            var root = new ShapeCollection {shapes};
            AddItem(root, null);
            _rootShapeCollection = new ShapeCollectionViewModel(root, this);
            ViewItems = new ObservableCollection<ShapeCollectionViewModel>(new[] {_rootShapeCollection});
        }

        public static void Remove(Shape shape)
        {
            shape.Parent?.Remove(shape);
        }

        public void Select(ContentShape shape)
        {
            _rootShapeCollection.Select(shape);
        }

        public void SelectedItemChanged(ShapeCollectionViewModel shapeVM)
        {
            if (shapeVM.IsSelected == false) return;
            _selectedItem = shapeVM;
            _overlay.Clear();

            if (!(shapeVM.Shape is Shape selectedItem)) return;
            if (selectedItem is ContentShape contentShape)
            {
                var matrix = ComputeAbsoluteMatrix(contentShape);
                MarkChildShapes(contentShape, matrix);
                _tagsTreeViewModel.Select(contentShape);
            }
        }

        public void SetCanvas(ObservableCollection<RectangleViewModel> canvas)
        {
            _overlay = canvas;
        }

        public void SetTagsTree(TagsTreeViewModel tagsTreeViewModel)
        {
            this._tagsTreeViewModel = tagsTreeViewModel;
        }

        internal ShapeCollection GetParent(Shape shape)
        {
            var index = GetParentID(shape);
            if (index >= 0 && index < _shapeCollections.Count) return _shapeCollections[index];
            return null;
        }

        private void AddItem(Shape shape, ShapeCollection parentShapeCollection)
        {
            if (null != parentShapeCollection)
            {
                if (shape is ShapeCollection)
                    shape.ID = string.Format("{0}:{1}", GetID(parentShapeCollection), GetNextShapeCollectionID());
                else shape.ID = GetID(parentShapeCollection).ToString();
            }
            else
            {
                if (shape is ShapeCollection) shape.ID = GetNextShapeCollectionID().ToString();
                else Debug.Assert(false); //the first item should be shapecollection
            }

            Add(shape as ShapeCollection);
            var contentShape = shape as ContentShape;
            if (contentShape?.ParentTag != null)
            {
                _tagsTreeViewModel.SetShape(contentShape);
            }

            if (shape is ShapeCollection)
            {
                var shapeCollection = shape as ShapeCollection;
                foreach (var child in shapeCollection) AddItem(child, shapeCollection);
            }
        }

        private Matrix ComputeAbsoluteMatrix(ContentShape shape, Matrix parentMatrix)
        {
            if (null == shape) return Matrix.Identity;
            var tr = shape.Transform.AsMatrixTransform;
            var matrix = new Matrix(tr.ScaleX, tr.ShearY, tr.ShearX, tr.ScaleY, tr.OffsetX, tr.OffsetY);
            return Matrix.Multiply(matrix, parentMatrix);
        }

        private Matrix ComputeAbsoluteMatrix(Shape shape)
        {
            var transform = new Matrix();
            var parent = GetParent(shape);
            while (null != parent)
            {
                ComputeAbsoluteMatrix(parent, transform);
                parent = GetParent(parent);
            }

            return transform;
        }

        private void MarkChildShapes(Shape shape, Matrix parentMatrix)
        {
            if (null == shape || null == parentMatrix) return;
            switch (shape)
            {
                case ShapeCollection shapeCollection:
                {
                    var transform = ComputeAbsoluteMatrix( shapeCollection, parentMatrix);
                        foreach (var child in shapeCollection) MarkChildShapes(child, transform);
                    break;
                }
                case TextShape text:
                {
                    var transform = ComputeAbsoluteMatrix(text, parentMatrix);
                    _overlay.Add(new RectangleViewModel { MatrixTransform = transform, Height = text.MeasuredHeight, Width = text.MeasuredWidth });
                        break;
                }
                case ImageShape image:
                {
                    var transform = ComputeAbsoluteMatrix(image, parentMatrix);
                    _overlay.Add(new RectangleViewModel { MatrixTransform = transform, Height = image.Height, Width = image.Width });

                        break;
                }
                case FreeHandShape freehand:
                    {
                        var transform = ComputeAbsoluteMatrix(freehand, parentMatrix);
                        var path = new Path();
                        var geometry = new PathGeometry();
                        path.Data = geometry;
                        foreach (var freehandPath in freehand.Paths)
                        {
                            var figure = new PathFigure { IsClosed = freehandPath.Closed };
                            foreach (var segment in freehandPath.Segments)
                            {
                                switch (segment)
                                {
                                    case FreeHandStartSegment start:
                                        {
                                            figure.StartPoint = new Point(start.X, start.Y);
                                            break;
                                        }
                                    case FreeHandLineSegment line:
                                        {
                                            figure.Segments.Add(new LineSegment(new Point(line.X1, line.Y1), true));
                                            break;
                                        }
                                    case FreeHandBezierSegment bezier:
                                        {
                                            figure.Segments.Add(new BezierSegment(new Point(bezier.X1, bezier.Y1),
                                                new Point(bezier.X2, bezier.Y2), new Point(bezier.X3, bezier.Y3), true));
                                            break;
                                        }
                                }
                            }

                            geometry.Figures.Add(figure);
                        }

                        path.Fill = new SolidColorBrush(Colors.Red);
                        path.Stroke = new SolidColorBrush(Colors.Green);
                        path.StrokeThickness = 1;
                        //path.RenderTransform = transform;
                        //_overlay.Children.Add(path);
                        break;
                    }
            }
        }
    }
}