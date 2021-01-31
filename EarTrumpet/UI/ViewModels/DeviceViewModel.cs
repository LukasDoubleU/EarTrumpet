using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using EarTrumpet.DataModel.Audio;
using EarTrumpet.DataModel.Storage;
using EarTrumpet.DataModel.WindowsAudio;
using EarTrumpet.Extensions;
using EarTrumpet.Properties;

namespace EarTrumpet.UI.ViewModels
{
    public class DeviceViewModel : AudioSessionViewModel, IDeviceViewModel
    {
        public enum DeviceIconKind
        {
            Mute,
            Bar1,
            Bar2,
            Bar3,
            Microphone,
        }

        public string DisplayName => _device.DisplayName;
        public string AccessibleName => IsMuted ? Resources.AppOrDeviceMutedFormatAccessibleText.Replace("{Name}", DisplayName) :
            Resources.AppOrDeviceFormatAccessibleText.Replace("{Name}", DisplayName).Replace("{Volume}", Volume.ToString());
        public string DeviceDescription => ((IAudioDeviceWindowsAudio)_device).DeviceDescription;
        public string EnumeratorName => ((IAudioDeviceWindowsAudio)_device).EnumeratorName;
        public string InterfaceName => ((IAudioDeviceWindowsAudio)_device).InterfaceName;
        public ObservableCollection<IAppItemViewModel> Apps { get; }

        public bool IsDisplayNameVisible
        {
            get => _isDisplayNameVisible;
            set
            {
                if (_isDisplayNameVisible != value)
                {
                    _isDisplayNameVisible = value;
                    RaisePropertyChanged(nameof(IsDisplayNameVisible));
                }
            }
        }

        public DeviceIconKind IconKind
        {
            get => _iconKind;
            set
            {
                if (_iconKind != value)
                {
                    _iconKind = value;
                    RaisePropertyChanged(nameof(IconKind));
                }
            }
        }

        protected readonly IAudioDevice _device;
        protected readonly IAudioDeviceManager _deviceManager;
        protected readonly WeakReference<DeviceCollectionViewModel> _parent;
        private bool _isDisplayNameVisible;
        private DeviceIconKind _iconKind;

        public DeviceViewModel(DeviceCollectionViewModel parent, IAudioDeviceManager deviceManager, IAudioDevice device) : base(device)
        {
            _deviceManager = deviceManager;
            _device = device;
            _parent = new WeakReference<DeviceCollectionViewModel>(parent);
            Apps = new ObservableCollection<IAppItemViewModel>();

            _device.PropertyChanged += OnPropertyChanged;
            _device.Groups.CollectionChanged += OnCollectionChanged;

            foreach (var session in _device.Groups)
            {
                var app = new AppItemViewModel(this, session);
                if (!app.IsHidden)
                {
                    RestorePersistedAppVolume(session.DisplayName, app);
                    ApplyDefaultAppVolume(app);
                    Apps.AddSorted(app, AppItemViewModel.CompareByExeName);   
                }
            }

            UpdateMasterVolumeIcon();
        }

        ~DeviceViewModel()
        {
            _device.PropertyChanged -= OnPropertyChanged;
            _device.Groups.CollectionChanged -= OnCollectionChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_device.IsMuted) ||
                e.PropertyName == nameof(_device.Volume))
            {
                UpdateMasterVolumeIcon();
                RaisePropertyChanged(nameof(AccessibleName));
            }
            else if (e.PropertyName == nameof(_device.DisplayName))
            {
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(AccessibleName));
            }
        }

        public override void UpdatePeakValueForeground()
        {
            base.UpdatePeakValueForeground();

            foreach (var app in Apps)
            {
                app.UpdatePeakValueForeground();
            }
        }

        private void UpdateMasterVolumeIcon()
        {
            if (_device.Parent.Kind == AudioDeviceKind.Recording.ToString())
            {
                IconKind = DeviceIconKind.Microphone;
            }
            else
            {
                if (_device.IsMuted)
                {
                    IconKind = DeviceIconKind.Mute;
                }
                else if (_device.Volume >= 0.66f)
                {
                    IconKind = DeviceIconKind.Bar3;
                }
                else if (_device.Volume >= 0.33f)
                {
                    IconKind = DeviceIconKind.Bar2;
                }
                else
                {
                    IconKind = DeviceIconKind.Bar1;
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    AddSession((IAudioDeviceSession)e.NewItems[0]);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    var existing = Apps.FirstOrDefault(x => x.Id == ((IAudioDeviceSession)e.OldItems[0]).Id);
                    if (existing != null)
                    {
                        Apps.Remove(existing);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void AddSession(IAudioDeviceSession session)
        {
            var newSession = new AppItemViewModel(this, session);

            foreach (var app in Apps)
            {
                if (app.DoesGroupWith(newSession))
                {
                    // Remove the fake app entry after copying any changes the user did.
                    newSession.Volume = app.Volume;
                    newSession.IsMuted = app.IsMuted;
                    Apps.Remove(app);
                    break;
                }
            }
            
            RestorePersistedAppVolume(DisplayName, newSession);
            ApplyDefaultAppVolume(newSession);

            if(!newSession.IsHidden)
            {
                Apps.AddSorted(newSession, AppItemViewModel.CompareByExeName);
            }
        }
        
        private void RestorePersistedAppVolume(string deviceName, AppItemViewModel app)
        {
            var settings = StorageFactory.GetSettings("PersistVolume");
            // Check if this functionality is active
            var persistAppVolumes = settings.Get("PersistAppVolumes", false);
            if (!persistAppVolumes) return;
            // Restore the volume if it exists
            var key = DisplayName + "." + app.ExeName;
            if (settings.HasKey(key))
            {
                app.Volume = settings.Get(key, 1f).ToVolumeInt();
            }
        }
        
        private void ApplyDefaultAppVolume(AppItemViewModel app)
        {
            // We only want to apply the default app volume to new apps
            // Thus, the existing volume must be 100 ..
            if (app.Volume != 100) return;
            // .. and we mustn't have applied the default for this app before
            var appVolumeSettings = StorageFactory.GetSettings("DefaultAppVolumes");
            var key = app.AppId;
            if (appVolumeSettings.HasKey(key)) return;
            
            // Check if this functionality is active
            var generalSettings = StorageFactory.GetSettings();
            var defaultAppVolume = generalSettings.Get("DefaultAppVolume", 100);
            if (defaultAppVolume == 100) return;

            // Only now we apply the default volume
            app.Volume = defaultAppVolume;
            // Remember that we applied the default for this app
            appVolumeSettings.Set(key, true);
            
            // TODO: What's the expected behaviour regarding multiple devices and changing appIds?
            // AppIds should be different amongst different devices anyway, right? Then it should be fine as is.
            
            // TODO: The amount of keys in 'appVolumeSettings' will grow indefinitely. What's a good way around that?
        }

        public void AppMovingToThisDevice(TemporaryAppItemViewModel app)
        {
            app.Expired += OnAppExpired;

            foreach (var childApp in app.ChildApps)
            {
                ((IAudioDeviceManagerWindowsAudio)_deviceManager).UnhideSessionsForProcessId(_device.Id, childApp.ProcessId);
            }

            bool hasExistingAppGroup = false;
            foreach (var a in Apps)
            {
                if (a.DoesGroupWith(app))
                {
                    hasExistingAppGroup = true;
                    break;
                }
            }

            if (!hasExistingAppGroup)
            {
                Apps.AddSorted(app, AppItemViewModel.CompareByExeName);
            }
        }

        private void OnAppExpired(object sender, EventArgs e)
        {
            var app = (TemporaryAppItemViewModel)sender;
            if (Apps.Contains(app))
            {
                app.Expired -= OnAppExpired;
                Apps.Remove(app);
            }
        }

        internal void AppLeavingFromThisDevice(IAppItemViewModel app)
        {
            if (app is TemporaryAppItemViewModel)
            {
                Apps.Remove(app);
            }
        }

        public void MakeDefaultDevice() => _deviceManager.Default = _device;
        public void IncrementVolume(int delta) => Volume += delta;
        public override string ToString() => string.Format(IsMuted ? Resources.AppOrDeviceMutedFormatAccessibleText : Resources.AppOrDeviceFormatAccessibleText, DisplayName, Volume);
    }
}
