using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private int _selectedTabIndex;
        private pdf.Page _selectedItem;
        private readonly Loader _loader = new Loader();
        private readonly IDialogBoxService _dialogBoxService;
        private ObservableCollection<RectangleViewModel> _canvasItems;
        private ObservableCollection<TabItemViewModel> _tabs;
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
            CloseTabCommand = new RelayCommand<TabItemViewModel>(CloseTab);
            SelectTabCommand = new RelayCommand<string>(OnSelectTab);

            SelectSingleItemCommand = new PositioningCommand(OnSelectSingleItem);
            SelectItemsContinuouslyCommand = new PositioningCommand(OnSelectItemsContinuously);
            SelectItemsContinuouslyBidirectionallyCommand = new PositioningCommand(OnSelectItemsContinuouslyBidirectionally);
            SelectItemsRandomlyCommand = new PositioningCommand(OnSelectItemsRandomly);

            ViewShapePropertiesCommand = new RelayCommand(OnViewProperties);
            DeleteShapeCommand = new RelayCommand(OnDelete);
            KeepShapesCommand = new RelayCommand(OnKeepShapes);

            TagsTreeViewModel = new TagsTreeViewModel();
            ShapesTreeViewModel = new ShapesTreeViewModel();
            RecentFilesMenuListViewModel = new RecentFilesMenuListViewModel(4);
            RecentFilesMenuListViewModel.OnFilePathSelected += OnFilePathSelected;
            TagsTreeViewModel.SetShapesTree(ShapesTreeViewModel);
            ShapesTreeViewModel.SetTagsTree(TagsTreeViewModel);
            CanvasItems = new ObservableCollection<RectangleViewModel>();
            Tabs = new ObservableCollection<TabItemViewModel>();

            ShapesTreeViewModel.SetCanvas(CanvasItems);
        }

        private void OnSelectTab(string tabIndex)
        {
            int index = int.Parse(tabIndex);
            if(index >= Tabs.Count)
            {
                index = Tabs.Count - 1;
            }
            SelectedTabIndex = index;
        }

        public ICommand ViewShapePropertiesCommand { get; set; }
        public ICommand SelectItemsRandomlyCommand { get; set; }
        public ICommand SelectItemsContinuouslyBidirectionallyCommand { get; set; }
        public ICommand SelectItemsContinuouslyCommand { get; set; }
        public ICommand KeepShapesCommand { get; set; }
        public ICommand DeleteShapeCommand { get; set; }
        public ICommand SelectSingleItemCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand CloseTabCommand { get; }
        public ICommand SelectTabCommand { get; }
        public ICommand SaveCommand { get; set; }

        public ObservableCollection<TabItemViewModel> Tabs
        {
            get => _tabs;
            set => SetProperty(ref _tabs, value);
        }

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

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                SetProperty(ref _selectedTabIndex, value);
                SelectedTabIndexChanged();
             }
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

        public ShapesTreeViewModel ShapesTreeViewModel { get; set; }
        public TagsTreeViewModel TagsTreeViewModel { get; }
        public RecentFilesMenuListViewModel RecentFilesMenuListViewModel { get; }

        private void OnFilePathSelected(string arg)
        {
            try
            {
                AfterOpen(arg);
            }
            catch (FileNotFoundException ex)
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
            var currentTabDocument = Tabs[SelectedTabIndex].Document;
            var page = currentTabDocument.Pages[index];
            var renderSettings = new RenderSettings();
            var summary = new Summary();
            var convertOptions = new ConvertToWpfOptions();
            var fixedDocument = currentTabDocument.ConvertToWpf(renderSettings, convertOptions, index, index, summary);
            Tabs[SelectedTabIndex].FixedDocument = fixedDocument;
            var fixedPage = fixedDocument.Pages[0].Child;

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
            if (SelectedTabIndex == -1) return;
            ItemsSource = Tabs[SelectedTabIndex].Document.Pages;
        }

        private void OnKeepShapes()
        {
            var pdfOut = new pdf.Document(); ;
            var selectedItems = ShapesTreeViewModel.GetSelectedItems();
            var page = Tabs[SelectedTabIndex].Document.Pages[SelectedIndex];
            var newPage = new pdf.Page(page.Width, page.Height);

            foreach (var shapeCollectionViewModel in selectedItems)
            {
                newPage.Overlay.Add(shapeCollectionViewModel.Shape);
            }
            pdfOut.Pages.Add(newPage);
            ReloadFile(pdfOut);
        }

        private void OnDelete()
        {
            ShapesTreeViewModel.RemoveSelectedItems();
            var root = ShapesTreeViewModel.GetRoot();
            var pdfOut = new pdf.Document();
            var selectedPage = SelectedItem;
            foreach (var page in Tabs[SelectedTabIndex].Document.Pages)
            {
                pdfOut.Pages.Add(page == selectedPage ? Copy(page, root[0] as ShapeCollection) : Copy(page));
            }
            ReloadFile(pdfOut);
        }

        private void OnViewProperties()
        {
            var selectedItems = ShapesTreeViewModel.GetSelectedItems();
            var selectedItem = selectedItems.FirstOrDefault();
            if (selectedItem == null) return;
            var shapeProperties = new ShapePropertiesViewModel(selectedItem.Shape);
            _dialogBoxService.ShowWindow(shapeProperties);
        }

        private void ReloadFile(pdf.Document pdfOut)
        {
            _loader.SaveTempFile(pdfOut, SelectedTabIndex);
            Tabs[SelectedTabIndex].Document.Close();
            _loader.CloseCurrentFile(SelectedTabIndex, true);
            var prevIndex = SelectedIndex;
            Tabs[SelectedTabIndex].Document = _loader.OpenTempFile(SelectedTabIndex);
            TagsTreeViewModel.Initialize(Tabs[SelectedTabIndex].Document);
            InitializePagesList();
            SelectedIndex = prevIndex;
            DrawPage(prevIndex);
        }

        private void OnSelectSingleItem(Point position)
        {
            Shape shape = ShapesTreeViewModel.FindShape(null, position);
            if (null != shape) ShapesTreeViewModel.SelectSingleItem((ContentShape) shape);
        }

        private void OnSelectItemsContinuously(Point position)
        {
            Shape shape = ShapesTreeViewModel.FindShape(null, position);
            if (null != shape) ShapesTreeViewModel.SelectItemsContinuously((ContentShape)shape);
        }
        private void OnSelectItemsContinuouslyBidirectionally(Point position)
        {
            Shape shape = ShapesTreeViewModel.FindShape(null, position);
            if (null != shape) ShapesTreeViewModel.SelectItemsContinuouslyBidirectionally((ContentShape)shape);
        }

        private void OnSelectItemsRandomly(Point position)
        {
            Shape shape = ShapesTreeViewModel.FindShape(null, position);
            if (null != shape) ShapesTreeViewModel.SelectItemsRandomly((ContentShape)shape);
        }
        private void Open()
        {
            _dialogBoxService.Filter = "PDF files (*.pdf)|*.pdf";
            var fileName = _dialogBoxService.OpenFileDialog(string.Empty);
            if (string.IsNullOrEmpty(fileName)) return;

            AfterOpen(fileName);
        }

        private void AfterOpen(string filePath)
        {
            var _currentDocument = _loader.Open(filePath, Tabs.Count);
            RecentFilesMenuListViewModel.AddFilePath(filePath);
            TagsTreeViewModel.Initialize(_currentDocument);
            OnPropertyChanged(nameof(TagsTreeViewModel));

            var header = string.IsNullOrEmpty(_currentDocument.DocumentInfo.Title) ? "[no title]" : _currentDocument.DocumentInfo.Title;
            var item = new TabItemViewModel { Header = header, Document = _currentDocument };
            Tabs.Add(item);
            SelectedTabIndex = Tabs.Count - 1;
            InitializePagesList();
            SelectedIndex = 0;
            DrawPage(0);
        }

        private void Save()
        {
            _dialogBoxService.Filter = "PDF files (*.pdf)|*.pdf";
            var fileName = _dialogBoxService.SaveFileDialog(string.Empty);
            if (string.IsNullOrEmpty(fileName)) return;

            _loader.Save(fileName, Tabs[SelectedTabIndex].Document);
        }

        private void SelectionChanged()
        {
            if (!(SelectedItem is pdf.Page page)) return;
            ShapesTreeViewModel.Initialize(page);
            DrawPage(page.Index);
            SelectedIndex = page.Index;
        }

        private void SelectedTabIndexChanged()
        {
            if (!(SelectedItem is pdf.Page page) || SelectedTabIndex == -1) return;
            ShapesTreeViewModel.Initialize(page);
            InitializePagesList();
            SelectedIndex = page.Index;
            DrawPage(page.Index);
        }

        private void CloseTab(TabItemViewModel index)
        {
            _loader.CloseCurrentFile(Tabs.IndexOf(index));
            Tabs.Remove(index);
            if (Tabs.Count != 0) return;
            ItemsSource = null;
            ShapesTreeViewModel.Clear();
            TagsTreeViewModel.Clear();
        }
    }
}