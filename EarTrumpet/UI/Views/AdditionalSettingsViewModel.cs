using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.Properties;
using EarTrumpet.UI.Helpers;

namespace EarTrumpet.UI.ViewModels
{
    public class AdditionalSettingsViewModel : SettingsPageViewModel
    {
        public AdditionalSettingsViewModel() : base(null)
        {
            Title = Resources.AboutTitle; //AdditionalSettingsTitle;
            Glyph = "\xE10C";
            RestoreHiddenApps =
                new RelayCommand(() =>
                {
                    StorageFactory.GetSettings().Set("HIDDEN_APPS", new List<string>());
                    MessageBox.Show("");
                });
        }

        public ICommand RestoreHiddenApps { get; }
    }
}