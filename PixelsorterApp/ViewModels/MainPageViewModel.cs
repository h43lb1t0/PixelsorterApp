using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelsorterApp.Models.Presets;
using PixelsorterApp.Services;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Represents UI state and commands for the main pixel sorting screen.
/// </summary>
public sealed partial class MainPageViewModel : BaseViewModel
{
    private readonly IPresetService presetService;
    private readonly IHelpNavigationService helpNavigationService;
    private readonly IPresetNavigationService presetNavigationService;
    private readonly Dictionary<string, Func<Hsl, float>> sortByOptions = SortBy.GetAllSortingCriteria();
    private readonly Dictionary<string, SortDirections> sortDirectionOptions = [];
    private IReadOnlyDictionary<string, string> AvailablePresets = new Dictionary<string, string>();
    private readonly string NewPresetOptionLabel = PixelsorterApp.Resources.Languages.PresetStrings.NewPresetAction;
    private bool suppressPresetSelectionChangedHandling;
    private bool suppressSortDirectionRefresh;
    private bool isNavigatingToPresetPage;
    private string? lastValidPresetOption;


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
    /// Gets or sets the selected sort criterion.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedSortBy { get; set; }

    /// <summary>
    /// Gets or sets the selected sort direction.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedSortDirection { get; set; }

    /// <summary>
    /// Gets or sets the selected preset option.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedPresetOption { get; set; }

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
    public partial string CurrentCaption { get; set; } = PixelsorterApp.Resources.Languages.MainPageStrings.TapToLoadAnImage;

    private readonly IRelayCommand sortCommand;
    private readonly IRelayCommand saveCommand;
    private readonly IRelayCommand loadImageCommand;
    private readonly IRelayCommand shareImageCommand;

    public event Action? ShareRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    public MainPageViewModel(IHelpNavigationService helpNavigationService, IPresetNavigationService presetNavigationService, IPresetService presetService)
    {
        this.helpNavigationService = helpNavigationService;
        this.presetNavigationService = presetNavigationService;
        this.presetService = presetService;

        sortCommand = new RelayCommand(() => SortRequested?.Invoke(), () => IsSortEnabled);
        saveCommand = new RelayCommand(() => SaveRequested?.Invoke(), () => IsSaveEnabled);
        loadImageCommand = new RelayCommand(() => LoadImageRequested?.Invoke(), () => IsInteractionEnabled);
        shareImageCommand = new RelayCommand(() => ShareRequested?.Invoke(), () => IsSaveEnabled);

        foreach (SortDirections direction in Enum.GetValues<SortDirections>())
        {
            string name = Regex.Replace(direction.ToString(), "([A-Z])", " $1").Trim();
            sortDirectionOptions[name] = direction;
        }

        SortByOptions = [.. sortByOptions.Keys];
        SelectedSortBy = SortByOptions.Count > 0 ? SortByOptions[0] : null;

        RefreshSortDirectionOptions();
        SelectedSortDirection = SortDirectionOptions.Count > 0 ? SortDirectionOptions[0] : null;

        RefreshAvailablePresets();
        SelectedPresetOption = this.presetService.FindDefaultPresetOption(AvailablePresets) ?? PresetOptions.FirstOrDefault();
        lastValidPresetOption = SelectedPresetOption;
    }

    public void RefreshAvailablePresets()
    {
        AvailablePresets = this.presetService.GetAvailablePresets();
        PresetOptions.Clear();
        foreach (var presetName in AvailablePresets.Keys)
        {
            PresetOptions.Add(presetName);
        }
        PresetOptions.Add(NewPresetOptionLabel);
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

    public IRelayCommand ShareImageCommand => shareImageCommand;

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
    /// Gets the available preset names.
    /// </summary>
    public ObservableCollection<string> PresetOptions { get; } = [];

    /// <summary>
    /// Gets the currently selected sorting criterion delegate.
    /// </summary>
    public Func<Hsl, float>? SortingCriterion =>
        !string.IsNullOrEmpty(SelectedSortBy) && sortByOptions.TryGetValue(SelectedSortBy, out var criterion)
            ? criterion
            : null;

    /// <summary>
    /// Gets the currently selected sorting direction.
    /// </summary>
    public SortDirections SortingDirection =>
        !string.IsNullOrEmpty(SelectedSortDirection) && sortDirectionOptions.TryGetValue(SelectedSortDirection, out var direction)
            ? direction
            : SortDirections.RowRightToLeft;

    /// <summary>
    /// Gets the selected sort criterion display name.
    /// </summary>
    public string SelectedSortByName => SelectedSortBy ?? PixelsorterApp.Resources.Languages.CommonStrings.common_Unknown;

    /// <summary>
    /// Gets the selected sort direction display name.
    /// </summary>
    public string SelectedSortDirectionName => SelectedSortDirection ?? PixelsorterApp.Resources.Languages.CommonStrings.common_Unknown;

    /// <summary>
    /// Refreshes sort direction options based on current mask settings and preserves selection when possible.
    /// </summary>
    private void RefreshSortDirectionOptions()
    {
        if (suppressSortDirectionRefresh)
        {
            return;
        }

        string? previousSelection = SelectedSortDirection;

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
            SelectedSortDirection = null;
            return;
        }

        if (!string.IsNullOrEmpty(previousSelection) && SortDirectionOptions.Contains(previousSelection))
        {
            SelectedSortDirection = previousSelection;
            return;
        }

        SelectedSortDirection = SortDirectionOptions[0];
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
        shareImageCommand.NotifyCanExecuteChanged();
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

    partial void OnSelectedPresetOptionChanged(string? value)
    {
        if (suppressPresetSelectionChangedHandling)
        {
            return;
        }

        _ = HandleSelectedPresetOptionChangedSafelyAsync(value);
    }

    private async Task HandleSelectedPresetOptionChangedSafelyAsync(string? value)
    {
        try
        {
            await HandleSelectedPresetOptionChangedAsync(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to handle preset selection change: {ex}");
        }
    }

    /// <summary>
    /// Handles changes to the selected preset option asynchronously, navigating to the preset creation page if the user
    /// selects the option to create a new preset.
    /// </summary>
    /// <remarks>If the selected value corresponds to the option for creating a new preset, the method
    /// navigates to the preset creation page and restores the previous selection if necessary. If a valid preset is
    /// selected, the method loads the associated preset.</remarks>
    /// <param name="value">The new preset option selected by the user. This value cannot be null or whitespace.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleSelectedPresetOptionChangedAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (string.Equals(value, NewPresetOptionLabel, StringComparison.Ordinal))
        {
            if (isNavigatingToPresetPage)
            {
                return;
            }

            isNavigatingToPresetPage = true;
            try
            {
                await presetNavigationService.ShowCreatePresetPageAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                isNavigatingToPresetPage = false;
            }

            if (!string.IsNullOrWhiteSpace(lastValidPresetOption))
            {
                suppressPresetSelectionChangedHandling = true;
                SelectedPresetOption = lastValidPresetOption;
                suppressPresetSelectionChangedHandling = false;
            }

            return;
        }

        if (!AvailablePresets.TryGetValue(value, out var presetPath))
        {
            return;
        }

        lastValidPresetOption = value;
        var presetState = await this.presetService.LoadPresetAsync(presetPath);
        if (presetState != null)
        {
            ApplyPresetState(presetState);
        }
    }

    private void ApplyPresetState(PresetState state)
    {
        suppressSortDirectionRefresh = true;
        try
        {
            if (state.UseCanny.HasValue) UseCanny = state.UseCanny.Value;
            if (state.UseSubjectMask.HasValue) UseSubjectMask = state.UseSubjectMask.Value;
            if (state.CannyThresholdPercent.HasValue) CannyThresholdPercent = state.CannyThresholdPercent.Value;
            if (state.SubjectMaskPadding.HasValue) SubjectMaskPadding = state.SubjectMaskPadding.Value;
            if (state.UseInvertedSubjectMask.HasValue) UseInvertedSubjectMask = state.UseInvertedSubjectMask.Value;
            if (state.UseSubtractMasks.HasValue) UseSubtractMasks = state.UseSubtractMasks.Value;
        }
        finally
        {
            suppressSortDirectionRefresh = false;
            RefreshSortDirectionOptions();
        }

        if (!string.IsNullOrEmpty(state.SortByName) && SortByOptions.Contains(state.SortByName))
        {
            SelectedSortBy = state.SortByName;
        }

        if (!string.IsNullOrEmpty(state.DirectionName))
        {
            var displayName = Regex.Replace(state.DirectionName, "([A-Z])", " $1").Trim();
            if (SortDirectionOptions.Contains(displayName)) SelectedSortDirection = displayName;
        }
    }
}
