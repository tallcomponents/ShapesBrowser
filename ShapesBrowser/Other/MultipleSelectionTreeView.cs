using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace TallComponents.Samples.ShapesBrowser.Views
{
    public class MultipleSelectionTreeView : TreeView
    {
        public static readonly DependencyProperty IsItemSelectedProperty =
            DependencyProperty.RegisterAttached("IsItemSelected", typeof(bool), typeof(MultipleSelectionTreeView),
                new PropertyMetadata(false, OnIsItemSelectedPropertyChanged));

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(MultipleSelectionTreeView));

        private TreeViewItem _startItem;

        public MultipleSelectionTreeView()
        {
            GotFocus += OnTreeViewItemGotFocus;
            PreviewMouseLeftButtonDown += OnTreeViewItemPreviewMouseDown;
        }

        public static bool GetIsItemSelected(TreeViewItem element)
        {
            return element != null && (bool) element.GetValue(IsItemSelectedProperty);
        }

        public static IList GetSelectedItems(TreeView element)
        {
            return (IList) element?.GetValue(SelectedItemsProperty);
        }

        public static void SetIsItemSelected(TreeViewItem element, bool value)
        {
            if (element == null) return;
            element.IsSelected = false;
            element.SetValue(IsItemSelectedProperty, value);
        }

        public static void SetSelectedItems(TreeView element, IList value)
        {
            element?.SetValue(SelectedItemsProperty, value);
        }

        private static void DeSelectAllItems(TreeView treeView, TreeViewItem treeViewItem)
        {
            if (treeView != null)
            {
                for (var i = 0; i < treeView.Items.Count; i++)
                {
                    if (!(treeView.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)) continue;
                    SetIsItemSelected(item, false);
                    DeSelectAllItems(null, item);
                }
            }
            else
            {
                for (var i = 0; i < treeViewItem.Items.Count; i++)
                {
                    if (!(treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)) continue;
                    SetIsItemSelected(item, false);
                    DeSelectAllItems(null, item);
                }
            }
        }

        private static TreeView FindTreeView(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                return null;
            }

            var treeView = dependencyObject as TreeView;

            return treeView ?? FindTreeView(VisualTreeHelper.GetParent(dependencyObject));
        }

        private static TreeViewItem FindTreeViewItem(DependencyObject dependencyObject)
        {
            if (!(dependencyObject is Visual || dependencyObject is Visual3D))
                return null;

            if (dependencyObject is TreeViewItem treeViewItem)
            {
                return treeViewItem;
            }

            return FindTreeViewItem(VisualTreeHelper.GetParent(dependencyObject));
        }

        private static void GetAllItems(TreeView treeView, TreeViewItem treeViewItem,
            ICollection<TreeViewItem> allItems)
        {
            if (treeView != null)
            {
                for (var i = 0; i < treeView.Items.Count; i++)
                {
                    if (!(treeView.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)) continue;
                    allItems.Add(item);
                    GetAllItems(null, item, allItems);
                }
            }
            else
            {
                for (var i = 0; i < treeViewItem.Items.Count; i++)
                {
                    if (!(treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)) continue;
                    allItems.Add(item);
                    GetAllItems(null, item, allItems);
                }
            }
        }

        private static void OnIsItemSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeViewItem = d as TreeViewItem;
            var treeView = FindTreeView(treeViewItem);
            if (treeViewItem == null || treeView == null) return;

            var selectedItems = GetSelectedItems(treeView);
            if (selectedItems == null) return;

            if (GetIsItemSelected(treeViewItem))
            {
                selectedItems.Add(treeViewItem.DataContext);
            }
            else
            {
                selectedItems.Remove(treeViewItem.DataContext);
            }
        }

        private void OnTreeViewItemGotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeView) return;
            var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
            if (treeViewItem == null || !(sender is TreeView treeView)) return;

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Control | ModifierKeys.Shift:
                    SelectMultipleItemsContinuously(treeView, treeViewItem, true);
                    break;
                case ModifierKeys.Control:
                    SelectMultipleItemsRandomly(treeView, treeViewItem);
                    break;
                case ModifierKeys.Shift:
                    SelectMultipleItemsContinuously(treeView, treeViewItem);
                    break;
                default:
                    SelectSingleItem(treeView, treeViewItem);
                    break;
            }
        }

        private static void OnTreeViewItemPreviewMouseDown(object sender, MouseEventArgs e)
        {
            var treeViewItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
            if (treeViewItem != null) return;

            var treeView = sender as TreeView;
            DeSelectAllItems(treeView, null);
        }

        private void SelectMultipleItemsContinuously(TreeView treeView, TreeViewItem treeViewItem,
            bool isControlPressed = false)
        {
            if (_startItem == null || _startItem == treeViewItem) return;

            ICollection<TreeViewItem> allItems = new List<TreeViewItem>();
            GetAllItems(treeView, null, allItems);
            if (!isControlPressed)
            {
                DeSelectAllItems(treeView, null);
            }

            var isBetween = false;
            foreach (var item in allItems)
            {
                if (item == treeViewItem || item == _startItem)
                {
                    isBetween = !isBetween;

                    SetIsItemSelected(item, true);
                    continue;
                }

                if (isBetween)
                {
                    SetIsItemSelected(item, true);
                }
            }
        }

        private void SelectMultipleItemsRandomly(TreeView treeView, TreeViewItem treeViewItem)
        {
            SetIsItemSelected(treeViewItem, !GetIsItemSelected(treeViewItem));
            if (GetIsItemSelected(treeViewItem))
            {
                _startItem = treeViewItem;
            }

            if (GetSelectedItems(treeView).Count == 0)
            {
                _startItem = null;
            }
        }

        private void SelectSingleItem(TreeView treeView, TreeViewItem treeViewItem)
        {
            DeSelectAllItems(treeView, null);
            SetIsItemSelected(treeViewItem, true);
            _startItem = treeViewItem;
        }
    }
}