namespace DocMind.ViewModels
{
    public class SearchResultViewModel : BaseViewModel
    {
        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        private int _pageNumber;
        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                _pageNumber = value;
                OnPropertyChanged(nameof(PageNumber));
            }
        }

        private string _excerpt = string.Empty;
        public string Excerpt
        {
            get => _excerpt;
            set
            {
                _excerpt = value;
                OnPropertyChanged(nameof(Excerpt));
            }
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        private double _relevanceScore;
        public double RelevanceScore
        {
            get => _relevanceScore;
            set
            {
                _relevanceScore = value;
                OnPropertyChanged(nameof(RelevanceScore));
            }
        }

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }
    }
}
