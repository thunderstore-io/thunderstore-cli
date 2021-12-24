namespace ThunderstoreCLI;

public class ProgressSpinner
{
    private int _lastSeenCompleted = 0;
    private ushort _spinIndex = 0;
    private static readonly char[] _spinChars = { '|', '/', '-', '\\' };
    private readonly string _label;
    private readonly Task[] _tasks;

    public ProgressSpinner(string label, Task[] tasks)
    {
        if (tasks.Length == 0)
        {
            throw new ArgumentException("Task list can't be empty", nameof(tasks));
        }

        _label = label;
        _tasks = tasks;
    }

    public async Task Start()
    {
        // Cursor operations are not always available e.g. in GitHub Actions environment. Done up here to minimize exception usage (throws and catches are expensive and all)
        bool canUseCursor;
        try
        {
            // nop that will throw if cursor position can't be gotten
            _ = Console.CursorTop;
            canUseCursor = true;
        }
        catch
        {
            canUseCursor = false;
        }

        while (true)
        {
            IEnumerable<Task> faultedTasks;
            if ((faultedTasks = _tasks.Where(static x => x.IsFaulted)).Any())
            {
                Write.Empty();
                throw new SpinnerException("Some of the tasks have faulted", faultedTasks.Select(x => x.Exception!));
            }

            var completed = _tasks.Count(static x => x.IsCompleted);

            if (canUseCursor)
            {
                var spinner = completed == _tasks.Length ? '✓' : _spinChars[_spinIndex++ % _spinChars.Length];
                Console.SetCursorPosition(0, Console.CursorTop);
                Write.Success($"{completed}/{_tasks.Length} {_label} {spinner}");
            }
            else
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
public class SpinnerException : AggregateException
{
    public SpinnerException(string message, IEnumerable<Exception> innerExceptions)
        : base(message, innerExceptions)
    {
    }
}
