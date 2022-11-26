namespace ThunderstoreCLI.Utils;

public static class ActionUtils
{
    public static void Retry(int maxTryCount, Action action)
    {
        for (int i = 1; i <= maxTryCount; i++)
        {
            try
            {
                action();
                return;
            }
            catch
            {
                if (i == maxTryCount)
                {
                    throw;
                }
            }
        }
    }

    public static async Task RetryAsync(int maxTryCount, Func<Task> action)
    {
        for (int i = 1; i <= maxTryCount; i++)
        {
            try
            {
                await action();
                return;
            }
            catch
            {
                if (i == maxTryCount)
                {
                    throw;
                }
            }
        }
    }
}
