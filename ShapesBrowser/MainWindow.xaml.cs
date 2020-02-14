using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using pdf = TallComponents.PDF;
using TallComponents.PDF.Shapes;

namespace TallComponents.Samples.ShapesBrowser
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tagsTree = new TagsTree(baseTagsTree);
            shapesTree = new ShapesTree(baseShapesTree);
            tagsTree.SetShapesTree(shapesTree);
            shapesTree.SetTagsTree(tagsTree);
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                currentPath = dialog.FileName;
                CloseCurrentFile();
                MakeTempFile();
                Open(tempPath);
                InitializePagesList();
                pagesList.SelectedIndex = 0;
            }
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                if(dialog.FileName == currentPath)
                {
                    currentDocument.Write(currentFile);
                }
                else
                {
                    using(FileStream file = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        currentDocument.Write(file);
                    }
                }
            }
        }

        void MakeTempFile()
        {
            tempPath = Path.GetTempFileName();
            File.Copy(currentPath, tempPath, true);
        }

        void Open(string path)
        {
            currentFile = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            currentDocument = new pdf.Document(currentFile);
            tagsTree.Initialize(currentDocument);
        }

        void CloseCurrentFile()
        {
            if (null != currentFile)
            {
                currentFile.Dispose();
                currentFile = null;
                currentDocument = null;
                currentPath = null;
            }
        }

        void InitializePagesList()
        {
            if (null != currentDocument)
            {
                pagesList.ItemsSource = currentDocument.Pages;
            }
        }

        private void PagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pagesList.SelectedItem is pdf.Page page)
            {
                shapesTree.Initialize(page);
                DrawPage(page.Index);
            }
        }

        void DrawPage(int index)
        {
            pdf.Page page = currentDocument.Pages[index];

            pdf.Configuration.RenderSettings renderSettings = new pdf.Configuration.RenderSettings() ;
            pdf.Diagnostics.Summary summary = new pdf.Diagnostics.Summary();
            pdf.Rasterizer.ConvertToWpfOptions convertOptions = new pdf.Rasterizer.ConvertToWpfOptions();
            FixedDocument fixedDocument = currentDocument.ConvertToWpf(renderSettings, convertOptions, index, index, summary);

            documentViewer.Document = fixedDocument;

            FixedPage fixedPage = fixedDocument.Pages[0].Child;

            // add a canvas and change its coordinate system to that of PDF
            overlay = new System.Windows.Controls.Canvas();
            shapesTree.SetCanvas(overlay);
            overlay.Width = fixedPage.Width;
            overlay.Height = fixedPage.Height;
            TransformGroup group = new TransformGroup();
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
                default:
                    break;
            }

            overlay.RenderTransform = group;
            fixedPage.Children.Add(overlay);
        }

        private void ShapesTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            shapesTree.SelectedItemChanged();
        }

        private void TagsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            tagsTree.SelectedItemChanged();
        }

        private void DocumentViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = Mouse.GetPosition(overlay);
            Shape shape = shapesTree.FindShape(null, null, pos);
            if (null != shape)
                shapesTree.Select(shape as ContentShape);
        }

        private void DocumentViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                Shape shape = shapesTree.GetSelectedShape();

                if (null == shape)
                    return;
                ShapeCollection root = FindRoot(shape.Parent);

                shapesTree.Remove(shape);

                pdf.Document pdfOut = new pdf.Document();

                pdf.Page selectedPage = pagesList.SelectedItem as pdf.Page;
                foreach (pdf.Page page in currentDocument.Pages)
                {
                    pdfOut.Pages.Add((page == selectedPage) ? Copy(page, root[0] as ShapeCollection): Copy(page));
                }

                CloseCurrentFile();
                using (FileStream file = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    pdfOut.Write(file);
                }

                int prevIndex = pagesList.SelectedIndex;
                Open(tempPath);
                InitializePagesList();
                pagesList.SelectedIndex = prevIndex;
                DrawPage(prevIndex);
            }
        }

        static pdf.Page Copy(pdf.Page page, ShapeCollection shapeCollection = null)
        {
            pdf.Page newPage = new pdf.Page(page.Width, page.Height);

            ShapeCollection shapes = shapeCollection ?? page.CreateShapes();
            newPage.Overlay.Add(shapes);

            return newPage;
        }

        ShapeCollection FindRoot(ShapeCollection sc)
        {
            while(sc.Parent != null)
            {
                sc = sc.Parent;
            }
            return sc;
        }

        string tempPath;
        string currentPath;
        pdf.Document currentDocument;
        FileStream currentFile;
        TagsTree tagsTree;
        ShapesTree shapesTree;
        System.Windows.Controls.Canvas overlay;
    }
}
