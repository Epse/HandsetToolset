using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Controls;

namespace HandsetToolset
{
    public class BluetoothConnection
    {
        private readonly CommandProcessor _processor = CommandProcessor.Current;

        public delegate void TextReceivedHandler(BluetoothConnection sender, string text);
        public event TextReceivedHandler? TextReceived;

        public delegate void NotifyHandler(BluetoothConnection sender, string message, InfoBarSeverity severity);
        public event NotifyHandler? Notify;

        private string _buf = "";

        private StreamSocket? _socket;
        private RfcommDeviceService? _service;
        private readonly DataReader _reader;
        public readonly BluetoothDevice Device;
        public readonly RfcommDeviceDisplay Display;

        private BluetoothConnection(StreamSocket socket, RfcommDeviceService service, BluetoothDevice device, RfcommDeviceDisplay display)
        {
            this._socket = socket;
            this._service = service;
            this.Device = device;
            _reader = new DataReader(socket.InputStream);
            Device = device;
            Display = display;
        }

        public static async Task<Result<BluetoothConnection>> Make(RfcommDeviceDisplay device)
        {
            // Perform device access checks before trying to get the device.
            // First, we check if consent has been explicitly denied by the user.
            DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(device.Id).CurrentStatus;
            if (accessStatus == DeviceAccessStatus.DeniedByUser)
            {
                return new ErrorResult<BluetoothConnection>(
                    "This app does not have access to connect to the remote device (please grant access in Settings > Privacy > Other Devices");
            }

            BluetoothDevice bluetoothDevice;

            // If not, try to get the Bluetooth device
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(device.Id);
            }
            catch (Exception ex)
            {
                return new ErrorResult<BluetoothConnection> (ex.Message);
            }

            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.
            if (bluetoothDevice == null)
            {
                return new ErrorResult<BluetoothConnection>("Bluetooth Device returned null. Access Status = " + accessStatus.ToString());
            }

            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.SerialPort, BluetoothCacheMode.Uncached);
            RfcommDeviceService service;

            if (rfcommServices.Services.Count > 0)
            {
                service = rfcommServices.Services[0];
            }
            else
            {
                return new ErrorResult<BluetoothConnection>(
                    "Could not discover the chat service on the remote device");
            }

            StreamSocket socket = new();

            try
            {
                await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName);
                return new SuccessResult<BluetoothConnection>(new BluetoothConnection(socket, service, bluetoothDevice, device));
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                return new ErrorResult<BluetoothConnection>("The device does not support RFCOMM SPP protocol.");
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                return new ErrorResult<BluetoothConnection>("Please verify that there is no other RFCOMM Connection to the same device.");
            }
        }

        public void Run()
        {
            ReceiveStringLoop();
        }

        private async void ReceiveStringLoop()
        {
            try
            {
                // Read one byte at a time
                uint size = await _reader.LoadAsync(1);
                if (size < 1)
                {
                    Disconnect(
                        "Remote device terminated Connection - make sure only one instance of server is running on remote device");
                    return;
                }

                _buf += _reader.ReadString(1);
                _buf = _processor.Process(_buf);
                TextReceived?.Invoke(this, _buf);


                ReceiveStringLoop();
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (_socket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        if ((uint)ex.HResult == 0x80072745)
                        {
                            Notify?.Invoke(this, "Disconnect triggered by remote device", InfoBarSeverity.Informational);
                        }
                        else if ((uint)ex.HResult == 0x800703E3)
                        {
                            Notify?.Invoke(this, "The I/O operation has been aborted because of either a thread exit or an application request.", InfoBarSeverity.Informational);
                        }
                    }
                    else
                    {
                        Disconnect("Read stream failed with error: " + ex.Message);
                    }
                }
            }
        }
        public void Disconnect(string disconnectReason)
        {
            if (_service != null)
            {
                _service.Dispose();
                _service = null;
            }

            lock (this)
            {
                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket = null;
                }
            }

            Notify?.Invoke(this, disconnectReason, InfoBarSeverity.Informational);
        }
    }
}
