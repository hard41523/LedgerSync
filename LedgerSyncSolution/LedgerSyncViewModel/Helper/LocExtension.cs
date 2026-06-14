using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace LedgerSyncViewModel.Helper
{
    public class LocExtension : MarkupExtension, INotifyPropertyChanged
    {
        public string Key { get; set; }

        // FIX: store handler reference so we can unsubscribe and avoid memory leak
        private EventHandler _languageChangedHandler;

        public LocExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // FIX: unsubscribe previous handler before subscribing to avoid duplicate handlers
            if (_languageChangedHandler != null)
                LocalizationManager.OnLanguageChanged -= _languageChangedHandler;

            _languageChangedHandler = (_, __) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

            LocalizationManager.OnLanguageChanged += _languageChangedHandler;

            // FIX: return 'this' so WPF can bind to Value property and receive PropertyChanged
            // Previously returned Value (plain string) - WPF had no way to update on language change
            return this;
        }

        public object Value => LocalizationManager.Get(Key);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
