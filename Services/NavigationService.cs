using System;
using System.Collections.Generic;
using System.Windows.Controls;
using DocMind.Views.Pages;

namespace DocMind.Services;

public class NavigationService
{
    private static NavigationService? _instance;
    public static NavigationService Instance => _instance ??= new NavigationService();

    private Frame? _mainFrame;
    public event Action<string>? Navigated;

    private readonly Dictionary<string, Page> _pageCache = new();

    public void Initialize(Frame frame)
    {
        _mainFrame = frame;
        // Navigation without journaling
        _mainFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
    }

    public void Navigate(string targetPageId)
    {
        if (_mainFrame == null) return;

        try
        {
            if (!_pageCache.TryGetValue(targetPageId, out var page))
            {
                page = targetPageId switch
                {
                    "Search" => new Page_Search(),
                    "Library" => new Page_Library(),
                    "Categories" => new Page_Categories(),
                    "Duplicates" => new Page_Duplicates(),
                    "Evaluation" => new Page_Evaluation(),
                    "Settings" => new Page_Settings(),
                    _ => throw new Exception($"Unknown page: {targetPageId}")
                };
                _pageCache[targetPageId] = page;
            }

            _mainFrame.Navigate(page);
            SettingsService.Instance.LastPage = targetPageId;
            Navigated?.Invoke(targetPageId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to navigate to {targetPageId}: {ex.Message}");
        }
    }

    public Page? GetPage(string targetPageId)
    {
        _pageCache.TryGetValue(targetPageId, out var page);
        return page;
    }
}
