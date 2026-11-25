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
    private volatile bool _syncAllMicrophones;
    private volatile bool _lockMuteState;
    private bool? _lockedTargetMute;
    private int _cachedMuteState = -1;
    private int _disposed;

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

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

    public bool IsMuted => !IsDisposed && Volatile.Read(ref _cachedMuteState) == 1;

    public void SetMute(bool value, bool showOsd = true) => Queue(() => SetMuteOnAudioThread(value, showOsd));

    public bool SyncAllMicrophones
    {
        get => _syncAllMicrophones;
        set => _syncAllMicrophones = value;
    }
    public bool LockMuteState
    {
        get => _lockMuteState;
        set
        {
            _lockMuteState = value;
            if (value)
            {
                Queue(() =>
                {
                    if (_currentDevice != null)
                    {
                        try
                        {
                            bool current = _muteState.Current ?? _currentDevice.AudioEndpointVolume.Mute;
                            _lockedTargetMute = current;
                            _muteState.RecordLocalChange(current);
                            Volatile.Write(ref _cachedMuteState, current ? 1 : 0);
                        }
                        catch
                        {
                        }
                    }
                });
            }
        }
    }
    public string CurrentDeviceName => Volatile.Read(ref _currentName);
    public string CurrentDeviceId => Volatile.Read(ref _currentId);

    private static readonly Role[] RenderLookupRoles = { Role.Multimedia, Role.Console, Role.Communications };

    public virtual int GetDefaultOutputVolumePercent(int fallback = UiBehavior.DefaultSoundVolume)
    {
        int safeFallback = Math.Clamp(fallback, UiBehavior.MinimumSoundVolume, UiBehavior.MaximumSoundVolume);
        if (IsDisposed) return safeFallback;
        try
        {
            return _workQueue.InvokeAsync(() =>
            {
                if (IsDisposed || _enumerator == null) return safeFallback;
                return TryReadDefaultOutputVolumePercent(_enumerator) ?? safeFallback;
            }).GetAwaiter().GetResult();
        }
        catch
        {
            return safeFallback;
        }
    }

    private static int? TryReadDefaultOutputVolumePercent(MMDeviceEnumerator enumerator)
    {
        foreach (Role role in RenderLookupRoles)
        {
            try
            {
                using MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
                float scalar = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                if (float.IsFinite(scalar))
                {
                    return Math.Clamp((int)Math.Round(scalar * 100.0f), UiBehavior.MinimumSoundVolume, UiBehavior.MaximumSoundVolume);
                }
            }
            catch
            {
            }
        }
        return null;
    }

    public Task<List<AudioDevice>> GetCaptureDevicesAsync()
    {
        if (IsDisposed) return Task.FromResult(new List<AudioDevice>());
        return _workQueue.InvokeAsync(() =>
        {
            var devices = new List<AudioDevice>();
            if (IsDisposed || _enumerator == null) return devices;
            try
            {
                foreach (MMDevice device in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
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

    public Task SetTargetDeviceAsync(string deviceId) => IsDisposed ? Task.CompletedTask :
        _workQueue.InvokeAsync(() => { _targetDeviceId = deviceId ?? string.Empty; UpdateActiveDevice(); });

    public void ToggleMute()
    {
        Queue(() =>
        {
            if (_currentDevice == null)
            {
                UpdateActiveDevice();
                if (_currentDevice == null)
                {
                    DiagnosticLogger.LogWarning("ToggleMute called but no active capture device found.");
                    WarningNotification?.Invoke(this, "No active audio capture devices found.");
                    return;
                }
            }
            bool current;
            try { current = _currentDevice.AudioEndpointVolume.Mute; }
            catch (Exception ex1)
            {
                DiagnosticLogger.LogWarning("ToggleMute initial read failed: " + ex1.Message);
                UpdateActiveDevice();
                if (_currentDevice == null)
                {
                    DiagnosticLogger.LogWarning("ToggleMute retry failed: no active device.");
                    WarningNotification?.Invoke(this, "No active audio capture devices found.");
                    return;
                }
                try { current = _currentDevice.AudioEndpointVolume.Mute; }
                catch (Exception ex2)
                {
                    DiagnosticLogger.LogWarning("ToggleMute retry read failed: " + ex2.Message);
                    current = _muteState.Current ?? false; 
                }
            }
            SetMuteOnAudioThread(!current, showOsd: true);
        });
    }

    public void ForceUpdateActiveDevice() => Queue(UpdateActiveDevice);

    private void SetMuteOnAudioThread(bool value, bool showOsd = true)
    {
        if (IsDisposed) return;
        _lockedTargetMute = value;
        if (_currentDevice == null)
        {
            UpdateActiveDevice();
            if (_currentDevice == null) return;
        }
        try
        {
            _currentDevice.AudioEndpointVolume.Mute = value;
            if (_syncAllMicrophones) ApplySyncToAllEndpoints(value, _currentDevice.ID);
            if (_muteState.RecordLocalChange(value))
            {
                Volatile.Write(ref _cachedMuteState, value ? 1 : 0);
                MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(value, showOsd));
            }
        }
        catch (Exception ex)
        {
            try
            {
                UpdateActiveDevice();
                if (_currentDevice != null)
                {
                    _currentDevice.AudioEndpointVolume.Mute = value;
                    if (_syncAllMicrophones) ApplySyncToAllEndpoints(value, _currentDevice.ID);
                    if (_muteState.RecordLocalChange(value))
                    {
                        Volatile.Write(ref _cachedMuteState, value ? 1 : 0);
                        MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(value, showOsd));
                    }
                    return;
                }
            }
            catch (Exception retryEx) { DiagnosticLogger.LogError("Retry SetMuteOnAudioThread failed", retryEx); }
            WarningNotification?.Invoke(this, "Failed to set mute state: " + ex.Message);
        }
    }

    private void ApplySyncToAllEndpoints(bool value, string excludeDeviceId)
    {
        if (IsDisposed || _enumerator == null) return;
        try
        {
            foreach (MMDevice device in _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                using (device)
                {
                    if (string.Equals(device.ID, excludeDeviceId, StringComparison.OrdinalIgnoreCase)) continue;
                    try { device.AudioEndpointVolume.Mute = value; } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogWarning("SyncAllMicrophones enumeration failed: " + ex.Message);
        }
    }

    private static MMDevice? TryFindFallbackEndpoint(MMDeviceEnumerator enumerator)
    {
        try { return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications); } catch { }
        try { return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console); } catch { }
        try { return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia); } catch { }
        try
        {
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            if (endpoints.Count > 0) return endpoints[0];
        }
        catch { }
        return null;
    }

    private void UpdateActiveDevice()
    {
        if (IsDisposed) return;
        MMDevice? candidate = null;
        if (_enumerator != null)
        {
            if (!string.IsNullOrEmpty(_targetDeviceId))
            {
                try
                {
                    candidate = _enumerator.GetDevice(_targetDeviceId);
                    if (candidate.State != DeviceState.Active) { candidate.Dispose(); candidate = null; }
                }
                catch { candidate?.Dispose(); candidate = null; }
            }
            candidate ??= TryFindFallbackEndpoint(_enumerator);
        }
        if (candidate == null)
        {
            try
            {
                var fresh = new MMDeviceEnumerator();
                candidate = TryFindFallbackEndpoint(fresh);
                if (candidate != null)
                {
                    try { _enumerator?.UnregisterEndpointNotificationCallback(this); } catch { }
                    try { _enumerator?.Dispose(); } catch { }
                    _enumerator = fresh;
                    try { _enumerator.RegisterEndpointNotificationCallback(this); } catch { }
                }
                else
                {
                    fresh.Dispose();
                }
            }
            catch { }
        }

        // Do not activate a second endpoint-volume listener merely to inspect the candidate.
        if (candidate != null && _currentDevice != null && _volumeHandler != null)
        {
            try
            {
                if (_currentDevice.State == DeviceState.Active && string.Equals(candidate.ID, _currentId, StringComparison.OrdinalIgnoreCase))
                {
                    bool muted = _currentDevice.AudioEndpointVolume.Mute; // also detects invalidated audio-service objects
                    if (_lockMuteState && _lockedTargetMute is bool lockedMuted && muted != lockedMuted)
                    {
                        _currentDevice.AudioEndpointVolume.Mute = lockedMuted;
                        muted = lockedMuted;
                    }
                    else
                    {
                        _lockedTargetMute = muted;
                    }
                    if (_syncAllMicrophones) ApplySyncToAllEndpoints(muted, candidate.ID);
                    string name = candidate.FriendlyName;
                    bool changed = _muteState.Current != muted || name != _currentName;
                    Volatile.Write(ref _currentName, name);
                    _muteState.RecordLocalChange(muted);
                    Volatile.Write(ref _cachedMuteState, muted ? 1 : 0);
                    candidate.Dispose();
                    if (changed) MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(muted, false));
                    return;
                }
            }
            catch (Exception ex) { DiagnosticLogger.LogWarning("Current endpoint invalidated, will rebind: " + ex.Message); }
        }

        DetachCurrentDevice();
        _currentDevice = candidate;
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
            if (_lockMuteState && _lockedTargetMute is bool lockedRebindMuted && muted != lockedRebindMuted)
            {
                candidate.AudioEndpointVolume.Mute = lockedRebindMuted;
                muted = lockedRebindMuted;
            }
            else
            {
                _lockedTargetMute = muted;
            }
            if (_syncAllMicrophones) ApplySyncToAllEndpoints(muted, candidate.ID);
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
                if (_lockMuteState && (_lockedTargetMute ?? _muteState.Current) is bool lockedMuted && muted != lockedMuted)
                {
                    expected.AudioEndpointVolume.Mute = lockedMuted;
                    if (_syncAllMicrophones) ApplySyncToAllEndpoints(lockedMuted, expected.ID);
                    _muteState.RecordLocalChange(lockedMuted);
                    Volatile.Write(ref _cachedMuteState, lockedMuted ? 1 : 0);
                    return;
                }
                if (!_muteState.TryApplyNotification(muted, () => expected.AudioEndpointVolume.Mute, out bool current)) return;
                _lockedTargetMute = current;
                if (_syncAllMicrophones) ApplySyncToAllEndpoints(current, expected.ID);
                Volatile.Write(ref _cachedMuteState, current ? 1 : 0);
                MuteStateChanged?.Invoke(this, new MuteStateChangedEventArgs(current, true));
            }
            catch (Exception ex) { WarningNotification?.Invoke(this, "Could not read microphone state: " + ex.Message); }
        });
    }

    private void Queue(Action action)
    {
        if (IsDisposed) return;
        Task queued = _workQueue.InvokeAsync(() => { if (!IsDisposed) action(); });
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
        try { if (handler != null) previous.AudioEndpointVolume.OnVolumeNotification -= handler; } catch (Exception ex) { DiagnosticLogger.LogError("Failed to unsubscribe OnVolumeNotification", ex); }
        try { previous.Dispose(); } catch (Exception ex) { DiagnosticLogger.LogError("Failed to dispose MMDevice", ex); }
    }

    private void NotifyDevicesChanged()
    {
        Queue(() =>
        {
            UpdateActiveDevice();
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => NotifyDevicesChanged();
    public void OnDeviceAdded(string deviceId) => NotifyDevicesChanged();
    public void OnDeviceRemoved(string deviceId) => NotifyDevicesChanged();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Capture && (role == Role.Communications || role == Role.Console || role == Role.Multimedia))
            NotifyDevicesChanged();
    }
    private static readonly Guid PkeyDeviceFriendlyNameFmtId = new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);
    private static readonly Guid PkeyDeviceInterfaceFriendlyNameFmtId = new(0x026e516e, 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22);

    public void OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
        bool isFriendlyNameChange = (key.formatId == PkeyDeviceFriendlyNameFmtId && key.propertyId == 14)
            || (key.formatId == PkeyDeviceInterfaceFriendlyNameFmtId && key.propertyId == 2);
        if (isFriendlyNameChange && string.Equals(deviceId, CurrentDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            NotifyDevicesChanged();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        DevicesChanged = null;
        MuteStateChanged = null;
        WarningNotification = null;
        Task cleanup = _workQueue.InvokeAsync(() =>
        {
            try { _enumerator?.UnregisterEndpointNotificationCallback(this); } catch (Exception ex) { DiagnosticLogger.LogError("Failed to unregister notification callback", ex); }
            DetachCurrentDevice();
            try { _enumerator?.Dispose(); } catch (Exception ex) { DiagnosticLogger.LogError("Failed to dispose enumerator", ex); }
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
        GC.SuppressFinalize(this);
    }
}
