using EarTrumpet.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.Properties;

namespace EarTrumpet.UI.ViewModels
{
    public class AdditionalSettingsViewModel : SettingsPageViewModel
    {
        public ICommand RestoreHiddenApps { get; }

        public AdditionalSettingsViewModel() : base(null)
        {
            Title = Resources.AdditionalSettingsTitle;
            Glyph = "\xE10C";
            RestoreHiddenApps =
                new RelayCommand(() =>
                {
                    StorageFactory.GetSettings().Set("HIDDEN_APPS", new List<string>());
                    MessageBox.Show(Resources.RestoreHiddenAppsDone, Resources.RestoreHiddenAppsButton);
                });
        }
    }
}