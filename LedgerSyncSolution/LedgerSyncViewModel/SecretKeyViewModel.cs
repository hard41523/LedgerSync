using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using LedgerSyncViewModel.Helper;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

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

        [RelayCommand]
        public void SecretKeyViewLoad()
        {
            if (!string.IsNullOrEmpty(SecretKeyModels.ApiKey) && !string.IsNullOrEmpty(SecretKeyModels.ApiSecret))
                Ioc.Default.GetService<ShellViewModel>().QuerySecretKey();
        }

        [RelayCommand]
        public void SaveSecretKey()
        {
            if (!ValidateKeys(SecretKeyModels.ApiKey, SecretKeyModels.ApiSecret))
                return;

            Ioc.Default.GetService<ShellViewModel>().InsertSecretKey();
        }

        /// <summary>
        /// Binance API keys are 64-character alphanumeric strings.
        /// Validates both key and secret before saving.
        /// </summary>
        private bool ValidateKeys(string apiKey, string apiSecret)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                MessageBox.Show("API Key and Secret cannot be empty.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Binance keys: 64 alphanumeric characters
            var keyPattern = new Regex(@"^[A-Za-z0-9]{64}$");

            if (!keyPattern.IsMatch(apiKey.Trim()))
            {
                MessageBox.Show("Invalid API Key format. Binance keys must be 64 alphanumeric characters.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!keyPattern.IsMatch(apiSecret.Trim()))
            {
                MessageBox.Show("Invalid API Secret format. Binance secrets must be 64 alphanumeric characters.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}
