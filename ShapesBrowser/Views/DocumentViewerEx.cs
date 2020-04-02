using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class DocumentViewerEx : DocumentViewer
    {
        public FrameworkElement AttachToPage
        {
            get => (FrameworkElement) GetValue(AttachToPageProperty);
            set => SetValue(AttachToPageProperty, value);
        }

        public static readonly DependencyProperty AttachToPageProperty =
            DependencyProperty.RegisterAttached("AttachToPage", typeof(FrameworkElement), typeof(DocumentViewerEx));
        protected override void OnDocumentChanged()
        {
            base.OnDocumentChanged();

            if (Document == null) return;
            var fixedPage = (Document as FixedDocument)?.Pages[0].Child;
            var dc = AttachToPage.DataContext;
            AttachToPage.Width = fixedPage.Width ;
            AttachToPage.Height = fixedPage.Height;

            RemoveElementFromItsParent(AttachToPage);
            fixedPage.Children.Add(AttachToPage);
            AttachToPage.DataContext = dc;
        }

        private static void RemoveElementFromItsParent(FrameworkElement el)
        {
            switch (el.Parent)
            {
                case null:
                    return;
                case Panel panel:
                    panel.Children.Remove(el);
                    return;
                case Decorator decorator:
                    decorator.Child = null;
                    return;
                case ContentPresenter contentPresenter:
                    contentPresenter.Content = null;
                    return;
                case ContentControl contentControl:
                    contentControl.Content = null;
                    return;
                case FixedPage fixedPage:
                    fixedPage.Children.Remove(el);
                    break;
            }
        }
    }
}