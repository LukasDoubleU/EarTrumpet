using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.Properties;
using EarTrumpet.UI.Helpers;

namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetAdditionalSettingsViewModel : SettingsPageViewModel
    {
        public EarTrumpetAdditionalSettingsViewModel() : base(null)
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
        
        public ICommand RestoreHiddenApps { get; }

        public bool ChangeCommDevice
        {
            get => StorageFactory.GetSettings().Get("ChangeCommDevice", false);
            set => StorageFactory.GetSettings().Set("ChangeCommDevice", value);
        }

        public int DefaultAppVolume
        {
            get => StorageFactory.GetSettings().Get("DefaultAppVolume", 100);
            set => StorageFactory.GetSettings().Set("DefaultAppVolume", value);
        }
        
        public bool PersistAppVolumes
        {
            get => StorageFactory.GetSettings("PersistVolume").Get("PersistAppVolumes", false);
            set => StorageFactory.GetSettings("PersistVolume").Set("PersistAppVolumes", value);
        }
    }
}