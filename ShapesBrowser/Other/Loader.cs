using System.Collections.Generic;
using System.IO;
using pdf = TallComponents.PDF;

namespace TallComponents.Samples.ShapesBrowser
{
    internal class Loader
    {
        private List<FileStream> _currentFiles = new List<FileStream>();
        private List<string> _tempPaths = new List<string>();

        public void CloseCurrentFile(int index, bool reload = false)
        {
            if (_currentFiles.Count > index)
            {
                _currentFiles[index].Dispose();
                if(!reload)
                {

                _currentFiles.RemoveAt(index);
                _tempPaths.RemoveAt(index);
                }
            }
        }

        public pdf.Document Open(string path, int index)
        {
            MakeTempFile(path, index);
            return OpenTempFile(index);
        }

        public pdf.Document OpenTempFile(int index)
        {
            if (_currentFiles.Count <= index)
                _currentFiles.Add(new FileStream(_tempPaths[index], FileMode.Open, FileAccess.ReadWrite));
            else
                _currentFiles[index] = new FileStream(_tempPaths[index], FileMode.Open, FileAccess.ReadWrite);

            return new pdf.Document(_currentFiles[index]);
        }

        public void Save(string path, pdf.Document document)
        {
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                document.Write(file);
            }
        }

        public void SaveTempFile(pdf.Document tempDocument, int index)
        {
            tempDocument.Write(_currentFiles[index]);
        }

        private void MakeTempFile(string _currentPath, int index)
        {
            _tempPaths.Add(Path.GetTempFileName());
            File.Copy(_currentPath, _tempPaths[index], true);
        }
    }
}