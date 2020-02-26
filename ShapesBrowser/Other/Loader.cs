using System.IO;
using pdf = TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class Loader
    {
        private pdf.Document _currentDocument;
        private FileStream _currentFile;
        private string _currentPath;
        private string _tempPath;

        public void CloseCurrentFile()
        {
            if (null != _currentFile)
            {
                _currentDocument.Close();
                _currentFile.Flush();
                _currentFile.Close();
                _currentFile.Dispose();
                _currentFile = null;
                _currentDocument = null;
                _currentPath = null;
            }
        }

        public pdf.Document GetCurrentDocument()
        {
            return _currentDocument;
        }

        public void Open(string path)
        {
            _currentPath = path;
            MakeTempFile();
            OpenTempFile();
        }

        public void OpenFile(string path)
        {
            _currentFile = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _currentDocument = new pdf.Document(_currentFile);
        }

        public void OpenTempFile()
        {
            _currentFile = new FileStream(_tempPath, FileMode.Open, FileAccess.ReadWrite);
            _currentDocument = new pdf.Document(_currentFile);
        }

        public void Save(string path)
        {
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                _currentDocument.Write(file);
            }
        }

        public void SaveTempFile(pdf.Document tempDocument)
        {
            tempDocument.Write(_currentFile);
        }

        private void MakeTempFile()
        {
            _tempPath = Path.GetTempFileName();
            File.Copy(_currentPath, _tempPath, true);
        }
    }
}