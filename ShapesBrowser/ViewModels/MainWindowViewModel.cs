﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using TallComponents.PDF.Configuration;
using TallComponents.PDF.Diagnostics;
using TallComponents.PDF.Rasterizer;
using TallComponents.PDF.Shapes;
using TallComponents.Samples.ShapesBrowser.MenuViewModel;
using Canvas = System.Windows.Controls.Canvas;
using pdf = TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class MainWindowViewModel : BaseViewModel
    {
        private FixedDocument _fixedDocument;
        private pdf.PageCollection _itemSource;
        private int _selectedIndex;
        private pdf.Page _selectedItem;
        private pdf.Document _currentDocument;
        private readonly Loader _loader = new Loader();
        private Canvas _overlay;
        private readonly RecentFilesMenuListViewModel _recentFilesMenuListViewModel;

        public MainWindowViewModel()
        {
            SaveCommand = new RelayCommand(Save);
            OpenCommand = new RelayCommand(Open);
            DocumentClickCommand = new RelayCommand(OnMouseClick);
            DeleteShapeCommand = new RelayCommand<KeyEventArgs>(OnDelete);
            TagsTreeViewModel = new TagsTreeViewModel();
            ShapesTreeViewModel = new ShapesTreeViewModel();
            _recentFilesMenuListViewModel = new RecentFilesMenuListViewModel(4);
            _recentFilesMenuListViewModel.OnFilePathSelected += OnFilePathSelected;
            TagsTreeViewModel.SetShapesTree(ShapesTreeViewModel);
            ShapesTreeViewModel.SetTagsTree(TagsTreeViewModel);
        }

        public ICommand DeleteShapeCommand { get; set; }
        public ICommand DocumentClickCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public FixedDocument FixedDocument
        {
            get => _fixedDocument;
            set => SetProperty(ref _fixedDocument, value);
        }

        public pdf.PageCollection ItemsSource
        {
            get => _itemSource;
            set => SetProperty(ref _itemSource, value);
        }

        public ObservableCollection<MenuItemViewModel> MenuItems => _recentFilesMenuListViewModel.MenuItems;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
        }

        public pdf.Page SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                SelectionChanged();
            }
        }

        public ShapesTreeViewModel ShapesTreeViewModel { get; }
        public TagsTreeViewModel TagsTreeViewModel { get; }

        private static pdf.Page Copy(pdf.Page page, ShapeCollection shapeCollection = null)
        {
            var newPage = new pdf.Page(page.Width, page.Height);
            var shapes = shapeCollection ?? page.CreateShapes();
            newPage.Overlay.Add(shapes);
            return newPage;
        }

        private void AfterOpen(string filePath)
        {
            _currentDocument = _loader.GetCurrentDocument();
            _recentFilesMenuListViewModel.AddFilePath(filePath);
            TagsTreeViewModel.Initialize(_currentDocument);
            OnPropertyChanged(nameof(TagsTreeViewModel));
            InitializePagesList();
            SelectedIndex = 0;
            DrawPage(0);
        }

        private void DrawPage(int index)
        {
            var page = _currentDocument.Pages[index];
            var renderSettings = new RenderSettings();
            var summary = new Summary();
            var convertOptions = new ConvertToWpfOptions();
            var fixedDocument = _currentDocument.ConvertToWpf(renderSettings, convertOptions, index, index, summary);
            FixedDocument = fixedDocument;
            var fixedPage = fixedDocument.Pages[0].Child;

            var mouseGesture = new MouseGesture {MouseAction = MouseAction.LeftClick};
            fixedPage.InputBindings.Add(new InputBinding(DocumentClickCommand, mouseGesture));

            _overlay = new Canvas();
            _overlay.InputBindings.Add(new InputBinding(DeleteShapeCommand, new KeyGesture(Key.Delete)));
            ShapesTreeViewModel.SetCanvas(_overlay);
            _overlay.Width = fixedPage.Width;
            _overlay.Height = fixedPage.Height;

            var group = new TransformGroup();
            group.Children.Insert(0, new TranslateTransform(0, fixedPage.Height));
            group.Children.Insert(0, new ScaleTransform(fixedPage.Width / page.Width, -fixedPage.Height / page.Height));
            switch (page.Orientation)
            {
                case pdf.Orientation.Rotate90:
                    group.Children.Insert(0, new RotateTransform(90));
                    group.Children.Insert(0, new TranslateTransform(0, -page.MediaBox.Height));
                    break;
                case pdf.Orientation.Rotate180:
                    group.Children.Insert(0, new RotateTransform(180));
                    group.Children.Insert(0, new TranslateTransform(-page.MediaBox.Width, -page.MediaBox.Height));
                    break;
                case pdf.Orientation.Rotate270:
                    group.Children.Insert(0, new RotateTransform(270));
                    group.Children.Insert(0, new TranslateTransform(-page.MediaBox.Width, 0));
                    break;
            }

            _overlay.RenderTransform = group;
            fixedPage.Children.Add(_overlay);
            ShapesTreeViewModel.Deselect();
        }

        private void InitializePagesList()
        {
            if (null != _currentDocument)
            {
                ItemsSource = _currentDocument.Pages;
            }
        }

        private void OnDelete(KeyEventArgs e)
        {
            var shape = ShapesTreeViewModel.GetSelectedShape();
            if (null == shape) return;
            var root = ShapesTreeViewModel.FindRoot(shape.Parent);
            ShapesTreeViewModel.Remove(shape);
            var pdfOut = new pdf.Document();
            var selectedPage = SelectedItem;
            foreach (var page in _currentDocument.Pages)
            {
                pdfOut.Pages.Add(page == selectedPage ? Copy(page, root[0] as ShapeCollection) : Copy(page));
            }

            _loader.SaveTempFile(pdfOut);
            _loader.CloseCurrentFile();
            var prevIndex = SelectedIndex;
            _loader.OpenTempFile();
            _currentDocument = _loader.GetCurrentDocument();
            TagsTreeViewModel.Initialize(_currentDocument);
            InitializePagesList();
            SelectedIndex = prevIndex;
            DrawPage(prevIndex);
        }

        private void OnFilePathSelected(string arg)
        {
            try
            {
                _loader.Open(arg);
                AfterOpen(arg);
            }
            catch (Exception ex)
            {
                _recentFilesMenuListViewModel.RemoveFile(arg);
                MessageBox.Show(ex.Message);
            }
        }

        private void OnMouseClick()
        {
            var pos = Mouse.GetPosition(_overlay);
            Shape shape = ShapesTreeViewModel.FindShape(null, null, pos);
            if (null != shape) ShapesTreeViewModel.Select(shape as ContentShape);
        }

        private void Open()
        {
            var dialog = new OpenFileDialog {Filter = "PDF files (*.pdf)|*.pdf"};
            if (dialog.ShowDialog() == true)
            {
                _loader.Open(dialog.FileName);
                AfterOpen(dialog.FileName);
            }
        }

        private void Save()
        {
            var dialog = new SaveFileDialog {Filter = "PDF files (*.pdf)|*.pdf"};
            if (dialog.ShowDialog() == true)
            {
                _loader.Save(dialog.FileName);
            }
        }

        private void SelectionChanged()
        {
            if (SelectedItem is pdf.Page page)
            {
                ShapesTreeViewModel.Initialize(page);
                DrawPage(page.Index);
            }
        }
    }
}