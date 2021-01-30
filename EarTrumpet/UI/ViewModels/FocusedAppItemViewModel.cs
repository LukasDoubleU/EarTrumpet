using EarTrumpet.DataModel.Storage;
using EarTrumpet.Extensibility.Hosting;
using EarTrumpet.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace EarTrumpet.UI.ViewModels
{
    public class FocusedAppItemViewModel : IFocusedViewModel
    {
        public event Action RequestClose;

        public IAppItemViewModel App { get; }
        public ObservableCollection<ToolbarItemViewModel> Toolbar { get; }
        public string DisplayName => App.DisplayName;
        public ObservableCollection<object> Addons { get; }

        public FocusedAppItemViewModel(DeviceCollectionViewModel parent, IAppItemViewModel app)
        {
            App = app;

            Toolbar = new ObservableCollection<ToolbarItemViewModel>();

            // Option to Hide an App
            Toolbar.Add(new ToolbarItemViewModel
            {
                GlyphFontSize = 10,
                DisplayName = "Hide", // Properties.Resources.CloseButtonAccessibleText,
                Glyph = "\uE107",
                Command = new RelayCommand(() =>
                    {
                        // Add To HiddenApps in Settings
                        const string Key = "HIDDEN_APPS";
                        List<string> hiddenApps = StorageFactory.GetSettings().Get(Key, new List<string>());
                        hiddenApps.Add(App.ExeName);
                        StorageFactory.GetSettings().Set(Key, hiddenApps);

                        // Remove in Memory from Device-App-Collections
                        foreach(DeviceViewModel device in parent.AllDevices)
                        {
                            var appsToRemove = device.Apps.Where(_app => app.ExeName.Equals(_app.ExeName)).ToList();
                            foreach(var _app in appsToRemove)
                            {
                                device.Apps.Remove(_app);
                            }
                        }
                        RequestClose.Invoke();
                    })
            });

            Toolbar.Add(new ToolbarItemViewModel
            {
                GlyphFontSize = 10,
                DisplayName = Properties.Resources.CloseButtonAccessibleText,
                Glyph = "\uE8BB",
                Command = new RelayCommand(() => RequestClose.Invoke())
            });

            if (app.IsMovable)
            {
                var persistedDeviceId = app.PersistedOutputDevice;

                var items = parent.AllDevices.Select(dev => new ContextMenuItem
                {
                    DisplayName = dev.DisplayName,
                    Command = new RelayCommand(() =>
                    {
                        parent.MoveAppToDevice(app, dev);
                        RequestClose.Invoke();
                    }),
                    IsChecked = (dev.Id == persistedDeviceId),
                }).ToList();

                items.Insert(0, new ContextMenuItem
                {
                    DisplayName = Properties.Resources.DefaultDeviceText,
                    IsChecked = (string.IsNullOrWhiteSpace(persistedDeviceId)),
                    Command = new RelayCommand(() =>
                    {
                        parent.MoveAppToDevice(app, null);
                        RequestClose.Invoke();
                    }),
                });
                items.Insert(1, new ContextMenuSeparator());
                Toolbar.Insert(0, new ToolbarItemViewModel
                {
                    GlyphFontSize = 16,
                    DisplayName = Properties.Resources.MoveButtonAccessibleText,
                    Glyph = "\uE8AB",
                    Menu = new ObservableCollection<ContextMenuItem>(items)
                });
            }

            var contentItems = AddonManager.Host.AppContentItems;
            if (contentItems != null)
            {
                Addons = new ObservableCollection<object>(contentItems.Select(a => a.GetContentForApp(App.Parent.Id, App.Id, () => RequestClose.Invoke())).ToArray());

                var menuItems = contentItems.SelectMany(a => a.GetItemsForApp(app.Parent.Id, app.AppId));
                if (menuItems.Any())
                {
                    Toolbar.Insert(0, new ToolbarItemViewModel
                    {
                        GlyphFontSize = 16,
                        DisplayName = Properties.Resources.MoreCommandsAccessibleText,
                        Glyph = "\uE10C",
                        Menu = new ObservableCollection<ContextMenuItem>(menuItems)
                    });
                }
            }
        }

        public void Closing()
        {

        }
    }
}
