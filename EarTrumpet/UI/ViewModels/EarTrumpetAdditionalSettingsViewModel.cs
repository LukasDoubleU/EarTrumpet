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

        public int DefaultAppVolume
        {
            get => StorageFactory.GetSettings().Get("DefaultAppVolume", 100);
            set => StorageFactory.GetSettings().Set("DefaultAppVolume", value);
        }
    }
}