using System.Windows.Documents;
using pdf = TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{
    public class TabItemViewModel : BaseViewModel
    {
        private FixedDocument _fixedDocument;
        public string Header { get; set; }
        public pdf.Document Document { get; set; }
        public FixedDocument FixedDocument
        {
            get => _fixedDocument;
            set => SetProperty(ref _fixedDocument, value);
        }
    }
}
