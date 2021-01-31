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

        public bool PersistAppVolumes
        {
            get => StorageFactory.GetSettings("PersistVolume").Get("PersistAppVolumes", false);
            set => StorageFactory.GetSettings("PersistVolume").Set("PersistAppVolumes", value);
        }
    }
}