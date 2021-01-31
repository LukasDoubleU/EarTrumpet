using System;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.Properties;

namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetAdditionalSettingsViewModel : SettingsPageViewModel
    {
        public EarTrumpetAdditionalSettingsViewModel() : base(null)
        {
            Title = Resources.AdditionalSettingsTitle;
            Glyph = "\xE10C";
        }

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
    }
}