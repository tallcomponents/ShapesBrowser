using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using TallComponents.PDF.Configuration;
using TallComponents.PDF.Diagnostics;
using TallComponents.PDF.Rasterizer;
using TallComponents.PDF.Shapes;
using TallComponents.Samples.ShapesBrowser.Other;
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
        private readonly IDialogBoxService _dialogBoxService;
        private ObservableCollection<RectangleViewModel> _canvasItems;
        private double _pageOrientation;
        private double _translateX;
        private double _translateY;
        private double _scaleX;
        private double _scaleY;
        private double _translateOrientationX;
        private double _translateOrientationY;

        public MainWindowViewModel(IDialogBoxService dialogBoxService)
        {
            _dialogBoxService = dialogBoxService;
            SaveCommand = new RelayCommand(Save);
            OpenCommand = new RelayCommand(Open);
            DocumentClickCommand = new PositioningCommand(OnMouseClick);
            DeleteShapeCommand = new RelayCommand<KeyEventArgs>(OnDelete);
            TagsTreeViewModel = new TagsTreeViewModel();
            ShapesTreeViewModel = new ShapesTreeViewModel();
            RecentFilesMenuListViewModel = new RecentFilesMenuListViewModel(4);
            RecentFilesMenuListViewModel.OnFilePathSelected += OnFilePathSelected;
            TagsTreeViewModel.SetShapesTree(ShapesTreeViewModel);
            ShapesTreeViewModel.SetTagsTree(TagsTreeViewModel);
            CanvasItems = new ObservableCollection<RectangleViewModel>();
        }

        public ICommand DeleteShapeCommand { get; set; }
        public ICommand DocumentClickCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public double PageOrientation
        {
            get => _pageOrientation;
            set => SetProperty(ref _pageOrientation, value);
        }

        public double TranslateX
        {
            get => _translateX;
            set => SetProperty(ref _translateX, value);
        }

        public double TranslateY
        {
            get => _translateY;
            set => SetProperty(ref _translateY, value);
        }

        public double TranslateOrientationX
        {
            get => _translateOrientationX;
            set => SetProperty(ref _translateOrientationX, value);
        }

        public double TranslateOrientationY
        {
            get => _translateOrientationY;
            set => SetProperty(ref _translateOrientationY, value);
        }

        public double ScaleX
        {
            get => _scaleX;
            set => SetProperty(ref _scaleX, value);
        }

        public double ScaleY
        {
            get => _scaleY;
            set => SetProperty(ref _scaleY, value);
        }

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

        public ObservableCollection<RectangleViewModel> CanvasItems
        {
            get => _canvasItems;
            set => SetProperty(ref _canvasItems, value);
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

            ShapesTreeViewModel.SetCanvas(CanvasItems);

            PageOrientation = 0;
            TranslateX = 0;
            TranslateY = fixedPage.Height;
            TranslateY = fixedPage.Height;
            ScaleX = fixedPage.Width / page.Width;
            ScaleY = -fixedPage.Height / page.Height;

            switch (page.Orientation)
            {
                case pdf.Orientation.Rotate90:
                    PageOrientation = 90;
                    TranslateOrientationX = 0;
                    TranslateOrientationY = -page.MediaBox.Height;
                    break;
                case pdf.Orientation.Rotate180:
                    PageOrientation = 180;
                    TranslateOrientationX = -page.MediaBox.Width;
                    TranslateOrientationY = -page.MediaBox.Height;
                    break;
                case pdf.Orientation.Rotate270:
                    PageOrientation = 270;
                    TranslateOrientationX = -page.MediaBox.Width;
                    TranslateOrientationY = 0;
                    break;
            }

            ShapesTreeViewModel.Deselect();
        }

        private void InitializePagesList()
        {
            if (null == _currentDocument) return;
            ItemsSource = _currentDocument.Pages;
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

        private void OnMouseClick(Point position)
        {
            Shape shape = ShapesTreeViewModel.FindShape(null, position);
            if (null != shape) ShapesTreeViewModel.Select((ContentShape) shape);
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