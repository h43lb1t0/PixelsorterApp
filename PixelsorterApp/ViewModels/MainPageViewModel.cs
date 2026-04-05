using CommunityToolkit.Mvvm.Input;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Represents UI state and commands for the main pixel sorting screen.
/// </summary>
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
    private bool isInteractionEnabled = true;
    private int selectedSortByIndex;
    private int selectedSortDirectionIndex;
    private int cannyThresholdPercent = 30;
    private int subjectMaskPadding = 15;
    private bool useInvertedSubjectMask;
    private bool useSubtractMasks = true;
    private string currentCaption = "Tap to load an image";

    private readonly IRelayCommand sortCommand;
    private readonly IRelayCommand saveCommand;
    private readonly IRelayCommand loadImageCommand;
    private readonly IRelayCommand openLicensesCommand;
    private readonly IRelayCommand openPrivacyPolicyCommand;
    private readonly IRelayCommand openHelpCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    public MainPageViewModel()
    {
        sortCommand = new RelayCommand(() => SortRequested?.Invoke(), () => IsSortEnabled);
        saveCommand = new RelayCommand(() => SaveRequested?.Invoke(), () => IsSaveEnabled);
        loadImageCommand = new RelayCommand(() => LoadImageRequested?.Invoke(), () => IsInteractionEnabled);
        openLicensesCommand = new RelayCommand(() => OpenLicensesRequested?.Invoke());
        openPrivacyPolicyCommand = new RelayCommand(() => OpenPrivacyPolicyRequested?.Invoke());
        openHelpCommand = new RelayCommand(() => OpenHelpRequested?.Invoke());

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

    /// <summary>
    /// Gets or sets a value indicating whether the page is busy.
    /// </summary>
    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether subject masking is enabled.
    /// </summary>
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
            OnPropertyChanged(nameof(ShowSubjectPadding));
            OnPropertyChanged(nameof(ShowWhatToSort));
            OnPropertyChanged(nameof(ShowHowToCombine));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether Canny masking is enabled.
    /// </summary>
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
            OnPropertyChanged(nameof(ShowCannyThreshold));
            OnPropertyChanged(nameof(ShowWhatToSort));
            OnPropertyChanged(nameof(ShowHowToCombine));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is currently enabled.
    /// </summary>
    public bool IsSortEnabled
    {
        get => isSortEnabled;
        set
        {
            if (!SetProperty(ref isSortEnabled, value))
            {
                return;
            }

            sortCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the save button should be visible.
    /// </summary>
    public bool IsSaveVisible
    {
        get => isSaveVisible;
        set => SetProperty(ref isSaveVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether saving is currently enabled.
    /// </summary>
    public bool IsSaveEnabled
    {
        get => isSaveEnabled;
        set
        {
            if (!SetProperty(ref isSaveEnabled, value))
            {
                return;
            }

            saveCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether interactive controls should be enabled.
    /// </summary>
    public bool IsInteractionEnabled
    {
        get => isInteractionEnabled;
        set
        {
            if (!SetProperty(ref isInteractionEnabled, value))
            {
                return;
            }

            loadImageCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the caption shown for the currently displayed image.
    /// </summary>
    public string CurrentCaption
    {
        get => currentCaption;
        set => SetProperty(ref currentCaption, value);
    }

    /// <summary>
    /// Gets or sets the Canny threshold value in percent (1-99).
    /// </summary>
    public int CannyThresholdPercent
    {
        get => cannyThresholdPercent;
        set
        {
            int clamped = Math.Clamp(value, 1, 99);
            if (!SetProperty(ref cannyThresholdPercent, clamped))
            {
                return;
            }

            OnPropertyChanged(nameof(CannyThresholdText));
        }
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
    /// Gets or sets the subject mask padding in pixels (1-100).
    /// </summary>
    public int SubjectMaskPadding
    {
        get => subjectMaskPadding;
        set
        {
            int clamped = Math.Clamp(value, 1, 100);
            if (!SetProperty(ref subjectMaskPadding, clamped))
            {
                return;
            }

            OnPropertyChanged(nameof(SubjectMaskPaddingText));
        }
    }

    /// <summary>
    /// Gets the formatted subject mask padding label.
    /// </summary>
    public string SubjectMaskPaddingText => $"{SubjectMaskPadding} px";

    /// <summary>
    /// Gets or sets a value indicating whether the subject mask should be inverted.
    /// </summary>
    public bool UseInvertedSubjectMask
    {
        get => useInvertedSubjectMask;
        set
        {
            if (!SetProperty(ref useInvertedSubjectMask, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SortBackgroundSelected));
            OnPropertyChanged(nameof(SortForegroundSelected));
        }
    }

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
    /// Gets or sets a value indicating whether masks should be combined by subtraction.
    /// </summary>
    public bool UseSubtractMasks
    {
        get => useSubtractMasks;
        set
        {
            if (!SetProperty(ref useSubtractMasks, value))
            {
                return;
            }

            OnPropertyChanged(nameof(UseSubtractMasksSelected));
            OnPropertyChanged(nameof(UseAddMasksSelected));
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

    /// <summary>
    /// Gets the command that requests opening the licenses page.
    /// </summary>
    public IRelayCommand OpenLicensesCommand => openLicensesCommand;

    /// <summary>
    /// Gets the command that requests opening the privacy policy page.
    /// </summary>
    public IRelayCommand OpenPrivacyPolicyCommand => openPrivacyPolicyCommand;

    /// <summary>
    /// Gets the command that requests opening the help menu.
    /// </summary>
    public IRelayCommand OpenHelpCommand => openHelpCommand;

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
    /// Occurs when navigation to licenses is requested.
    /// </summary>
    public event Action? OpenLicensesRequested;

    /// <summary>
    /// Occurs when navigation to privacy policy is requested.
    /// </summary>
    public event Action? OpenPrivacyPolicyRequested;

    /// <summary>
    /// Occurs when opening help actions is requested.
    /// </summary>
    public event Action? OpenHelpRequested;

    /// <summary>
    /// Gets the available sort criteria names.
    /// </summary>
    public IReadOnlyList<string> SortByOptions { get; }

    /// <summary>
    /// Gets the available sort direction names for the current masking configuration.
    /// </summary>
    public ObservableCollection<string> SortDirectionOptions { get; } = [];

    /// <summary>
    /// Gets or sets the selected sort criterion index.
    /// </summary>
    public int SelectedSortByIndex
    {
        get => selectedSortByIndex;
        set => SetProperty(ref selectedSortByIndex, value);
    }

    /// <summary>
    /// Gets or sets the selected sort direction index.
    /// </summary>
    public int SelectedSortDirectionIndex
    {
        get => selectedSortDirectionIndex;
        set => SetProperty(ref selectedSortDirectionIndex, value);
    }

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
}
