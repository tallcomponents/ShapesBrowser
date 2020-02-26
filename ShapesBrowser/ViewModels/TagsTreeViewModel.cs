using System.Collections.ObjectModel;
using TallComponents.PDF;
using TallComponents.PDF.Shapes;
using TallComponents.PDF.Tags;

namespace TallComponents.Samples.ShapesBrowser
{
    public class TagsTreeViewModel : BaseViewModel
    {
        private TagViewModel _rootTagViewModel;
        private ShapesTreeViewModel _shapesTreeViewModel;
        private bool _suppressChangeEvent;
        private ObservableCollection<TagViewModel> _viewItems;

        public ObservableCollection<TagViewModel> ViewItems
        {
            get => _viewItems;
            private set => SetProperty(ref _viewItems, value);
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
            try
            {
                _suppressChangeEvent = true;
                if (null != shape.ParentTag)
                {
                    _rootTagViewModel.Select(shape.ParentTag);
                }
            }
            finally
            {
                _suppressChangeEvent = false;
            }
        }

        public void SelectedItemChanged(TagViewModel tagVM)
        {
            if (_suppressChangeEvent) return;
            if (tagVM.IsSelected)
            {
                _shapesTreeViewModel.Select(tagVM.Shape as ContentShape);
            }
        }

        public void SetShape(ContentShape shape)
        {
            _rootTagViewModel.SetShape(shape);
        }

        public void SetShapesTree(ShapesTreeViewModel shapesTreeViewModel)
        {
            _shapesTreeViewModel = shapesTreeViewModel;
        }
    }
}