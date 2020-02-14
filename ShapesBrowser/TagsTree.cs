using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TallComponents.PDF.Shapes;
using TallComponents.PDF.Tags;
using TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{
    class TagAndShape
    {
        public TagAndShape(Tag tag, ContentShape shape)
        {
            this.tag = tag;
            this.shape = shape;
        }

        public Tag tag = null;
        public ContentShape shape = null;
    }

    class TagsTree
    {
        public TagsTree(TreeView parentTree)
        {
            this.parentTree = parentTree;
        }

        public void SetShapesTree(ShapesTree shapesTree)
        {
            this.shapesTree = shapesTree;
        }

        public void Select(ContentShape shape)
        {
            if (null != shape.ParentTag)
            {
                var tagPath = GetTagPath(shape.ParentTag);
                Select(tagPath);
            }
            else
            {
                if (null != SelectedItem)
                    SelectedItem.IsSelected = false;
            }
        }

        List<Tag> GetTagPath(Tag tag)
        {
            List<Tag> tagPath = new List<Tag>();

            if (null == tag)
                return tagPath;

            tagPath.Add(tag);

            var parent = tag.ParentTag;
            while (null != parent)
            {
                tagPath.Insert(0, parent);
                parent = parent.ParentTag;
            }

            return tagPath;
        }

        public void AddTag(TreeViewItem tvItem, Tag tag)
        {
            TreeViewItem item = new TreeViewItem();

            if (tag.Title != null)
                item.Header = String.Format("<{0}> - {1}", tag.Type, tag.Title);
            else
            {
                if (tag.Type == null)
                    item.Header = String.Format("Tags");
                else
                    item.Header = String.Format("<{0}>", tag.Type);
            }
            item.Tag = new TagAndShape(tag, null);

            if (null == tvItem)
                parentTree.Items.Add(item);
            else
                tvItem.Items.Add(item);

            foreach (var child in tag.Childs)
            {
                if (child is Tag)
                {
                    AddTag(item, child as Tag);
                }
            }
        }

        public void Initialize(Document document)
        {
            parentTree.Items.Clear();

            if (document.LogicalStructure == null)
                document.LogicalStructure = new LogicalStructure();

            var logicalStructure = document.LogicalStructure;
            AddTag(null, logicalStructure.RootTag);
        }

        public void SelectedItemChanged()
        {
            if (suppressChangeEvent) return;

            if (!(parentTree.SelectedItem is TreeViewItem selectedItem))
                return;

            if (selectedItem.Tag is TagAndShape tagAndShape)
                shapesTree.Select(tagAndShape.shape);
        }

        private TreeViewItem Find(Tag tag, TreeViewItem treeItem)
        {
            if (null == tag)
                return null;

            if (null == treeItem && tag.Type == null)
            {
                System.Diagnostics.Debug.Assert(tag.Equals(((parentTree.Items[0] as TreeViewItem).Tag as TagAndShape).tag));
                return parentTree.Items[0] as TreeViewItem;
            }

            for (int i = 0; i < treeItem.Items.Count; i++)
            {
                var itemAsTVI = treeItem.Items[i] as TreeViewItem;
                if (itemAsTVI.Tag is TagAndShape tagAndShape && tag.Equals(tagAndShape.tag))
                    return itemAsTVI;
            }

            return null;
        }

        TreeViewItem FindTreeViewItem(List<Tag> tagPath, bool expand)
        {
            if (null == tagPath || tagPath.Count == 0)
                return null;

            TreeViewItem item = null;

            for (int i = 0; i < tagPath.Count; i++)
            {
                item = Find(tagPath[i], item);
                if (null != item)
                {
                    if (expand)
                        item.IsExpanded = true;
                }
                else
                    break;
            }

            return item;
        }

        public void Select(List<Tag> tagPath)
        {
            try
            {
                suppressChangeEvent = true;

                var item = FindTreeViewItem(tagPath, true);

                if (null != item)
                {
                    item.IsSelected = true;
                    item.BringIntoView();
                    SelectedItem = item;
                }

            }
            finally
            {
                suppressChangeEvent = false;
            }
        }

        public void SetShape(ContentShape shape)
        {
            if (null == shape || null == shape.ParentTag)
                return;

            var tagPath = GetTagPath(shape.ParentTag);
            var item = FindTreeViewItem(tagPath, false);

            if (null != item)
            {
                if (item.Tag is TagAndShape tagAndShape)
                    tagAndShape.shape = shape;
            }
        }

        TreeViewItem SelectedItem;

        TreeView parentTree;
        ShapesTree shapesTree;

        bool suppressChangeEvent;
    }
}
