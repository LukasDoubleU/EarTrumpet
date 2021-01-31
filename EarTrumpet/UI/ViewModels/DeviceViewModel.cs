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
            Microphone
        }

        protected readonly IAudioDevice _device;
        protected readonly IAudioDeviceManager _deviceManager;
        protected readonly WeakReference<DeviceCollectionViewModel> _parent;
        private DeviceIconKind _iconKind;
        private bool _isDisplayNameVisible;

        public DeviceViewModel(DeviceCollectionViewModel parent, IAudioDeviceManager deviceManager,
            IAudioDevice device) : base(device)
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
                // Restore persisted volume if it exists
                var settings = StorageFactory.GetSettings("PersistVolume");
                var key = session.DisplayName + "." + app.ExeName;
                if (settings.HasKey(key)) app.Volume = settings.Get(key, 1f).ToVolumeInt();
                Apps.AddSorted(app, AppItemViewModel.CompareByExeName);
            }

            UpdateMasterVolumeIcon();
        }

        public string DisplayName => _device.DisplayName;

        public string AccessibleName => IsMuted
            ? Resources.AppOrDeviceMutedFormatAccessibleText.Replace("{Name}", DisplayName)
            : Resources.AppOrDeviceFormatAccessibleText.Replace("{Name}", DisplayName)
                .Replace("{Volume}", Volume.ToString());

        public string DeviceDescription => ((IAudioDeviceWindowsAudio) _device).DeviceDescription;
        public string EnumeratorName => ((IAudioDeviceWindowsAudio) _device).EnumeratorName;
        public string InterfaceName => ((IAudioDeviceWindowsAudio) _device).InterfaceName;
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

            foreach (var app in Apps) app.UpdatePeakValueForeground();
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
                    IconKind = DeviceIconKind.Mute;
                else if (_device.Volume >= 0.66f)
                    IconKind = DeviceIconKind.Bar3;
                else if (_device.Volume >= 0.33f)
                    IconKind = DeviceIconKind.Bar2;
                else
                    IconKind = DeviceIconKind.Bar1;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    AddSession((IAudioDeviceSession) e.NewItems[0]);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    var existing = Apps.FirstOrDefault(x => x.Id == ((IAudioDeviceSession) e.OldItems[0]).Id);
                    if (existing != null) Apps.Remove(existing);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void AddSession(IAudioDeviceSession session)
        {
            var newSession = new AppItemViewModel(this, session);

            foreach (var app in Apps)
                if (app.DoesGroupWith(newSession))
                {
                    // Remove the fake app entry after copying any changes the user did.
                    newSession.Volume = app.Volume;
                    newSession.IsMuted = app.IsMuted;
                    Apps.Remove(app);
                    break;
                }

            // Restore persisted volume if it exists
            var settings = StorageFactory.GetSettings("PersistVolume");
            var key = DisplayName + "." + newSession.ExeName;
            if (settings.HasKey(key)) newSession.Volume = settings.Get(key, 1f).ToVolumeInt();
            Apps.AddSorted(newSession, AppItemViewModel.CompareByExeName);
        }

        public void AppMovingToThisDevice(TemporaryAppItemViewModel app)
        {
            app.Expired += OnAppExpired;

            foreach (var childApp in app.ChildApps)
                ((IAudioDeviceManagerWindowsAudio) _deviceManager).UnhideSessionsForProcessId(_device.Id,
                    childApp.ProcessId);

            var hasExistingAppGroup = false;
            foreach (var a in Apps)
                if (a.DoesGroupWith(app))
                {
                    hasExistingAppGroup = true;
                    break;
                }

            if (!hasExistingAppGroup) Apps.AddSorted(app, AppItemViewModel.CompareByExeName);
        }

        private void OnAppExpired(object sender, EventArgs e)
        {
            var app = (TemporaryAppItemViewModel) sender;
            if (Apps.Contains(app))
            {
                app.Expired -= OnAppExpired;
                Apps.Remove(app);
            }
        }

        internal void AppLeavingFromThisDevice(IAppItemViewModel app)
        {
            if (app is TemporaryAppItemViewModel) Apps.Remove(app);
        }

        public void MakeDefaultDevice()
        {
            _deviceManager.Default = _device;
        }

        public void IncrementVolume(int delta)
        {
            Volume += delta;
        }

        public override string ToString()
        {
            return string.Format(
                IsMuted
                    ? Resources.AppOrDeviceMutedFormatAccessibleText
                    : Resources.AppOrDeviceFormatAccessibleText, DisplayName, Volume);
        }
    }
}