using DocMind.ViewModels;

namespace DocMind.Models;

public enum IndexStatus { Ready, NotIndexed, Indexing }

public class NavigationItem : BaseViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;      // Segoe Fluent / emoji glyph
    public string TargetPage { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
