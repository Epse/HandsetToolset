using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using ABI.Windows.Web.Http;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HandsetToolset
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            ResultCollection = new ObservableCollection<RfcommDeviceDisplay>();
        }

        private DeviceWatcher? deviceWatcher = null;
        private BluetoothDevice? bluetoothDevice;

        // Used to display list of available devices to chat with
        public ObservableCollection<RfcommDeviceDisplay> ResultCollection { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ResultCollection = new ObservableCollection<RfcommDeviceDisplay>();
            DataContext = this;

            if (App.Connection != null)
            {
                AfterConnected();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (App.Connection == null) return;
            App.Connection.Notify -= NotifyHandler;
            App.Connection.TextReceived -= TextReceived;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher == null)
            {
                SetDeviceWatcherUI();
                StartUnpairedDeviceWatcher();
            }
            else
            {
                ResetMainUI();
            }
        }

        private void StopWatcher()
        {
            if (null == deviceWatcher) return;
            if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                 DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
            {
                deviceWatcher.Stop();
            }

            deviceWatcher = null;
        }

        private void SetDeviceWatcherUI()
        {
            // Disable the button while we do async operations so the user can't Run twice.
            RunButton.Content = "Stop";
            NotifyUser("Device watcher started", InfoBarSeverity.Informational);
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;
        }

        private void ResetMainUI()
        {
            RunButton.Content = "Start";
            RunButton.IsEnabled = true;
            ConnectButton.Visibility = Visibility.Visible;
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;

            // Re-set device specific UX
            ChatBox.Visibility = Visibility.Collapsed;
            RequestAccessButton.Visibility = Visibility.Collapsed;
            OutputBox.Text = "";
            StopWatcher();
        }

        private void StartUnpairedDeviceWatcher()
        {
            // Request additional properties
            string[] requestedProperties = new string[]
                { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher(
                "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                requestedProperties,
                DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(
                (watcher, deviceInfo) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        // Make sure device name isn't blank
                        if (deviceInfo.Name != "")
                        {
                            ResultCollection.Add(new RfcommDeviceDisplay(deviceInfo));
                            NotifyUser(
                                $"{ResultCollection.Count} devices found.",
                                InfoBarSeverity.Informational);
                        }
                    });
                });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(
                (watcher, deviceInfoUpdate) =>
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                        {
                            if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                            {
                                rfcommInfoDisp.Update(deviceInfoUpdate);
                                break;
                            }
                        }
                    });
                });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    NotifyUser(
                        $"{ResultCollection.Count} devices found. Enumeration completed. Watching for updates...",
                        InfoBarSeverity.Informational);
                });
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(
                (watcher, deviceInfoUpdate) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        // Find the corresponding DeviceInformation in the collection and remove it
                        foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                        {
                            if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                            {
                                ResultCollection.Remove(rfcommInfoDisp);
                                break;
                            }
                        }

                        NotifyUser(
                            String.Format("{0} devices found.", ResultCollection.Count),
                            InfoBarSeverity.Informational);
                    });
                });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>((watcher, obj) =>
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => { ResultCollection.Clear(); });
            });

            deviceWatcher.Start();
        }

        private void SetChatUI(string serviceName, string deviceName)
        {
            //NotifyUser("Connected", InfoBarSeverity.Informational);
            DeviceName.Text = "Connected to: " + deviceName;
            RunButton.IsEnabled = false;
            ConnectButton.Visibility = Visibility.Collapsed;
            RequestAccessButton.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = false;
            resultsListView.Visibility = Visibility.Collapsed;
            ChatBox.Visibility = Visibility.Visible;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure user has selected a device first
            if (resultsListView.SelectedItem != null)
            {
                NotifyUser("Connecting to remote device. Please wait...", InfoBarSeverity.Informational);
            }
            else
            {
                NotifyUser("Please select an item to connect to", InfoBarSeverity.Error);
                return;
            }

            RfcommDeviceDisplay deviceInfoDisp = (resultsListView.SelectedItem as RfcommDeviceDisplay)!;

            var connection = await BluetoothConnection.Make(deviceInfoDisp);

            if (connection.Failure)
            {
                NotifyUser(connection.ToString(), InfoBarSeverity.Error);
                ResetMainUI();
                return;
            }

            bluetoothDevice = connection.Data.Device;

            //SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);
            SetChatUI("Placeholder", bluetoothDevice.Name);

            App.Connection = connection.Data;
            App.Connection.Run();
            AfterConnected();
        }

        private void AfterConnected()
        {
            if (App.Connection == null) return;

            if (ResultCollection.Count == 0)
            {
                ResultCollection.Add(App.Connection.Display);
                resultsListView.SelectedIndex = 0;
            }

            App.Connection.Notify += NotifyHandler;
            App.Connection.TextReceived += TextReceived;

            SetChatUI("RFCOMM", App.Connection.Device.Name);
        }

        private void TextReceived(BluetoothConnection sender, string text)
        {
            OutputBox.Text = text;
        }

        private void NotifyHandler(BluetoothConnection sender, string message, InfoBarSeverity severity)
        {
            NotifyUser(message, severity, "Bluetooth");
        }

        /// <summary>
        ///  If you believe the Bluetooth device will eventually be paired with Windows,
        ///  you might want to pre-emptively get consent to access the device.
        ///  An explicit call to RequestAccessAsync() prompts the user for consent.
        ///  If this is not done, a device that's working before being paired,
        ///  will no longer work after being paired.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RequestAccessButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure user has given consent to access device
            DeviceAccessStatus accessStatus = await bluetoothDevice.RequestAccessAsync();

            if (accessStatus != DeviceAccessStatus.Allowed)
            {
                NotifyUser(
                    "Access to the device is denied because the application was not granted access",
                    InfoBarSeverity.Informational);
            }
            else
            {
                NotifyUser(
                    "Access granted, you are free to pair devices",
                    InfoBarSeverity.Informational);
            }
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePairingButtons();
        }

        private void UpdatePairingButtons()
        {
            RfcommDeviceDisplay deviceDisp = (RfcommDeviceDisplay)resultsListView.SelectedItem;

            if (null != deviceDisp)
            {
                ConnectButton.IsEnabled = true;
            }
            else
            {
                ConnectButton.IsEnabled = false;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect("Disconnected");
        }


        /// <summary>
        /// Cleans up the socket and DataWriter and reset the UI
        /// </summary>
        /// <param name="disconnectReason"></param>
        private void Disconnect(string disconnectReason)
        {
            App.Connection?.Disconnect(disconnectReason);

            ResetMainUI();
        }

        private void NotifyUser(string message, InfoBarSeverity severity, string? title = null)
        {
            Bar.Title = title ?? string.Empty;
            Bar.Message = message;
            Bar.Severity = severity;
            Bar.IsOpen = true;
        }
    }
}