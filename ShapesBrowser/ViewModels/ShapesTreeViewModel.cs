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
    internal class ShapesTreeViewModel : BaseViewModel
    {
        private ShapeCollectionViewModel _rootShapeCollection;
        private Canvas _overlay;
        private readonly List<ShapeCollection> _shapeCollections = new List<ShapeCollection>();
        private TagsTreeViewModel _tagsTreeViewModel;
        private ObservableCollection<ShapeCollectionViewModel> _viewItems;
        private ObservableCollection<ShapeCollectionViewModel> _selectedItemsViewModel;
        private ShapeCollectionViewModel _startItem;
        private bool _suspendTagDeselection;

        public ShapesTreeViewModel()
        {
            SelectedItems = new ObservableCollection<ShapeCollectionViewModel>();
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

        public void Deselect(ShapeCollectionViewModel contentShape)
        {
            if (contentShape == null) return;
            if (contentShape.Parent?.Shape is ShapeCollection)
            {
                contentShape.Parent.IsSelected = false;
            }
            if (contentShape.Shape is ShapeCollection)
            {
                contentShape.IsSelected = false;
                if (IsAtLeastOneDeselected(contentShape)) return;
                foreach (var child in contentShape.Children)
                {
                    Deselect(child);
                }
            }
            else
            {
                _overlay.Children.Remove(contentShape.OverlayShape);
                contentShape.IsMarked = false;
                contentShape.IsSelected = false;
            }
        }

        private bool IsAtLeastOneDeselected(ShapeCollectionViewModel contentShape)
        {
            return contentShape.Shape is ShapeCollection && contentShape.Children.Any(child => !child.IsSelected);
        }

        public void Deselect()
        {
            var selectedItems = SelectedItems.ToList();

            foreach (var shapeCollectionViewModel in selectedItems)
            {
                shapeCollectionViewModel.IsSelected = false;
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
            GenerateShapeTagBinding(_rootShapeCollection);

            ViewItems = new ObservableCollection<ShapeCollectionViewModel>(new[] {_rootShapeCollection});
        }

        private void GenerateShapeTagBinding(ShapeCollectionViewModel shapeCollectionViewModel)
        {
            var contentShape = shapeCollectionViewModel.Shape as ContentShape;
            if (contentShape?.ParentTag != null)
            {
                _tagsTreeViewModel.SetShape(shapeCollectionViewModel);
            }

            if (shapeCollectionViewModel.Children != null)
            {
                foreach (var child in shapeCollectionViewModel.Children)
                {
                    GenerateShapeTagBinding(child);
                }
            }
        }

        public void RemoveSelectedItems()
        {
            foreach (var shape in SelectedItems)
            {
                shape.Shape.Parent?.Remove(shape.Shape);
            }

            SelectedItems.Clear();
        }

        public void Select(ContentShape shape, MainWindowViewModel.Modifiers modified)
        {
            switch (modified)
            {
                case MainWindowViewModel.Modifiers.None:
                {
                    Deselect();
                    _startItem = _rootShapeCollection.Select(shape);
                    break;
                }
                case MainWindowViewModel.Modifiers.Ctrl:
                    _startItem = _rootShapeCollection.Select(shape);
                    break;
                case MainWindowViewModel.Modifiers.Shift:
                case MainWindowViewModel.Modifiers.CtrlShift:
                {
                    var list = ViewItems[0].ToList();

                    if (modified != MainWindowViewModel.Modifiers.CtrlShift)
                    {
                        Deselect();
                    }

                    var endItem = _rootShapeCollection.Select(shape);
                    if (endItem == _startItem)
                    {
                        return;
                    }

                    var isBetween = false;
                    foreach (var item in list)
                    {
                        if (item == endItem || item == _startItem)
                        {
                            isBetween = !isBetween;

                            item.IsSelected = true;
                            continue;
                        }

                        if (isBetween)
                        {
                            item.IsSelected = true;
                        }
                    }

                    break;
                }
            }
        }

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var shape = oldItem as ShapeCollectionViewModel;
                    Deselect(shape);
                    if (!_suspendTagDeselection)
                        _tagsTreeViewModel.Deselect(shape.Shape as ContentShape);
                }

                return;
            }

            var collection = sender as ObservableCollection<ShapeCollectionViewModel>;
            for (var i = 0; i < collection.Count; i++)
            {
                var shapeVM = collection[i];

                if (shapeVM == null || shapeVM.IsSelected == false || !(shapeVM.Shape is Shape selectedItem) ||
                    !(selectedItem is ContentShape contentShape)) continue;

                var transform = GetTransform(contentShape);
                MarkChildShapes(shapeVM, transform);
                _tagsTreeViewModel.Select(contentShape);
            }
        }

        public void SetCanvas(Canvas canvas)
        {
            _overlay = canvas;
        }

        public void SetTagsTree(TagsTreeViewModel tagsTreeViewModel)
        {
            _tagsTreeViewModel = tagsTreeViewModel;
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

        private bool MarkChildShapes(ShapeCollectionViewModel shape, TransformGroup parentTransform)
        {
            if (null == shape || null == parentTransform || shape.IsMarked) return false;
            bool ret = false;
            switch (shape.Shape)
            {
                case ShapeCollection shapeCollection:
                {
                    var transform = CreateTransformGroup(parentTransform, shapeCollection);
                    foreach (var child in shape.Children)
                    {
                        if (MarkChildShapes(child, transform))
                        {
                            ret = true;
                        }
                    }
                    shape.IsSelected = true;

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
                        Height = text.MeasuredHeight
                    };
                    _overlay.Children.Add(rectangle);
                    shape.IsMarked = true;
                    shape.IsSelected = true;
                    shape.OverlayShape = rectangle;
                    ret = true;
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
                    shape.IsMarked = true;
                    shape.OverlayShape = rectangle;

                    ret = true;
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
                    shape.IsMarked = true;
                    shape.OverlayShape = path;

                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void SuspendTagDeselection(bool suspendTagDeselection)
        {
            _suspendTagDeselection = suspendTagDeselection;
        }
    }
}