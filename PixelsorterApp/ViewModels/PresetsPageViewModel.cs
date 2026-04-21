using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PixelsorterApp.ViewModels
{
    public sealed partial class PresetsPageViewModel : BaseViewModel
    {
        private readonly MainPageViewModel _mainViewModel;
        private readonly string sortBy;
        private readonly string sortDirection;
        private readonly bool cannyMasking;
        private readonly int cannyThreashold;
        private readonly bool subjectMasking;
        private readonly int subjectPadding;
        private readonly bool subjectBackground;

        private readonly bool subtractMask;

        private readonly string tomlMapPath;

        [ObservableProperty]
        public partial string PresetToml {  get; set; }


        public PresetsPageViewModel(MainPageViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            sortBy = _mainViewModel.SelectedSortByName;
            sortDirection = _mainViewModel.SelectedSortDirectionName;

            cannyMasking = _mainViewModel.UseCanny;
            cannyThreashold = _mainViewModel.CannyThresholdPercent;

            subjectMasking = _mainViewModel.UseSubjectMask;
            subjectPadding = _mainViewModel.SubjectMaskPadding;
            subjectBackground = _mainViewModel.UseInvertedSubjectMask;

            subtractMask = _mainViewModel.UseSubtractMasks;

            tomlMapPath = MainPageViewModel.TomlMapPath;

            PresetToml = CreateToml();
        }

        private string CreateToml()
        {
            StringBuilder sb = new();

            sb.AppendLine("[sort_settings]");
            sb.AppendLine($"sort_by={sortBy}");
            sb.AppendLine($"direction={sortDirection}");
            sb.AppendLine("");

            sb.AppendLine("[masking_options]");
            sb.AppendLine($"use_canny={cannyMasking}");
            sb.AppendLine($"use_subject={subjectMasking}");
            sb.AppendLine("");

            sb.AppendLine("[canny_options]");
            sb.AppendLine($"threashold={cannyThreashold}");
            sb.AppendLine("");

            sb.AppendLine("[subject_settings]");
            sb.AppendLine($"padding={subjectPadding}");
            sb.AppendLine($"what_to_sort={subjectBackground}");
            sb.AppendLine("");

            sb.AppendLine("[mask_combination]");
            sb.AppendLine($"mode={subtractMask}");

            return sb.ToString();


        }
    }
}
