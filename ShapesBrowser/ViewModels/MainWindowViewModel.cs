using System;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TallComponents.PDF.Configuration;
using TallComponents.PDF.Diagnostics;
using TallComponents.PDF.Rasterizer;
using TallComponents.PDF.Shapes;
using TallComponents.Samples.ShapesBrowser.Other;
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
        private readonly IDialogBoxService _dialogBoxService;

        public MainWindowViewModel(IDialogBoxService dialogBoxService)
        {
            _dialogBoxService = dialogBoxService;
            SaveCommand = new RelayCommand(Save);
            OpenCommand = new RelayCommand(Open);
            DocumentClickCommand = new RelayCommand(OnMouseClick);
            DocumentModifiedClickCommand = new RelayCommand(OnModifiedMouseClick);
            DeleteShapeCommand = new RelayCommand<KeyEventArgs>(OnDelete);
            TagsTreeViewModel = new TagsTreeViewModel();
            ShapesTreeViewModel = new ShapesTreeViewModel();
            RecentFilesMenuListViewModel = new RecentFilesMenuListViewModel(4);
            RecentFilesMenuListViewModel.OnFilePathSelected += OnFilePathSelected;
            TagsTreeViewModel.SetShapesTree(ShapesTreeViewModel);
            ShapesTreeViewModel.SetTagsTree(TagsTreeViewModel);
        }

        public ICommand DeleteShapeCommand { get; set; }
        public ICommand DocumentClickCommand { get; set; }
        public ICommand DocumentModifiedClickCommand { get; set; }
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
        public RecentFilesMenuListViewModel RecentFilesMenuListViewModel { get; }

        private void OnFilePathSelected(string arg)
        {
            try
            {
                _loader.Open(arg);
                AfterOpen(arg);
            }
            catch (Exception ex)
            {
                RecentFilesMenuListViewModel.RemoveFile(arg);
                _dialogBoxService.ShowMessage(ex.Message);
            }
        }

        private static pdf.Page Copy(pdf.Page page, ShapeCollection shapeCollection = null)
        {
            var newPage = new pdf.Page(page.Width, page.Height);
            var shapes = shapeCollection ?? page.CreateShapes();
            newPage.Overlay.Add(shapes);
            return newPage;
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

            _overlay = new Canvas();
            _overlay.InputBindings.Add(new InputBinding(DeleteShapeCommand, new KeyGesture(Key.Delete)));
            var mouseGesture = new MouseGesture {MouseAction = MouseAction.LeftClick};
            _overlay.InputBindings.Add(new InputBinding(DocumentClickCommand, mouseGesture));
            mouseGesture = new MouseGesture {MouseAction = MouseAction.LeftClick, Modifiers = ModifierKeys.Control};
            _overlay.InputBindings.Add(new InputBinding(DocumentModifiedClickCommand, mouseGesture));
            ShapesTreeViewModel.SetCanvas(_overlay);
            _overlay.Width = fixedPage.Width;
            _overlay.Height = fixedPage.Height;
            _overlay.Background = Brushes.Transparent;


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
            if (null == _currentDocument) return;
            ItemsSource = _currentDocument.Pages;
        }

        private void OnDelete(KeyEventArgs e)
        {
            ShapesTreeViewModel.RemoveSelectedItems();
            var root = ShapesTreeViewModel.GetRoot();
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

        private void OnModifiedMouseClick()
        {
            MouseClick(true);
        }

        private void OnMouseClick()
        {
            MouseClick(false);
        }

        private void MouseClick(bool modified)
        {
            var pos = Mouse.GetPosition(_overlay);
            Shape shape = ShapesTreeViewModel.FindShape(null, null, pos);
            if (null != shape) ShapesTreeViewModel.Select((ContentShape) shape, modified);
        }

        private void Open()
        {
            _dialogBoxService.Filter = "PDF files (*.pdf)|*.pdf";
            var fileName = _dialogBoxService.OpenFileDialog(string.Empty);
            if (string.IsNullOrEmpty(fileName)) return;

            _loader.Open(fileName);
            AfterOpen(fileName);
        }

        private void AfterOpen(string filePath)
        {
            _currentDocument = _loader.GetCurrentDocument();
            RecentFilesMenuListViewModel.AddFilePath(filePath);
            TagsTreeViewModel.Initialize(_currentDocument);
            OnPropertyChanged(nameof(TagsTreeViewModel));
            InitializePagesList();
            SelectedIndex = 0;
            DrawPage(0);
        }

        private void Save()
        {
            _dialogBoxService.Filter = "PDF files (*.pdf)|*.pdf";
            var fileName = _dialogBoxService.SaveFileDialog(string.Empty);
            if (string.IsNullOrEmpty(fileName)) return;

            _loader.Save(fileName);
        }

        private void SelectionChanged()
        {
            if (!(SelectedItem is pdf.Page page)) return;
            ShapesTreeViewModel.Initialize(page);
            DrawPage(page.Index);
        }
    }
}