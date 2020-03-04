using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using TallComponents.PDF;
using TallComponents.PDF.Shapes;
using Canvas = System.Windows.Controls.Canvas;
using Rectangle = System.Windows.Shapes.Rectangle;
using Shape = TallComponents.PDF.Shapes.Shape;

namespace TallComponents.Samples.ShapesBrowser
{
    public class ShapesTreeViewModel : BaseViewModel
    {
        private ShapeCollectionViewModel _rootShapeCollection;
        private Canvas _overlay;
        private ShapeCollectionViewModel _selectedItem;
        private HashSet<ShapeCollectionViewModel> _selectedItems;
        private readonly List<ShapeCollection> _shapeCollections = new List<ShapeCollection>();
        private TagsTreeViewModel _tagsTreeViewModel;
        private ObservableCollection<ShapeCollectionViewModel> _viewItems;
        private bool _modified;
        private ObservableCollection<ShapeCollectionViewModel> _selectedItemsViewModel;

        public ShapesTreeViewModel()
        {
            _selectedItems = new HashSet<ShapeCollectionViewModel>();
            SelectedItems  = new ObservableCollection<ShapeCollectionViewModel>();
            SelectedItems.CollectionChanged += OnSelectedItemsCollectionChanged;
        }

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

        public ShapeCollection GetRoot()
        {
            return _rootShapeCollection.Shape as ShapeCollection;
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
            foreach (var selectedItem in _selectedItems)
            {
                selectedItem.IsSelected = false;
            }
        }

        public ObservableCollection<ShapeCollectionViewModel> SelectedItems
        {
            get => _selectedItemsViewModel;
            set => SetProperty(ref _selectedItemsViewModel, value);
        }
        public ContentShape FindShape(Shape shape, TransformGroup parentTransform, Point position)
        {
            ContentShape shapeFound = null;
            if (_shapeCollections.Count == 0) return null;
            if (null == shape || null == parentTransform)
            {
                shape = _shapeCollections[0];
                parentTransform = new TransformGroup();
                AddShapeToTransform(shape as ShapeCollection, parentTransform);
            }

            switch (shape)
            {
                case ShapeCollection shapeCollection:
                {
                    var transform = CreateTransformGroup(parentTransform, shapeCollection);
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
                    var transform = CreateTransformGroup(parentTransform, text);
                        var rect = transform.TransformBounds(new Rect(new Size(text.MeasuredWidth, text.MeasuredHeight)));
                    if (rect.Contains(position)) shapeFound = text;
                    break;
                }
                case ImageShape image:
                {
                    var transform = CreateTransformGroup(parentTransform, image);
                        var rect = transform.TransformBounds(new Rect(new Size(image.Width, image.Height)));
                    if (rect.Contains(position)) shapeFound = image;
                    break;
                }
                case FreeHandShape freehand:
                {
                    var transform = CreateTransformGroup(parentTransform, freehand);
                    foreach (var freehandPath in freehand.Paths)
                    {
                        var geometry = new PathGeometry();
                        var figure = new PathFigure {IsClosed = freehandPath.Closed};
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
                        var bounds = transform.TransformBounds(geometry.Bounds);
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

        private TransformGroup CreateTransformGroup(TransformGroup parentTransform, ContentShape contentShape)
        {
            var transform = new TransformGroup();
            transform.Children.Add(parentTransform);
            AddShapeToTransform(contentShape, transform);
            return transform;
        }

        public int GetNextShapeCollectionID()
        {
            return _shapeCollections.Count;
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

        public void RemoveSelectedItems()
        {
            foreach (var shape in _selectedItems)
            {
                shape.Shape.Parent?.Remove(shape.Shape);
            }
            _selectedItems.Clear();
        }

        public void Select(ContentShape shape, bool modified)
        {
            this._modified = modified;

            // WIP - deselect everything if control is not being held
           /* if (!_modified)
            {
                foreach (var shapeCollectionViewModel in SelectedItems)
                {
                    shapeCollectionViewModel.IsSelected = false;
                }
            }*/

            _rootShapeCollection.Select(shape);
        }

        private void OnSelectedItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _overlay.Children.Clear();
            var collection =  sender as ObservableCollection<ShapeCollectionViewModel>;

            foreach (var shapeVM in collection)
            {
                if(shapeVM == null)
                    continue;

                if (shapeVM.IsSelected == false) continue;

                if (!(shapeVM.Shape is Shape selectedItem)) continue;
                if (selectedItem is ContentShape contentShape)
                {
                    var transform = GetTransform(contentShape);
                    MarkChildShapes(contentShape, transform);
                    _tagsTreeViewModel.Select(contentShape);
                }
            }
        }

        public void SetCanvas(Canvas canvas)
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

        private void AddShapeToTransform(ContentShape shape, TransformGroup parentTransform)
        {
            if (null == shape || null == parentTransform) return;
            var tr = shape.Transform.AsMatrixTransform;
            var matrix = new MatrixTransform(tr.ScaleX, tr.ShearX, tr.ShearY, tr.ScaleY, tr.OffsetX, tr.OffsetY);
            parentTransform.Children.Add(matrix);
        }

        private TransformGroup GetTransform(Shape shape)
        {
            var transform = new TransformGroup();
            var parent = GetParent(shape);
            while (null != parent)
            {
                AddShapeToTransform(parent, transform);
                parent = GetParent(parent);
            }

            return transform;
        }

        private void MarkChildShapes(Shape shape, TransformGroup parentTransform)
        {
            if (null == shape || null == parentTransform) return;
            switch (shape)
            {
                case ShapeCollection shapeCollection:
                {
                    var transform = CreateTransformGroup(parentTransform, shapeCollection);
                        foreach (var child in shapeCollection) MarkChildShapes(child, transform);
                    break;
                }
                case TextShape text:
                {
                    var transform = CreateTransformGroup(parentTransform, text);
                        var rectangle = new Rectangle
                    {
                        RenderTransform = transform,
                        Fill = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)),
                        Width = text.MeasuredWidth,
                        Height = text.MeasuredHeight,
                    };
                    _overlay.Children.Add(rectangle);
                    break;
                }
                case ImageShape image:
                {
                    var transform = CreateTransformGroup(parentTransform, image);
                        var rectangle = new Rectangle
                    {
                        RenderTransform = transform,
                        Fill = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)),
                        Width = image.Width,
                        Height = image.Height
                    };
                    _overlay.Children.Add(rectangle);
                    break;
                }
                case FreeHandShape freehand:
                {
                    var transform = CreateTransformGroup(parentTransform, freehand);
                        var path = new Path();
                    var geometry = new PathGeometry();
                    path.Data = geometry;
                    foreach (var freehandPath in freehand.Paths)
                    {
                        var figure = new PathFigure {IsClosed = freehandPath.Closed};
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
                    path.RenderTransform = transform;
                    _overlay.Children.Add(path);
                    break;
                }
            }
        }
    }
}