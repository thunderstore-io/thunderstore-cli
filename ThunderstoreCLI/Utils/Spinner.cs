using static Crayon.Output;

namespace ThunderstoreCLI.Utils;

public class ProgressSpinner
{
    private int _lastSeenCompleted = 0;
    private ushort _spinIndex = 0;
    private static readonly char[] _spinChars = { '|', '/', '-', '\\' };
    private readonly string _label;
    private readonly Task[] _tasks;
    private readonly int _offset;

    public ProgressSpinner(string label, Task[] tasks, int offset = 0)
    {
        if (tasks.Length == 0)
        {
            throw new ArgumentException("Task list can't be empty", nameof(tasks));
        }

        _label = label;
        _tasks = tasks;
        _offset = offset;
    }

    public async Task Spin()
    {
        // Cursor operations are not always available e.g. in GitHub Actions environment.
        // Done up here to minimize exception usage (throws and catches are expensive and all)
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

        if (!canUseCursor && _offset != 0)
        {
            for (int i = 1; i <= _offset; i++)
            {
                Console.Write(Green($"{0}/{_tasks.Length + _offset} {_label}"));
            }
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
                Console.Write(Green($"{completed + _offset}/{_tasks.Length + _offset} {_label} {spinner}"));
            }
            else
            {
                if (completed > _lastSeenCompleted)
                {
                    Write.Success($"{completed + _offset}/{_tasks.Length + _offset} {_label}");
                    _lastSeenCompleted = completed;
                }
            }

            if (completed == _tasks.Length)
            {
                Write.Empty();
                await Task.WhenAll(_tasks);
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
