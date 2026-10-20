using Examples.AudioPlayer.Library;
using Examples.Common;
using Rin.Core.Audio;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     The queue and the stream of the track playing from it. Only the current track has an open stream.
/// </summary>
public sealed class Player : IDisposable
{
    private const double RestartThresholdSeconds = 3.0;
    private static readonly Log Log = Log.For<Player>();

    private readonly List<QueueEntry> _queue = [];
    private IAudioSample? _sample;
    private IActiveAudio? _audio;

    public IReadOnlyList<QueueEntry> Queue => _queue;

    public int CurrentIndex { get; private set; } = -1;

    public QueueEntry? Current => CurrentIndex >= 0 && CurrentIndex < _queue.Count ? _queue[CurrentIndex] : null;

    public bool IsPlaying => _audio?.IsPlaying ?? false;

    public double Position => _audio?.Position ?? 0.0;

    public double Duration => _audio?.Duration ?? Current?.Info.Duration ?? 0.0;

    public bool Repeat { get; set; }

    public float Volume
    {
        get => IAudioModule.Get().GetVolume();
        set => IAudioModule.Get().SetVolume(float.Clamp(value, 0f, 1f));
    }

    public event Action? QueueChanged;

    public event Action? TrackChanged;

    public void Add(IReadOnlyList<TrackInfo> tracks)
    {
        if (tracks.Count == 0) return;

        foreach (var track in tracks) _queue.Add(new QueueEntry(track));
        QueueChanged?.Invoke();
        if (Current is null) Play(0);
    }

    /// <summary>
    ///     Throws the queue away, queues these tracks and plays the one at <paramref name="startIndex" />.
    /// </summary>
    public void Replace(IReadOnlyList<TrackInfo> tracks, int startIndex = 0)
    {
        CloseAudio();
        foreach (var entry in _queue) entry.Dispose();
        _queue.Clear();
        CurrentIndex = -1;
        foreach (var track in tracks) _queue.Add(new QueueEntry(track));
        QueueChanged?.Invoke();
        if (_queue.Count > 0) Play(int.Clamp(startIndex, 0, _queue.Count - 1));
        else TrackChanged?.Invoke();
    }

    public void Remove(int index)
    {
        if (index < 0 || index >= _queue.Count) return;

        var removingCurrent = index == CurrentIndex;
        var entry = _queue[index];
        _queue.RemoveAt(index);
        entry.Dispose();

        if (removingCurrent)
        {
            CloseAudio();
            CurrentIndex = -1;
            QueueChanged?.Invoke();
            if (_queue.Count > 0) Play(int.Min(index, _queue.Count - 1));
            else TrackChanged?.Invoke();
            return;
        }

        if (index < CurrentIndex) CurrentIndex--;
        QueueChanged?.Invoke();
    }

    public void Play(int index)
    {
        if (index < 0 || index >= _queue.Count) return;

        CloseAudio();
        CurrentIndex = index;
        Log.Info($"Playing '{_queue[index].Info.Title}' ({_queue[index].Info.Path})");
        _sample = IAudioModule.Get().MakeStream(_queue[index].Info.Path);
        _audio = _sample.MakeActive();
        _audio.Play();
        TrackChanged?.Invoke();
    }

    public void TogglePlay()
    {
        if (_audio is null)
        {
            if (_queue.Count > 0) Play(int.Max(CurrentIndex, 0));
            return;
        }

        if (_audio.IsPlaying)
            _audio.Pause();
        else
            _audio.Play();
    }

    public void Next()
    {
        if (_queue.Count == 0) return;

        var next = CurrentIndex + 1;
        if (next < _queue.Count)
        {
            Play(next);
            return;
        }

        if (Repeat) Play(0);
    }

    public void Previous()
    {
        if (_queue.Count == 0) return;

        if (Position > RestartThresholdSeconds || CurrentIndex <= 0)
            Seek(0.0);
        else
            Play(CurrentIndex - 1);
    }

    public void Seek(double seconds)
    {
        _audio?.SetPosition(double.Clamp(seconds, 0.0, Duration));
    }

    /// <summary>
    ///     Moves on to the next track once the current one has run off its end.
    /// </summary>
    public void Update()
    {
        if (_audio is { HasEnded: true } && HasNext) Next();
    }

    private bool HasNext => Repeat || CurrentIndex + 1 < _queue.Count;

    private void CloseAudio()
    {
        _audio?.Dispose();
        _audio = null;
        _sample?.Dispose();
        _sample = null;
    }

    public void Dispose()
    {
        CloseAudio();
        foreach (var entry in _queue) entry.Dispose();
        _queue.Clear();
        CurrentIndex = -1;
    }
}
