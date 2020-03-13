using System.Collections.ObjectModel;
using TallComponents.PDF.Shapes;
using TallComponents.PDF.Tags;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class TagViewModel : BaseViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private readonly TagsTreeViewModel _tagsTreeViewModel;

        public TagViewModel(Tag tag, TagsTreeViewModel shapesTreeViewModel) : this(tag, null, shapesTreeViewModel)
        {
        }

        private TagViewModel(Tag tag, TagViewModel parent, TagsTreeViewModel shapesTreeViewModel)
        {
            _tagsTreeViewModel = shapesTreeViewModel;
            Tag = tag;
            Parent = parent;
            Children = new ObservableCollection<TagViewModel>();
            foreach (var child in tag.Childs)
            {
                if (child is Tag childTag)
                {
                    Children.Add(new TagViewModel(childTag, this, shapesTreeViewModel));
                }
            }
        }

        public ObservableCollection<TagViewModel> Children { get; }

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

        public TagViewModel Parent { get; }
        public ShapeCollectionViewModel Shape { get; private set; }
        public Tag Tag { get; }

        public string Text
        {
            get
            {
                if (Tag.Title != null) Tag.Title = string.Format("<{0}> - {1}", Tag.Type, Tag.Title);
                else
                {
                    if (Tag.Type == null) Tag.Title = "Tags";
                    else Tag.Title = string.Format("<{0}>", Tag.Type);
                }

                return Tag.Title;
            }
        }

        public void Select(Tag shape)
        {
            if (Tag == shape)
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

        public void SetShape(ShapeCollectionViewModel shape)
        {
            ContentShape contentShape = shape.Shape as ContentShape;
            if (Tag == contentShape.ParentTag)
            {
                Shape = shape;
            }
            else
            {
                if (null == Children) return;
                foreach (var child in Children)
                {
                    child.SetShape(shape);
                }
            }
        }
    }
}