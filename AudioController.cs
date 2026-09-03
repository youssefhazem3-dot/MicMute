using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicMute;

/// <summary>Owns audio endpoints on the UI dispatcher; native callbacks only queue notifications.</summary>
public class AudioController : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly Dispatcher _dispatcher;
    private MMDevice? _currentDevice;
    private AudioEndpointVolumeNotificationDelegate? _volumeHandler;
    private string _targetDeviceId = string.Empty;
    private string _currentId = string.Empty;
    private string _currentName = "No Device";
    private bool _isUsingFallback;
    private bool? _lastReportedMuteState;
    private volatile bool _disposed;

    public event EventHandler? DevicesChanged;
    public event EventHandler<MuteStateChangedEventArgs>? MuteStateChanged;
    public event EventHandler<string>? WarningNotification;

    public AudioController()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _enumerator = new MMDeviceEnumerator();
        try { _enumerator.RegisterEndpointNotificationCallback(this); }
        catch { _enumerator.Dispose(); throw; }
    }

    public bool IsMuted
    {
        get
        {
            if (_disposed || _currentDevice == null) return false;
            if (!_dispatcher.CheckAccess()) return _lastReportedMuteState ?? false;
            try { return _currentDevice.AudioEndpointVolume.Mute; }
            catch { return _lastReportedMuteState ?? false; }
        }
        set
        {
            if (!_dispatcher.CheckAccess()) { Dispatch(() => IsMuted = value); return; }
            if (_disposed || _currentDevice == null) return;
            try { _currentDevice.AudioEndpointVolume.Mute = value; }
            catch (Exception ex) { WarningNotification?.Invoke(this, "Failed to set mute state: " + ex.Message); }
        }
    }

    public bool IsUsingFallback => _isUsingFallback;
    public string CurrentDeviceName => _currentName;
    public string CurrentDeviceId => _currentId;

    public List<AudioDevice> GetCaptureDevices()
    {
        var devices = new List<AudioDevice>();
        if (_disposed) return devices;
        try
        {
            foreach (MMDevice device in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                using (device) { devices.Add(new AudioDevice(device.ID, device.FriendlyName)); }
            }
        }
        catch (Exception ex) { WarningNotification?.Invoke(this, "Failed to list audio devices: " + ex.Message); }
        return devices;
    }

    public void SetTargetDevice(string deviceId)
    {
        if (!_dispatcher.CheckAccess()) { Dispatch(() => SetTargetDevice(deviceId)); return; }
        if (_disposed) return;
        _targetDeviceId = deviceId ?? string.Empty;
        UpdateActiveDevice();
    }

    public void ToggleMute()
    {
        if (!_dispatcher.CheckAccess()) { Dispatch(ToggleMute); return; }
        if (!_disposed) IsMuted = !IsMuted;
    }

    public void ForceUpdateActiveDevice()
    {
        if (!_dispatcher.CheckAccess()) { Dispatch(ForceUpdateActiveDevice); return; }
        UpdateActiveDevice();
    }

    private void UpdateActiveDevice()
    {
        if (_disposed) return;
        MMDevice? candidate = null;
        bool fallback = false;
        if (!string.IsNullOrEmpty(_targetDeviceId))
        {
            try
            {
                candidate = _enumerator.GetDevice(_targetDeviceId);
                if (candidate.State != DeviceState.Active) { candidate.Dispose(); candidate = null; }
            }
            catch { candidate?.Dispose(); candidate = null; }
        }
        if (candidate == null)
        {
            try { candidate = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications); fallback = true; }
            catch { candidate = null; }
        }

        // Do not activate a second endpoint-volume listener merely to inspect the candidate.
        if (candidate != null && _currentDevice != null && _volumeHandler != null)
        {
            try
            {
                if (_currentDevice.State == DeviceState.Active && candidate.ID == _currentId)
                {
                    bool muted = _currentDevice.AudioEndpointVolume.Mute; // also detects invalidated audio-service objects
                    string name = candidate.FriendlyName;
                    bool changed = _lastReportedMuteState != muted || name != _currentName;
                    _currentName = name;
                    _isUsingFallback = fallback;
                    _lastReportedMuteState = muted;
                    candidate.Dispose();
                    if (changed) MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(muted, false));
                    return;
                }
            }
            catch { /* Rebind an invalidated current endpoint. */ }
        }

        DetachCurrentDevice();
        _currentDevice = candidate;
        _isUsingFallback = fallback && candidate != null;
        if (candidate == null)
        {
            WarningNotification?.Invoke(this, "No active audio capture devices found.");
            MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(false, false));
            return;
        }
        try
        {
            _currentId = candidate.ID;
            _currentName = candidate.FriendlyName;
            MMDevice expected = candidate;
            _volumeHandler = data => OnVolumeNotification(expected, data.Muted);
            candidate.AudioEndpointVolume.OnVolumeNotification += _volumeHandler;
            bool muted = candidate.AudioEndpointVolume.Mute;
            _lastReportedMuteState = muted;
            MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(muted, false));
        }
        catch (Exception ex)
        {
            DetachCurrentDevice();
            WarningNotification?.Invoke(this, "Failed to initialize microphone: " + ex.Message);
            MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(false, false));
        }
    }

    private void OnVolumeNotification(MMDevice expected, bool muted)
    {
        // Never read COM or wait for the UI from this callback: unregister/dispose may wait for it.
        Dispatch(() =>
        {
            if (!ReferenceEquals(expected, _currentDevice)) return;
            try
            {
                if (expected.AudioEndpointVolume.Mute != muted || _lastReportedMuteState == muted) return;
                _lastReportedMuteState = muted;
                MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(muted, true));
            }
            catch (Exception ex) { WarningNotification?.Invoke(this, "Could not read microphone state: " + ex.Message); }
        });
    }

    private void Dispatch(Action action)
    {
        if (_disposed || _dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished) return;
        try { _dispatcher.BeginInvoke(new Action(() => { if (!_disposed) action(); })); }
        catch (InvalidOperationException) { }
    }

    private void DetachCurrentDevice()
    {
        MMDevice? previous = _currentDevice;
        AudioEndpointVolumeNotificationDelegate? handler = _volumeHandler;
        _currentDevice = null;
        _volumeHandler = null;
        _currentId = string.Empty;
        _currentName = "No Device";
        _lastReportedMuteState = null;
        if (previous == null) return;
        try { if (handler != null) previous.AudioEndpointVolume.OnVolumeNotification -= handler; } catch { }
        try { previous.Dispose(); } catch { }
    }

    private void NotifyDevicesChanged()
    {
        if (!_disposed) Dispatch(() => DevicesChanged?.Invoke(this, EventArgs.Empty));
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => NotifyDevicesChanged();
    public void OnDeviceAdded(string deviceId) => NotifyDevicesChanged();
    public void OnDeviceRemoved(string deviceId) => NotifyDevicesChanged();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Capture && role == Role.Communications) NotifyDevicesChanged();
    }
    public void OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
        if (deviceId == _currentId) NotifyDevicesChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
        DetachCurrentDevice();
        try { _enumerator.Dispose(); } catch { }
    }
}
