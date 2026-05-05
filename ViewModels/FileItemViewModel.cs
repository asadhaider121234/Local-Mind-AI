namespace DocMind.ViewModels
{
    public class FileItemViewModel : BaseViewModel
    {
        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(nameof(FileName)); }
        }

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(nameof(FilePath)); }
        }

        private string _fileType = string.Empty;
        public string FileType
        {
            get => _fileType;
            set { _fileType = value; OnPropertyChanged(nameof(FileType)); }
        }

        private double _fileSizeKB;
        public double FileSizeKB
        {
            get => _fileSizeKB;
            set { _fileSizeKB = value; OnPropertyChanged(nameof(FileSizeKB)); }
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set { _pageCount = value; OnPropertyChanged(nameof(PageCount)); }
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        private string _lastModified = string.Empty;
        public string LastModified
        {
            get => _lastModified;
            set { _lastModified = value; OnPropertyChanged(nameof(LastModified)); }
        }

        private bool _isIndexed = true;
        public bool IsIndexed
        {
            get => _isIndexed;
            set { _isIndexed = value; OnPropertyChanged(nameof(IsIndexed)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }
    }
}
