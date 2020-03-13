﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class ShapeCollectionViewModel : BaseViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private bool _isMarked;

        private readonly ShapesTreeViewModel _shapesTreeViewModel;

        public ShapeCollectionViewModel(Shape shape, ShapesTreeViewModel shapesTreeViewModel) : this(shape, null, shapesTreeViewModel)
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

        public bool IsMarked
        {
            get => _isMarked;
            set
            {
                _isMarked = value;
                if (value == false)
                {
                    if (Children == null) return;
                    foreach (var child in Children)
                    {
                        child.IsMarked = false;
                    }
                }
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

        public List<ShapeCollectionViewModel> ToList()
        {
            List<ShapeCollectionViewModel> allItems = new List<ShapeCollectionViewModel> {this};
            if (Children == null)
            {
                return allItems;
            }

            foreach (var child in Children)
            {
                var list = child.ToList();
                allItems.AddRange(list);
            }

            return allItems;
        }

        public ShapeCollectionViewModel Select(Shape shape)
        {
            if (Shape == shape)
            {
                IsSelected = true;
                IsExpanded = true;
                return this;
            }
            else
            {
                if (null == Children) return null;
                ShapeCollectionViewModel selectedViewModel = null;
                foreach (var child in Children)
                {
                    if (child.Select(shape) != null)
                    {
                        selectedViewModel = child.Select(shape);
                    }
                }

                return selectedViewModel;
            }
        }
    }
}