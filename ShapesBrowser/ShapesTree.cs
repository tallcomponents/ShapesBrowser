using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
{
    class ShapesTree
    {
        public ShapesTree(TreeView parentTree)
        {
            this.parentTree = parentTree;
        }

        public void SetTagsTree(TagsTree tagsTree)
        {
            this.tagsTree = tagsTree;
        }

        public void SetCanvas(System.Windows.Controls.Canvas canvas)
        {
            overlay = canvas;
        }

        public void Initialize(PDF.Page page)
        {
            Reset();
            ShapeCollection shapes = page.CreateShapes();
            ShapeCollection root = new ShapeCollection
            {
                shapes
            };

            parentTree.Items.Clear();

            AddItem(null, root, null);
        }

        void AddItem(TreeViewItem tvItem, Shape shape, ShapeCollection parentShapeCollection)
        {
            TreeViewItem item = new TreeViewItem();

            if (null != parentShapeCollection)
            {
                if (shape is ShapeCollection)
                    shape.ID = String.Format("{0}:{1}", GetID(parentShapeCollection), GetNextShapeCollectionID());
                else
                    shape.ID = GetID(parentShapeCollection).ToString();
            }
            else
            {
                if (shape is ShapeCollection)
                    shape.ID = GetNextShapeCollectionID().ToString();
                else
                    System.Diagnostics.Debug.Assert(false); //the first item should be shapecollection
            }

            if (null == tvItem)
                item.Header = "Page shapes";
            else
            {
                var shapeType = shape.ToString();
                shapeType = shapeType.Replace("TallComponents.PDF.Shapes.", "");
                item.Header = String.Format("{0} {1}", shapeType, shape.ID);
            }

            Add(shape as ShapeCollection);

            item.Tag = shape;

            if (null == tvItem)
                parentTree.Items.Add(item);
            else
                tvItem.Items.Add(item);

            if (shape is ContentShape)
            {
                var contentShape = shape as ContentShape;
                if (null != contentShape.ParentTag)
                {
                    tagsTree.SetShape(contentShape);
                }
            }

            if (shape is ShapeCollection)
            {
                var shapeCollection = shape as ShapeCollection;
                foreach (var child in shapeCollection)
                    AddItem(item, child, shapeCollection);
            }
        }

        public void SelectedItemChanged()
        {
            overlay.Children.Clear();

            if (!(parentTree.SelectedItem is TreeViewItem selectedItem))
                return;

            if (selectedItem.Tag is ContentShape shape)
            {
                var transform = GetTransform(shape);
                MarkChildShapes(shape, transform);
                tagsTree.Select(shape);
            }
        }

        public void Select(ContentShape shape)
        {
            var shapePath = GetShapePath(shape);
            Select(shapePath);
        }

        List<Shape> GetShapePath(Shape shape)
        {
            List<Shape> shapePath = new List<Shape>();

            if (null == shape)
                return shapePath;

            shapePath.Add(shape);

            var parent = GetParent(shape);
            while (null != parent)
            {
                shapePath.Insert(0, parent);
                parent = GetParent(parent);
            }

            return shapePath;
        }

        TreeViewItem FindTreeViewItem(List<Shape> shapePath, bool expand)
        {
            if (null == shapePath || shapePath.Count == 0)
                return null;

            TreeViewItem item = null;

            for (int i = 0; i < shapePath.Count; i++)
            {
                item = Find(shapePath[i], item);
                if (null != item && i < shapePath.Count - 1)
                {
                    if (expand)
                        item.IsExpanded = true;
                }
                else
                    break;
            }

            return item;
        }

        private TreeViewItem Find(Shape shape, TreeViewItem treeItem)
        {
            if (null == shape)
                return null;

            if (null == treeItem)
            {
                System.Diagnostics.Debug.Assert(shape.Equals((parentTree.Items[0] as TreeViewItem).Tag));
                return parentTree.Items[0] as TreeViewItem;
            }

            for (int i = 0; i < treeItem.Items.Count; i++)
            {
                var itemAsTVI = treeItem.Items[i] as TreeViewItem;
                if (itemAsTVI.Tag is Shape itemsShape && itemsShape.Equals(shape))
                    return itemAsTVI;
            }

            return null;
        }

        public void Select(List<Shape> shapePath)
        {
            var item = FindTreeViewItem(shapePath, true);

            if (null != item)
            {
                item.IsSelected = true;
                item.BringIntoView();
                selectedItem = item;
            }
        }

        public Shape GetSelectedShape()
        {
            return selectedItem.Tag as Shape;
        }

        void AddShapeToTransform(ContentShape shape, TransformGroup parentTransform)
        {
            if (null == shape || null == parentTransform)
                return;

            var tr = shape.Transform.AsMatrixTransform;

            MatrixTransform matrix = new MatrixTransform(tr.ScaleX, tr.ShearX, tr.ShearY, tr.ScaleY, tr.OffsetX, tr.OffsetY);

            parentTransform.Children.Add(matrix);
        }

        TransformGroup GetTransform(Shape shape)
        {
            var transform = new TransformGroup();

            ShapeCollection parent = GetParent(shape);
            while (null != parent)
            {
                AddShapeToTransform(parent, transform);
                parent = GetParent(parent);
            }

            return transform;
        }


        public void Reset()
        {
            shapeCollections.Clear();
        }

        public static int GetID(Shape shape)
        {
            int index = -1;
            if (shape is ShapeCollection)
            {
                var pos = shape.ID.IndexOf(':');
                if (pos < 0)
                    index = int.Parse(shape.ID);
                else
                    index = int.Parse(shape.ID.Substring(pos + 1, shape.ID.Length - pos - 1));
            }

            return index;
        }

        public static int GetParentID(Shape shape)
        {
            int index = -1;
            if (shape is ShapeCollection)
            {
                var pos = shape.ID.IndexOf(':');
                if (pos >= 0)
                    index = int.Parse(shape.ID.Substring(0, pos));
            }
            else
                index = int.Parse(shape.ID);

            return index;
        }

        public int GetNextShapeCollectionID()
        {
            return shapeCollections.Count;
        }

        public void Add(ShapeCollection shapeCollection)
        {
            if (null == shapeCollection)
                return;
            else
                shapeCollections.Add(shapeCollection);
        }

        internal ShapeCollection GetParent(Shape shape)
        {
            int index = GetParentID(shape);
            if (index >= 0 && index < shapeCollections.Count)
                return shapeCollections[index];
            else
                return null;
        }

        void MarkChildShapes(Shape shape, TransformGroup parentTransform)
        {
            if (null == shape || null == parentTransform)
                return;

            if (shape is ShapeCollection)
            {
                ShapeCollection shapeCollection = shape as ShapeCollection;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(shapeCollection, transform);

                foreach (var child in shapeCollection)
                    MarkChildShapes(child, transform);
                return;
            }
            else if (shape is TextShape)
            {
                TextShape text = shape as TextShape;
                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(text, transform);

                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle
                {
                    RenderTransform = transform,
                    Fill = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)),
                    Width = text.MeasuredWidth,
                    Height = text.MeasuredHeight
                };

                overlay.Children.Add(rectangle);
            }
            else if (shape is ImageShape)
            {
                ImageShape image = shape as ImageShape;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(image, transform);

                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle
                {
                    RenderTransform = transform,
                    Fill = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)),
                    Width = image.Width,
                    Height = image.Height
                };

                overlay.Children.Add(rectangle);
            }
            else if (shape is FreeHandShape)
            {
                FreeHandShape freehand = shape as FreeHandShape;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(freehand, transform);

                System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                PathGeometry geometry = new PathGeometry();
                path.Data = geometry;

                foreach (FreeHandPath freehandPath in freehand.Paths)
                {
                    PathFigure figure = new PathFigure
                    {
                        IsClosed = freehandPath.Closed
                    };

                    foreach (FreeHandSegment segment in freehandPath.Segments)
                    {
                        if (segment is FreeHandStartSegment)
                        {
                            FreeHandStartSegment start = segment as FreeHandStartSegment;
                            figure.StartPoint = new Point(start.X, start.Y);
                        }
                        else if (segment is FreeHandLineSegment)
                        {
                            FreeHandLineSegment line = segment as FreeHandLineSegment;
                            figure.Segments.Add(new LineSegment(new Point(line.X1, line.Y1), true));
                        }
                        else if (segment is FreeHandBezierSegment)
                        {
                            FreeHandBezierSegment bezier = segment as FreeHandBezierSegment;
                            figure.Segments.Add(new BezierSegment(
                                new Point(bezier.X1, bezier.Y1),
                                new Point(bezier.X2, bezier.Y2),
                                new Point(bezier.X3, bezier.Y3),
                                true));
                        }
                    }

                    geometry.Figures.Add(figure);
                }

                path.Fill = new SolidColorBrush(Colors.Red);
                path.Stroke = new SolidColorBrush(Colors.Green);
                path.StrokeThickness = 1;
                path.RenderTransform = transform;

                overlay.Children.Add(path);
            }
        }

        public void Remove(Shape shape)
        {
            ShapeCollection sc = shape.Parent;
            sc.Remove(shape);
        }

        public ContentShape FindShape(Shape shape, TransformGroup parentTransform, Point position)
        {
            ContentShape shapeFound = null;

            if (null == shape || null == parentTransform)
            {
                shape = shapeCollections[0];
                parentTransform = new TransformGroup();
                AddShapeToTransform(shape as ShapeCollection, parentTransform);
            }

            if (shape is ShapeCollection)
            {
                ShapeCollection shapeCollection = shape as ShapeCollection;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(shapeCollection, transform);

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
            }
            else if (shape is TextShape)
            {
                TextShape text = shape as TextShape;
                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(text, transform);

                var rect = transform.TransformBounds(new Rect(new Size(text.MeasuredWidth, text.MeasuredHeight)));

                if (rect.Contains(position))
                    shapeFound = text;
            }
            else if (shape is ImageShape)
            {
                ImageShape image = shape as ImageShape;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(image, transform);

                var rect = transform.TransformBounds(new Rect(new Size(image.Width, image.Height)));
                if (rect.Contains(position))
                    shapeFound = image;
            }
            else if (shape is FreeHandShape)
            {
                FreeHandShape freehand = shape as FreeHandShape;

                TransformGroup transform = new TransformGroup();
                transform.Children.Add(parentTransform);
                AddShapeToTransform(freehand, transform);

                foreach (FreeHandPath freehandPath in freehand.Paths)
                {
                    PathGeometry geometry = new PathGeometry();
                    PathFigure figure = new PathFigure
                    {
                        IsClosed = freehandPath.Closed
                    };
                    foreach (FreeHandSegment segment in freehandPath.Segments)
                    {
                        if (segment is FreeHandStartSegment)
                        {
                            FreeHandStartSegment start = segment as FreeHandStartSegment;
                            figure.StartPoint = new Point(start.X, start.Y);
                        }
                        else if (segment is FreeHandLineSegment)
                        {
                            FreeHandLineSegment line = segment as FreeHandLineSegment;
                            figure.Segments.Add(new LineSegment(new Point(line.X1, line.Y1), true));
                        }
                        else if (segment is FreeHandBezierSegment)
                        {
                            FreeHandBezierSegment bezier = segment as FreeHandBezierSegment;
                            figure.Segments.Add(new BezierSegment(
                                new Point(bezier.X1, bezier.Y1),
                                new Point(bezier.X2, bezier.Y2),
                                new Point(bezier.X3, bezier.Y3),
                                true));
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

            }

            return shapeFound;
        }

        List<ShapeCollection> shapeCollections = new List<ShapeCollection>();
        TreeViewItem selectedItem;
        TreeView parentTree;
        TagsTree tagsTree;
        System.Windows.Controls.Canvas overlay;
    }
}
