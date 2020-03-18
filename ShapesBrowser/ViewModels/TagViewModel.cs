using System.Collections.ObjectModel;
using System.Linq;
using TallComponents.PDF.Shapes;
using TallComponents.PDF.Tags;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class TagViewModel : BaseViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;

        public TagViewModel(Tag tag) : this(tag, null)
        {
        }

        private TagViewModel(Tag tag, TagViewModel parent)
        {
            Tag = tag;
            Parent = parent;
            Children = new ObservableCollection<TagViewModel>();
            foreach (var child in tag.Childs)
            {
                if (child is Tag childTag)
                {
                    Children.Add(new TagViewModel(childTag, this));
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

        public bool SetSelected(Tag tag, bool isSelected)
        {
            if (Tag == tag)
            {
                if (IsSelected == isSelected)
                {
                    return false;
                }
                IsSelected = isSelected;
                IsExpanded = true;
                return true;
            }

            return null != Children && Children.Any(child => child.SetSelected(tag, isSelected));
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