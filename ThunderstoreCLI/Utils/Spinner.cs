using static Crayon.Output;

namespace ThunderstoreCLI;

public class ProgressSpinner
{
    private int _lastSeenCompleted = 0;
    private ushort _spinIndex = 0;
    private string[] _spinChars = { "|", "/", "-", "\\" };
    private string _label;
    private readonly Task[] _tasks;

    public ProgressSpinner(string label, Task[] tasks)
    {
        if (tasks.Length == 0)
        {
            throw new SpinnerException("Task list can't be empty");
        }

        _label = label;
        _tasks = tasks;
    }

    public async Task Start()
    {
        while (true)
        {
            if (_tasks.Any(x => x.IsFaulted))
            {
                Write.Empty();
                throw new SpinnerException("Some of the tasks have faulted");
            }

            var completed = _tasks.Count(static x => x.IsCompleted);
            var spinner = completed == _tasks.Length ? "✓" : _spinChars[_spinIndex++ % _spinChars.Length];

            // Cursor operations are not always available e.g. in GitHub Actions environment.
            try
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(Green($"{completed}/{_tasks.Length} {_label} {spinner}"));
            }
            catch (IOException)
            {
                if (completed > _lastSeenCompleted)
                {
                    Write.Success($"{completed}/{_tasks.Length} {_label}");
                    _lastSeenCompleted = completed;
                }
            }

            if (completed == _tasks.Length)
            {
                Write.Empty();
                return;
            }

            await Task.Delay(200);
        }
    }
}

[Serializable]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public class SpinnerException : Exception
{
    public SpinnerException()
    {
    }

    public SpinnerException(string message)
        : base(message)
    {
    }

    public SpinnerException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
