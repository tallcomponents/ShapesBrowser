using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Microsoft.Win32;

using pdf = TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        pdf.Document document;
        FileStream file;
        string path;
        Canvas overlay;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "PDF files (*.pdf)|*.pdf";
            if (dialog.ShowDialog() == true)
            {
                close();
                open(dialog.FileName);
            }
        }

        void open(string path)
        {
            this.path = path;
            file = new FileStream(path, FileMode.Open, FileAccess.Read);
            document = new pdf.Document(file);
            initializePagesList();
        }

        void close()
        {
            if (null != file)
            {
                file.Dispose();
                file = null;
                document = null;
                path = null;
            }
        }

        void initializePagesList()
        {
            if (null != document)
            {
                pagesList.ItemsSource = document.Pages;
                pagesList.SelectedIndex = 0;
            }
        }

        private void pagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pdf.Page page = pagesList.SelectedItem as pdf.Page;
            if (null != page)
            {
                initializeShapesTree(page);
                drawPage(page.Index);
            }
        }

        void initializeShapesTree(pdf.Page page)
        {
            ShapeCollectionConverter.Reset();

            pdf.Shapes.ShapeCollection shapes = page.CreateShapes();
            pdf.Shapes.ShapeCollection root = new pdf.Shapes.ShapeCollection();
            root.Add(shapes);
            (shapesTree.Items[0] as TreeViewItem).ItemsSource = root;
            (shapesTree.Items[0] as TreeViewItem).IsExpanded = true;
        }

        void drawPage(int index)
        {
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                pdf.Rasterizer.Document document = new pdf.Rasterizer.Document(file);
                pdf.Rasterizer.Page page = document.Pages[index];

                pdf.Rasterizer.Configuration.RenderSettings renderSettings = new pdf.Rasterizer.Configuration.RenderSettings();
                pdf.Rasterizer.Diagnostics.Summary summary = new pdf.Rasterizer.Diagnostics.Summary();
                pdf.Rasterizer.ConvertToWpfOptions convertOptions = new pdf.Rasterizer.ConvertToWpfOptions();
                FixedDocument fixedDocument = document.ConvertToWpf(renderSettings, convertOptions, index, index, summary);

                documentViewer.Document = fixedDocument;

                FixedPage fixedPage = fixedDocument.Pages[0].Child;

                // add a canvas and change its coordinate system to that of PDF
                overlay = new Canvas();
                overlay.Width = fixedPage.Width;
                overlay.Height = fixedPage.Height;
                TransformGroup group = new TransformGroup();
                group.Children.Add(new ScaleTransform(fixedPage.Width / page.Width, -fixedPage.Height / page.Height));
                group.Children.Add(new TranslateTransform(0, fixedPage.Height));
                overlay.RenderTransform = group;
                fixedPage.Children.Add(overlay);

                // now draw on the canvas in PDF coordinate space
                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                line.Stroke = new SolidColorBrush(Colors.Red);
                line.StrokeThickness = 3;
                line.X1 = line.Y1 = 0;
                line.X2 = page.Width;
                line.Y2 = page.Height;
                overlay.Children.Add(line);
            }
        }

        private void shapesTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            overlay.Children.Clear();

            pdf.Shapes.ContentShape shape = shapesTree.SelectedItem as pdf.Shapes.ContentShape;
            if (null != shape)
            {
                TransformGroup group = new TransformGroup(); 
                Matrix matrix = shape.Transform.CreateWpfMatrix();
                group.Children.Add(new MatrixTransform(matrix));

                pdf.Shapes.ShapeCollection parent = ShapeCollectionConverter.GetParent(shape);
                while (null != parent)
                {
                    matrix = parent.Transform.CreateWpfMatrix();
                    group.Children.Add(new MatrixTransform(matrix));

                    parent = ShapeCollectionConverter.GetParent(parent);
                }
                
                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
                rectangle.RenderTransform = group;
                
                rectangle.Fill = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0));

                if (shape is pdf.Shapes.TextShape)
                {
                    pdf.Shapes.TextShape text = shape as pdf.Shapes.TextShape;
                    rectangle.Width = text.MeasuredWidth;
                    rectangle.Height = text.MeasuredHeight;

                    overlay.Children.Add(rectangle);
                }
                else if (shape is pdf.Shapes.ImageShape)
                {
                    pdf.Shapes.ImageShape image = shape as pdf.Shapes.ImageShape;
                    rectangle.Width = image.Width;
                    rectangle.Height = image.Height;

                    overlay.Children.Add(rectangle);
                }
                else if (shape is pdf.Shapes.FreeHandShape)
                {
                    pdf.Shapes.FreeHandShape freehand = shape as pdf.Shapes.FreeHandShape;

                    System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                    PathGeometry geometry = new PathGeometry();
                    path.Data = geometry;

                    foreach (pdf.Shapes.FreeHandPath freehandPath in freehand.Paths)
                    {
                        PathFigure figure = new PathFigure();
                        figure.IsClosed = freehandPath.Closed;

                        foreach (pdf.Shapes.FreeHandSegment segment in freehandPath.Segments)
                        {
                            if (segment is pdf.Shapes.FreeHandStartSegment)
                            {
                                pdf.Shapes.FreeHandStartSegment start = segment as pdf.Shapes.FreeHandStartSegment;
                                figure.StartPoint = new Point(start.X, start.Y);
                            }
                            else if (segment is pdf.Shapes.FreeHandLineSegment)
                            {
                                pdf.Shapes.FreeHandLineSegment line = segment as pdf.Shapes.FreeHandLineSegment;
                                figure.Segments.Add(new LineSegment(new Point(line.X1, line.Y1), true));
                            }
                            else if (segment is pdf.Shapes.FreeHandBezierSegment)
                            {
                                pdf.Shapes.FreeHandBezierSegment bezier = segment as pdf.Shapes.FreeHandBezierSegment;
                                figure.Segments.Add(new BezierSegment(
                                    new Point(bezier.X1, bezier.Y1),
                                    new Point(bezier.X2, bezier.Y2),
                                    new Point(bezier.X3, bezier.Y3),
                                    true));
                            }
                        }

                        geometry.Figures.Add(figure);
                    }

                    path.Fill = new SolidColorBrush(Colors.Red);
                    path.Stroke = new SolidColorBrush(Colors.Green);
                    path.StrokeThickness = 1;
                    path.RenderTransform = group;

                    overlay.Children.Add(path);

                }
            }
        }
    }
}
