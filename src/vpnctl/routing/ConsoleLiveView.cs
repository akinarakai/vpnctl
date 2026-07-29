using System.Diagnostics;

public class ConsoleLiveView
{
    private readonly Stopwatch _sw;
    private readonly int _tableWidth;

    private readonly TimeSpan? _duration;
    private readonly int? _iterations;

    private int _startLeft;
    private int _startTop;
    private int _currentIterations = 0;

    public ConsoleLiveView(TimeSpan? duration = null, int tableWidth = 95)
    {
        _sw = new Stopwatch();
        _duration = duration ?? TimeSpan.FromMinutes(1);
        _tableWidth = tableWidth;
    }

    public ConsoleLiveView(int iterations, int tableWidth = 95)
    {
        _sw = new Stopwatch();
        _iterations = iterations;
        _tableWidth = tableWidth;
    }

    public void Start()
    {
        Console.CursorVisible = false;

        _startLeft = Console.CursorLeft;
        _startTop = Console.CursorTop;

        _sw.Start();
    }

    public void Stop()
    {
        _sw.Stop();
        Console.CursorVisible = true;
    }

    public bool KeepRunning()
    {
        if (IsEnded())
        {
            Stop();
            return false;
        }

        _currentIterations++;

        Console.SetCursorPosition(_startLeft, _startTop);
        return true;
    }

    public void Wait(TimeSpan delay)
    {
        if (IsEnded()) return;

        Thread.Sleep(delay);
    }

    public void WriteLine(string text)
    {
        if (text.Length < _tableWidth)
        {
            text = text.PadRight(_tableWidth);
        }
        Console.WriteLine(text);
    }

    private bool IsEnded()
    {
        if (_duration.HasValue)
        {
            return _sw.Elapsed >= _duration.Value;
        }

        if (_iterations.HasValue)
        {
            return _currentIterations >= _iterations.Value;
        }

        return true;
    }
}