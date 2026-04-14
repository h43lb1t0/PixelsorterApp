using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.ViewModels
{
    public class PresetsPageViewModel : BaseViewModel
    {
        private readonly MainPageViewModel _mainViewModel;

        public PresetsPageViewModel(MainPageViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
    }
}
