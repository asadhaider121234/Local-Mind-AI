using System.Collections.Generic;

namespace DocMind.ViewModels
{
    public class AnswerViewModel : BaseViewModel
    {
        private string _answerText = string.Empty;
        public string AnswerText
        {
            get => _answerText;
            set
            {
                _answerText = value;
                OnPropertyChanged(nameof(AnswerText));
            }
        }

        private List<SourceReference> _sources = new List<SourceReference>();
        public List<SourceReference> Sources
        {
            get => _sources;
            set
            {
                _sources = value;
                OnPropertyChanged(nameof(Sources));
            }
        }
    }
}
