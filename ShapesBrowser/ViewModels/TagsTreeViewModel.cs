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
        public void DeselectAll()
        {
            var selectedItems = SelectedItems.ToList();
            foreach (var item in selectedItems)
            {
                item.IsSelected = false;
            }
        }

        public void Initialize(Document document)
        {
            if (document.LogicalStructure == null) document.LogicalStructure = new LogicalStructure();
            var logicalStructure = document.LogicalStructure;
            _rootTagViewModel = new TagViewModel(logicalStructure.RootTag, this);
            ViewItems = new ObservableCollection<TagViewModel>(new[] {_rootTagViewModel});
        }

        public void Select(ContentShape shape)
        {
            if (null == shape.ParentTag) return;
            _suppressChangeEvent = true;
            _rootTagViewModel.Select(shape.ParentTag);
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
            var collection = sender as ObservableCollection<TagViewModel>;

            for (var i = 0; i < collection.Count; i++)
            {
                var tagVM = collection[i];
                if (tagVM.Shape != null && tagVM.IsSelected && !tagVM.Shape.IsSelected && !_suppressChangeEvent)
                {
                    _shapesTreeViewModel.Select(tagVM.Shape.Shape as ContentShape, MainWindowViewModel.Modifiers.None);
                }
            }

            _suppressChangeEvent = false;
        }
    }
}