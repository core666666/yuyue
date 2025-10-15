using System;
using System.Diagnostics;

namespace YuYue.Services;

public class ReadingTimerService
{
    private readonly Stopwatch _stopwatch = new();
    private DateTime _sessionStart;
    private int _totalMinutesBeforeSession;

    public bool IsRunning => _stopwatch.IsRunning;
    public TimeSpan CurrentSessionTime => _stopwatch.Elapsed;
    public int TotalMinutes => _totalMinutesBeforeSession + (int)_stopwatch.Elapsed.TotalMinutes;

    public void Start(int previousTotalMinutes = 0)
    {
        if (_stopwatch.IsRunning)
            return;
            
        _totalMinutesBeforeSession = previousTotalMinutes;
        _sessionStart = DateTime.Now;
        _stopwatch.Start();
    }

    public void Pause()
    {
        if (!_stopwatch.IsRunning)
            return;
            
        _stopwatch.Stop();
    }

    public void Resume()
    {
        if (_stopwatch.IsRunning)
            return;
            
        _stopwatch.Start();
    }

    public int Stop()
    {
        _stopwatch.Stop();
        var totalMinutes = TotalMinutes;
        _stopwatch.Reset();
        return totalMinutes;
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _totalMinutesBeforeSession = 0;
    }
}
