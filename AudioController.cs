using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicMute;

public class AudioController : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    private MMDevice? _currentDevice;
    private string _targetDeviceId = string.Empty;
    private bool _isUsingFallback;
    private readonly object _lock = new object();
    private readonly object _updateLock = new object();

    public bool IsMuted
    {
        get
        {
            MMDevice? currentDevice;
            lock (_lock)
            {
                currentDevice = _currentDevice;
            }
            if (currentDevice == null)
            {
                return false;
            }
            try
            {
                return currentDevice.AudioEndpointVolume?.Mute ?? false;
            }
            catch
            {
                return false;
            }
        }
        set
        {
            MMDevice? currentDevice;
            lock (_lock)
            {
                currentDevice = _currentDevice;
            }
            if (currentDevice == null)
            {
                return;
            }
            try
            {
                if (currentDevice.AudioEndpointVolume != null)
                {
                    currentDevice.AudioEndpointVolume.Mute = value;
                }
            }
            catch (Exception ex)
            {
                WarningNotification?.Invoke(this, "Failed to set mute state: " + ex.Message);
            }
        }
    }

    public bool IsUsingFallback
    {
        get
        {
            lock (_lock)
            {
                return _isUsingFallback;
            }
        }
    }

    public string CurrentDeviceName
    {
        get
        {
            MMDevice? currentDevice;
            lock (_lock)
            {
                currentDevice = _currentDevice;
            }
            try
            {
                return currentDevice?.FriendlyName ?? "No Device";
            }
            catch
            {
                return "No Device";
            }
        }
    }

    public string CurrentDeviceId
    {
        get
        {
            MMDevice? currentDevice;
            lock (_lock)
            {
                currentDevice = _currentDevice;
            }
            try
            {
                return currentDevice?.ID ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public event EventHandler? DevicesChanged;
    public event EventHandler<MuteStateChangedEventArgs>? MuteStateChanged;
    public event EventHandler<string>? WarningNotification;

    public AudioController()
    {
        _enumerator = new MMDeviceEnumerator();
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    public List<AudioDevice> GetCaptureDevices()
    {
        List<AudioDevice> list = new List<AudioDevice>();
        try
        {
            foreach (MMDevice item in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                list.Add(new AudioDevice(item.ID, item.FriendlyName));
            }
        }
        catch (Exception ex)
        {
            WarningNotification?.Invoke(this, "Failed to list audio devices: " + ex.Message);
        }
        return list;
    }

    public void SetTargetDevice(string deviceId)
    {
        _targetDeviceId = deviceId;
        UpdateActiveDevice();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    private void UpdateActiveDevice()
    {
        Application? current = Application.Current;
        Dispatcher? dispatcher = current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(new Action(UpdateActiveDevice));
            return;
        }
        lock (_updateLock)
        {
            string targetDeviceId;
            lock (_lock)
            {
                targetDeviceId = _targetDeviceId;
            }
            MMDevice? mMDevice = null;
            bool isUsingFallback = false;
            if (!string.IsNullOrEmpty(targetDeviceId))
            {
                try
                {
                    MMDevice device = _enumerator.GetDevice(targetDeviceId);
                    if (device.State == DeviceState.Active)
                    {
                        mMDevice = device;
                    }
                    else
                    {
                        device.Dispose();
                    }
                }
                catch
                {
                    mMDevice = null;
                }
            }
            if (mMDevice == null)
            {
                try
                {
                    mMDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                    if (mMDevice != null)
                    {
                        isUsingFallback = true;
                    }
                }
                catch
                {
                    mMDevice = null;
                }
            }
            MMDevice? mMDevice2 = null;
            lock (_lock)
            {
                mMDevice2 = _currentDevice;
                _currentDevice = mMDevice;
                _isUsingFallback = isUsingFallback;
            }
            if (mMDevice2 != null)
            {
                try
                {
                    mMDevice2.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
                }
                catch
                {
                }
                try
                {
                    mMDevice2.Dispose();
                }
                catch
                {
                }
            }
            if (mMDevice != null)
            {
                try
                {
                    mMDevice.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
                    MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(mMDevice.AudioEndpointVolume.Mute, showOsd: false));
                    return;
                }
                catch (Exception ex)
                {
                    WarningNotification?.Invoke(this, "Failed to initialize volume listener: " + ex.Message);
                    return;
                }
            }
            WarningNotification?.Invoke(this, "No active audio capture devices found.");
            MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(isMuted: false, showOsd: false));
        }
    }

    public void ForceUpdateActiveDevice()
    {
        UpdateActiveDevice();
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        try
        {
            lock (_lock)
            {
                if (_currentDevice == null)
                {
                    return;
                }
            }
            MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(data.Muted, showOsd: true));
        }
        catch
        {
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        try
        {
            DevicesChanged?.Invoke(this, EventArgs.Empty);
            bool flag = false;
            lock (_lock)
            {
                flag = deviceId == _targetDeviceId || (_isUsingFallback && deviceId == _currentDevice?.ID);
            }
            if (flag)
            {
                UpdateActiveDevice();
            }
        }
        catch
        {
        }
    }

    public void OnDeviceAdded(string deviceId)
    {
        try
        {
            DevicesChanged?.Invoke(this, EventArgs.Empty);
            bool flag = false;
            lock (_lock)
            {
                flag = deviceId == _targetDeviceId;
            }
            if (flag)
            {
                UpdateActiveDevice();
            }
        }
        catch
        {
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        try
        {
            DevicesChanged?.Invoke(this, EventArgs.Empty);
            bool flag = false;
            lock (_lock)
            {
                flag = deviceId == _targetDeviceId || deviceId == _currentDevice?.ID;
            }
            if (flag)
            {
                UpdateActiveDevice();
            }
        }
        catch
        {
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        try
        {
            if (flow == DataFlow.Capture && role == Role.Communications)
            {
                DevicesChanged?.Invoke(this, EventArgs.Empty);
                bool flag = false;
                lock (_lock)
                {
                    flag = _isUsingFallback || string.IsNullOrEmpty(_targetDeviceId);
                }
                if (flag)
                {
                    UpdateActiveDevice();
                }
            }
        }
        catch
        {
        }
    }

    public void OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
    }

    public void Dispose()
    {
        lock (_lock)
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch
            {
            }
            if (_currentDevice != null)
            {
                try
                {
                    _currentDevice.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
                }
                catch
                {
                }
                try
                {
                    _currentDevice.Dispose();
                }
                catch
                {
                }
                _currentDevice = null;
            }
            try
            {
                _enumerator.Dispose();
            }
            catch
            {
            }
        }
    }
}