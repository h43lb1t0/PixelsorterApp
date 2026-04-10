using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelsorterApp.Services;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Represents UI state and commands for the main pixel sorting screen.
/// </summary>
public sealed partial class MainPageViewModel : BaseViewModel
{
    private readonly IHelpNavigationService helpNavigationService;
    private readonly Dictionary<string, Func<Hsl, float>> sortByOptions = SortBy.GetAllSortingCriteria();
    private readonly Dictionary<string, SortDirections> sortDirectionOptions = [];

    /// <summary>
    /// Gets or sets a value indicating whether the page is busy.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subject masking is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSubjectPadding))]
    [NotifyPropertyChangedFor(nameof(ShowWhatToSort))]
    [NotifyPropertyChangedFor(nameof(ShowHowToCombine))]
    public partial bool UseSubjectMask { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Canny masking is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCannyThreshold))]
    [NotifyPropertyChangedFor(nameof(ShowWhatToSort))]
    [NotifyPropertyChangedFor(nameof(ShowHowToCombine))]
    public partial bool UseCanny { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSortEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the save button should be visible.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSaveVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether saving is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSaveEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether interactive controls should be enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsInteractionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the selected sort criterion index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedSortByIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected sort direction index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedSortDirectionIndex { get; set; }

    /// <summary>
    /// Gets or sets the Canny threshold value in percent (1-99).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CannyThreshold))]
    [NotifyPropertyChangedFor(nameof(CannyThresholdText))]
    public partial int CannyThresholdPercent { get; set; } = 30;

    /// <summary>
    /// Gets or sets the subject mask padding in pixels (1-100).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubjectMaskPaddingText))]
    public partial int SubjectMaskPadding { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether the subject mask should be inverted.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortBackgroundSelected))]
    [NotifyPropertyChangedFor(nameof(SortForegroundSelected))]
    public partial bool UseInvertedSubjectMask { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether masks should be combined by subtraction.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseSubtractMasksSelected))]
    [NotifyPropertyChangedFor(nameof(UseAddMasksSelected))]
    public partial bool UseSubtractMasks { get; set; } = true;

    /// <summary>
    /// Gets or sets the caption shown for the currently displayed image.
    /// </summary>
    [ObservableProperty]
    public partial string CurrentCaption { get; set; } = "Tap to load an image";

    private readonly IRelayCommand sortCommand;
    private readonly IRelayCommand saveCommand;
    private readonly IRelayCommand loadImageCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    public MainPageViewModel(IHelpNavigationService helpNavigationService)
    {
        this.helpNavigationService = helpNavigationService;

        sortCommand = new RelayCommand(() => SortRequested?.Invoke(), () => IsSortEnabled);
        saveCommand = new RelayCommand(() => SaveRequested?.Invoke(), () => IsSaveEnabled);
        loadImageCommand = new RelayCommand(() => LoadImageRequested?.Invoke(), () => IsInteractionEnabled);

        foreach (SortDirections direction in Enum.GetValues<SortDirections>())
        {
            string name = Regex.Replace(direction.ToString(), "([A-Z])", " $1").Trim();
            sortDirectionOptions[name] = direction;
        }

        SortByOptions = [.. sortByOptions.Keys];
        SelectedSortByIndex = SortByOptions.Count > 0 ? 0 : -1;

        RefreshSortDirectionOptions();
        SelectedSortDirectionIndex = SortDirectionOptions.Count > 0 ? 0 : -1;
    }

    /// <summary>
    /// Gets the Canny threshold value as a 0-1 floating point number.
    /// </summary>
    public float CannyThreshold => CannyThresholdPercent / 100f;

    /// <summary>
    /// Gets the formatted Canny threshold label.
    /// </summary>
    public string CannyThresholdText => $"{CannyThresholdPercent}%";

    /// <summary>
    /// Gets the formatted subject mask padding label.
    /// </summary>
    public string SubjectMaskPaddingText => $"{SubjectMaskPadding} px";

    /// <summary>
    /// Gets or sets a value indicating whether background sorting is selected.
    /// </summary>
    public bool SortBackgroundSelected
    {
        get => !UseInvertedSubjectMask;
        set
        {
            if (value)
            {
                UseInvertedSubjectMask = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether foreground sorting is selected.
    /// </summary>
    public bool SortForegroundSelected
    {
        get => UseInvertedSubjectMask;
        set
        {
            if (value)
            {
                UseInvertedSubjectMask = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether subtract combination is selected.
    /// </summary>
    public bool UseSubtractMasksSelected
    {
        get => UseSubtractMasks;
        set
        {
            if (value)
            {
                UseSubtractMasks = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether additive combination is selected.
    /// </summary>
    public bool UseAddMasksSelected
    {
        get => !UseSubtractMasks;
        set
        {
            if (value)
            {
                UseSubtractMasks = false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Canny threshold section should be visible.
    /// </summary>
    public bool ShowCannyThreshold => UseCanny;

    /// <summary>
    /// Gets a value indicating whether the subject padding section should be visible.
    /// </summary>
    public bool ShowSubjectPadding => UseSubjectMask;

    /// <summary>
    /// Gets a value indicating whether the foreground/background selection section should be visible.
    /// </summary>
    public bool ShowWhatToSort => UseSubjectMask && !UseCanny;

    /// <summary>
    /// Gets a value indicating whether the mask combination section should be visible.
    /// </summary>
    public bool ShowHowToCombine => UseSubjectMask && UseCanny;

    /// <summary>
    /// Gets the command that requests image sorting.
    /// </summary>
    public IRelayCommand SortCommand => sortCommand;

    /// <summary>
    /// Gets the command that requests saving the focused image.
    /// </summary>
    public IRelayCommand SaveCommand => saveCommand;

    /// <summary>
    /// Gets the command that requests image loading.
    /// </summary>
    public IRelayCommand LoadImageCommand => loadImageCommand;

    [RelayCommand]
    private async Task OpenHelpAsync()
    {
        await helpNavigationService.ShowHelpMenuAsync();
    }

    /// <summary>
    /// Occurs when sorting is requested.
    /// </summary>
    public event Action? SortRequested;

    /// <summary>
    /// Occurs when saving is requested.
    /// </summary>
    public event Action? SaveRequested;

    /// <summary>
    /// Occurs when image loading is requested.
    /// </summary>
    public event Action? LoadImageRequested;

    /// <summary>
    /// Gets the available sort criteria names.
    /// </summary>
    public IReadOnlyList<string> SortByOptions { get; }

    /// <summary>
    /// Gets the available sort direction names for the current masking configuration.
    /// </summary>
    public ObservableCollection<string> SortDirectionOptions { get; } = [];

    /// <summary>
    /// Gets the currently selected sorting criterion delegate.
    /// </summary>
    public Func<Hsl, float>? SortingCriterion =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? sortByOptions[SortByOptions[SelectedSortByIndex]]
            : null;

    /// <summary>
    /// Gets the currently selected sorting direction.
    /// </summary>
    public SortDirections SortingDirection =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? sortDirectionOptions[SortDirectionOptions[SelectedSortDirectionIndex]]
            : SortDirections.RowRightToLeft;

    /// <summary>
    /// Gets the selected sort criterion display name.
    /// </summary>
    public string SelectedSortByName =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? SortByOptions[SelectedSortByIndex]
            : "Unknown";

    /// <summary>
    /// Gets the selected sort direction display name.
    /// </summary>
    public string SelectedSortDirectionName =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? SortDirectionOptions[SelectedSortDirectionIndex]
            : "Unknown";

    /// <summary>
    /// Refreshes sort direction options based on current mask settings and preserves selection when possible.
    /// </summary>
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

    partial void OnUseSubjectMaskChanged(bool value)
    {
        RefreshSortDirectionOptions();
    }

    partial void OnUseCannyChanged(bool value)
    {
        RefreshSortDirectionOptions();
    }

    partial void OnIsSortEnabledChanged(bool value)
    {
        sortCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSaveEnabledChanged(bool value)
    {
        saveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInteractionEnabledChanged(bool value)
    {
        loadImageCommand.NotifyCanExecuteChanged();
    }

    partial void OnCannyThresholdPercentChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 99);
        if (value != clamped)
        {
            CannyThresholdPercent = clamped;
        }
    }

    partial void OnSubjectMaskPaddingChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 100);
        if (value != clamped)
        {
            SubjectMaskPadding = clamped;
        }
    }
}
