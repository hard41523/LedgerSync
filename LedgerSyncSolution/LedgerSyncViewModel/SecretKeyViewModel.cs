using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using LedgerSyncViewModel.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedgerSyncViewModel
{
    public partial class SecretKeyViewModel : ObservableObject
    {
        public SecretKeyViewModel()
        {
            secretKeyModels = new SecretKeyModel();
        }

        [ObservableProperty]
        private SecretKeyModel secretKeyModels;

        // FIX: removed async - no await used, async void is dangerous (swallows exceptions)
        [RelayCommand]
        public void SecretKeyViewLoad()
        {
            if (!string.IsNullOrEmpty(SecretKeyModels.ApiKey) && !string.IsNullOrEmpty(SecretKeyModels.ApiSecret))
            {
                Ioc.Default.GetService<ShellViewModel>().QuerySecretKey();
            }
        }

        // FIX: removed async - no await used
        [RelayCommand]
        public void SaveSecretKey()
        {
            if (!string.IsNullOrEmpty(SecretKeyModels.ApiKey) && !string.IsNullOrEmpty(SecretKeyModels.ApiSecret))
            {
                Ioc.Default.GetService<ShellViewModel>().InsertSecretKey();
            }
        }
    }
}
