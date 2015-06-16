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

namespace WpfApplication1
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        pdf.Document document;
        FileStream file;
        string path;

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
            pdf.Shapes.ShapeCollection shapes = page.CreateShapes();
            (shapesTree.Items[0] as TreeViewItem).ItemsSource = shapes;
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
                documentViewer.Document = document.ConvertToWpf(renderSettings, convertOptions, index, index, summary);
            }
        }
    }
}
