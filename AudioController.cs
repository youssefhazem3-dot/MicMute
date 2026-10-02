using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicMute;

/// <summary>Owns audio endpoints on a dedicated STA dispatcher; native callbacks only queue notifications.</summary>
public class AudioController : IMMNotificationClient, IDisposable
{
    private readonly AudioWorkQueue _workQueue;
    private MMDeviceEnumerator? _enumerator;
    private readonly AudioMuteState _muteState = new();
    private MMDevice? _currentDevice;
    private AudioEndpointVolumeNotificationDelegate? _volumeHandler;
    private string _targetDeviceId = string.Empty;
    private string _currentId = string.Empty;
    private string _currentName = "No Device";
    private volatile bool _isUsingFallback;
    private int _cachedMuteState = -1;
    private volatile bool _disposed;

    public event EventHandler? DevicesChanged;
    public event EventHandler<MuteStateChangedEventArgs>? MuteStateChanged;
    public event EventHandler<string>? WarningNotification;

    public AudioController()
    {
        _workQueue = new AudioWorkQueue();
        try
        {
            _workQueue.InvokeAsync(() =>
            {
                _enumerator = new MMDeviceEnumerator();
                try { _enumerator.RegisterEndpointNotificationCallback(this); }
                catch { _enumerator.Dispose(); _enumerator = null; throw; }
            }).GetAwaiter().GetResult();
        }
        catch { _workQueue.Dispose(); throw; }
    }

    public bool IsMuted
    {
        get => !_disposed && Volatile.Read(ref _cachedMuteState) == 1;
        set => Queue(() => SetMuteOnAudioThread(value));
    }

    public bool IsUsingFallback => _isUsingFallback;
    public string CurrentDeviceName => Volatile.Read(ref _currentName);
    public string CurrentDeviceId => Volatile.Read(ref _currentId);

    public Task<List<AudioDevice>> GetCaptureDevicesAsync()
    {
        if (_disposed) return Task.FromResult(new List<AudioDevice>());
        return _workQueue.InvokeAsync(() =>
        {
            var devices = new List<AudioDevice>();
            try
            {
                foreach (MMDevice device in _enumerator!.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    using (device) { devices.Add(new AudioDevice(device.ID, device.FriendlyName)); }
                }
            }
            catch (Exception ex)
            {
                WarningNotification?.Invoke(this, "Failed to list audio devices: " + ex.Message);
                throw;
            }
            return devices;
        });
    }

    public void SetTargetDevice(string deviceId)
    {
        Queue(() => { _targetDeviceId = deviceId ?? string.Empty; UpdateActiveDevice(); });
    }

    public Task SetTargetDeviceAsync(string deviceId) => _disposed ? Task.CompletedTask :
        _workQueue.InvokeAsync(() => { _targetDeviceId = deviceId ?? string.Empty; UpdateActiveDevice(); });

    public void ToggleMute()
    {
        Queue(() =>
        {
            if (_currentDevice == null) return;
            bool current;
            try { current = _currentDevice.AudioEndpointVolume.Mute; }
            catch { current = _muteState.Current ?? false; }
            SetMuteOnAudioThread(!current);
        });
    }

    public void ForceUpdateActiveDevice() => Queue(UpdateActiveDevice);

    private void SetMuteOnAudioThread(bool value)
    {
        if (_disposed || _currentDevice == null) return;
        try
        {
            _currentDevice.AudioEndpointVolume.Mute = value;
            if (_muteState.RecordLocalChange(value))
            {
                Volatile.Write(ref _cachedMuteState, value ? 1 : 0);
                MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(value, true));
            }
        }
        catch (Exception ex) { WarningNotification?.Invoke(this, "Failed to set mute state: " + ex.Message); }
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
                candidate = _enumerator!.GetDevice(_targetDeviceId);
                if (candidate.State != DeviceState.Active) { candidate.Dispose(); candidate = null; }
            }
            catch { candidate?.Dispose(); candidate = null; }
        }
        if (candidate == null)
        {
            try { candidate = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications); fallback = true; }
            catch { candidate = null; }
        }
        if (candidate == null)
        {
            try { candidate = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console); fallback = true; }
            catch { candidate = null; }
        }
        if (candidate == null)
        {
            try { candidate = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia); fallback = true; }
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
                    bool changed = _muteState.Current != muted || name != _currentName;
                    Volatile.Write(ref _currentName, name);
                    _isUsingFallback = fallback;
                    _muteState.RecordLocalChange(muted);
                    Volatile.Write(ref _cachedMuteState, muted ? 1 : 0);
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
            Volatile.Write(ref _currentId, candidate.ID);
            Volatile.Write(ref _currentName, candidate.FriendlyName);
            MMDevice expected = candidate;
            _volumeHandler = data => OnVolumeNotification(expected, data.Muted);
            candidate.AudioEndpointVolume.OnVolumeNotification += _volumeHandler;
            bool muted = candidate.AudioEndpointVolume.Mute;
            _muteState.RecordLocalChange(muted);
            Volatile.Write(ref _cachedMuteState, muted ? 1 : 0);
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
        Queue(() =>
        {
            if (!ReferenceEquals(expected, _currentDevice)) return;
            try
            {
                if (!_muteState.TryApplyNotification(muted, () => expected.AudioEndpointVolume.Mute, out bool current)) return;
                Volatile.Write(ref _cachedMuteState, current ? 1 : 0);
                MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(current, true));
            }
            catch (Exception ex) { WarningNotification?.Invoke(this, "Could not read microphone state: " + ex.Message); }
        });
    }

    private void Queue(Action action)
    {
        if (_disposed) return;
        Task queued = _workQueue.InvokeAsync(() => { if (!_disposed) action(); });
        _ = queued.ContinueWith(task =>
            System.Diagnostics.Trace.WriteLine("Audio operation failed: " + task.Exception?.GetBaseException()),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private void DetachCurrentDevice()
    {
        MMDevice? previous = _currentDevice;
        AudioEndpointVolumeNotificationDelegate? handler = _volumeHandler;
        _currentDevice = null;
        _volumeHandler = null;
        Volatile.Write(ref _currentId, string.Empty);
        Volatile.Write(ref _currentName, "No Device");
        Volatile.Write(ref _cachedMuteState, -1);
        _muteState.Reset();
        if (previous == null) return;
        try { if (handler != null) previous.AudioEndpointVolume.OnVolumeNotification -= handler; } catch { }
        try { previous.Dispose(); } catch { }
    }

    private void NotifyDevicesChanged()
    {
        Queue(() => DevicesChanged?.Invoke(this, EventArgs.Empty));
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => NotifyDevicesChanged();
    public void OnDeviceAdded(string deviceId) => NotifyDevicesChanged();
    public void OnDeviceRemoved(string deviceId) => NotifyDevicesChanged();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Capture && (role == Role.Communications || role == Role.Console || role == Role.Multimedia))
            NotifyDevicesChanged();
    }
    public void OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
        if (deviceId == CurrentDeviceId) NotifyDevicesChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Task cleanup = _workQueue.InvokeAsync(() =>
        {
            try { _enumerator?.UnregisterEndpointNotificationCallback(this); } catch { }
            DetachCurrentDevice();
            try { _enumerator?.Dispose(); } catch { }
            _enumerator = null;
        });
        bool finished;
        try { finished = cleanup.Wait(TimeSpan.FromMilliseconds(100)); }
        catch (AggregateException ex)
        {
            System.Diagnostics.Trace.WriteLine("Audio cleanup failed: " + ex.GetBaseException());
            finished = true;
        }
        if (finished) _workQueue.Dispose();
        else _ = cleanup.ContinueWith(task =>
        {
            if (task.IsFaulted) System.Diagnostics.Trace.WriteLine("Audio cleanup failed: " + task.Exception?.GetBaseException());
            _workQueue.Dispose();
        }, TaskScheduler.Default);
    }
}
