using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace PixelsorterApp.ViewModels;

public sealed class MainPageViewModel : BaseViewModel
{
    private readonly Dictionary<string, Func<Hsl, float>> sortByOptions = SortBy.GetAllSortingCriteria();
    private readonly Dictionary<string, SortDirections> sortDirectionOptions = [];

    private bool isBusy;
    private bool useSubjectMask;
    private bool useCanny;
    private bool isSortEnabled;
    private bool isSaveVisible;
    private bool isSaveEnabled;
    private int selectedSortByIndex;
    private int selectedSortDirectionIndex;
    private string currentCaption = "Tap to load an image";

    public MainPageViewModel()
    {
        foreach (SortDirections direction in Enum.GetValues(typeof(SortDirections)))
        {
            string name = Regex.Replace(direction.ToString(), "([A-Z])", " $1").Trim();
            sortDirectionOptions[name] = direction;
        }

        SortByOptions = [.. sortByOptions.Keys];
        SelectedSortByIndex = SortByOptions.Count > 0 ? 0 : -1;

        RefreshSortDirectionOptions();
        SelectedSortDirectionIndex = SortDirectionOptions.Count > 0 ? 0 : -1;
    }

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    public bool UseSubjectMask
    {
        get => useSubjectMask;
        set
        {
            if (!SetProperty(ref useSubjectMask, value))
            {
                return;
            }

            RefreshSortDirectionOptions();
        }
    }

    public bool UseCanny
    {
        get => useCanny;
        set
        {
            if (!SetProperty(ref useCanny, value))
            {
                return;
            }

            RefreshSortDirectionOptions();
        }
    }

    public bool IsSortEnabled
    {
        get => isSortEnabled;
        set => SetProperty(ref isSortEnabled, value);
    }

    public bool IsSaveVisible
    {
        get => isSaveVisible;
        set => SetProperty(ref isSaveVisible, value);
    }

    public bool IsSaveEnabled
    {
        get => isSaveEnabled;
        set => SetProperty(ref isSaveEnabled, value);
    }

    public string CurrentCaption
    {
        get => currentCaption;
        set => SetProperty(ref currentCaption, value);
    }

    public IReadOnlyList<string> SortByOptions { get; }

    public ObservableCollection<string> SortDirectionOptions { get; } = [];

    public int SelectedSortByIndex
    {
        get => selectedSortByIndex;
        set => SetProperty(ref selectedSortByIndex, value);
    }

    public int SelectedSortDirectionIndex
    {
        get => selectedSortDirectionIndex;
        set => SetProperty(ref selectedSortDirectionIndex, value);
    }

    public Func<Hsl, float>? SortingCriterion =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? sortByOptions[SortByOptions[SelectedSortByIndex]]
            : null;

    public SortDirections SortingDirection =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? sortDirectionOptions[SortDirectionOptions[SelectedSortDirectionIndex]]
            : SortDirections.RowRightToLeft;

    public string SelectedSortByName =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? SortByOptions[SelectedSortByIndex]
            : "Unknown";

    public string SelectedSortDirectionName =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? SortDirectionOptions[SelectedSortDirectionIndex]
            : "Unknown";

    private void RefreshSortDirectionOptions()
    {
        string? previousSelection =
            SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
                ? SortDirectionOptions[SelectedSortDirectionIndex]
                : null;

        var filtered = sortDirectionOptions.Keys
            .Where(name => UseSubjectMask || UseCanny || !name.Contains("mask", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SortDirectionOptions.Clear();
        foreach (var option in filtered)
        {
            SortDirectionOptions.Add(option);
        }

        if (SortDirectionOptions.Count == 0)
        {
            SelectedSortDirectionIndex = -1;
            return;
        }

        if (!string.IsNullOrEmpty(previousSelection))
        {
            int previousIndex = SortDirectionOptions.IndexOf(previousSelection);
            SelectedSortDirectionIndex = previousIndex >= 0 ? previousIndex : 0;
            return;
        }

        if (SelectedSortDirectionIndex < 0 || SelectedSortDirectionIndex >= SortDirectionOptions.Count)
        {
            SelectedSortDirectionIndex = 0;
        }
    }
}
