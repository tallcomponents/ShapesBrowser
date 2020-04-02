using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using TallComponents.PDF;
using TallComponents.PDF.Shapes;
using TallComponents.PDF.Tags;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class TagsTreeViewModel : BaseViewModel
    {
        private TagViewModel _rootTagViewModel;
        private ObservableCollection<TagViewModel> _selectedItemsViewModel;
        private ShapesTreeViewModel _shapesTreeViewModel;
        private bool _suppressChangeEvent;
        private bool _suppressTagDeselection;

        private ObservableCollection<TagViewModel> _viewItems;
        public TagsTreeViewModel()
        {
            SelectedItems = new ObservableCollection<TagViewModel>();
            SelectedItems.CollectionChanged += OnSelectedItemsCollectionChanged;
        }

        public ObservableCollection<TagViewModel> SelectedItems
        {
            get => _selectedItemsViewModel;
            set => SetProperty(ref _selectedItemsViewModel, value);
        }

        public ObservableCollection<TagViewModel> ViewItems
        {
            get => _viewItems;
            private set => SetProperty(ref _viewItems, value);
        }

        public void Initialize(Document document)
        {
            if (document.LogicalStructure == null) document.LogicalStructure = new LogicalStructure();
            var logicalStructure = document.LogicalStructure;
            _rootTagViewModel = new TagViewModel(logicalStructure.RootTag);
            ViewItems = new ObservableCollection<TagViewModel>(new[] {_rootTagViewModel});
        }

        public void Select(ContentShape shape)
        {
            if (null == shape.ParentTag) return;
            _suppressChangeEvent = true;
            if (!_rootTagViewModel.SetSelected(shape.ParentTag, true))
            {
                _suppressChangeEvent = false;
            }
            _suppressTagDeselection = false;
        }

        public void Deselect(ContentShape shape)
        {
            if (_suppressTagDeselection) return;
            if (null == shape.ParentTag) return;
            _suppressChangeEvent = true;
            if (!_rootTagViewModel.SetSelected(shape.ParentTag, false))
            {
                _suppressChangeEvent = false;
            }
        }

        public void SetShape(ShapeCollectionViewModel shape)
        {
            _rootTagViewModel.SetShape(shape);
        }

        public void SetShapesTree(ShapesTreeViewModel shapesTreeViewModel)
        {
            _shapesTreeViewModel = shapesTreeViewModel;
        }

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressChangeEvent)
            {
                _suppressTagDeselection = true;
                _shapesTreeViewModel.Deselect();
                var collection = sender as ObservableCollection<TagViewModel>;
                for (var i = 0; i < collection.Count; i++)
                {
                    var tagVM = collection[i];
                    if (tagVM.Shape != null && tagVM.IsSelected && !tagVM.Shape.IsSelected)
                    {
                        _suppressTagDeselection = true;
                        _shapesTreeViewModel.SelectItemsRandomly(tagVM.Shape.Shape as ContentShape);
                    }
                }
            }
            _suppressChangeEvent = false;
        }

        public void Clear()
        {
            ViewItems.Clear();
        }
    }
}