
using System;
using System.Threading;
using System.Threading.Tasks;
public class AsyncHelper
{
    public static async Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> op, int attempts = 3, int delay = 300, CancellationToken ct = default)
    {
        int attempt = 0;
        int delayTime = delay;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await op(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                attempt++;
                if (attempt >= attempts)
                {
                    throw;
                }
                await Task.Delay(delay, ct);
                delayTime *= 2;
            }
        }
    }

    public static async Task<T> WithTimeout<T>(Func<CancellationToken, Task<T>> op, TimeSpan timeout, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var task = op(cts.Token);
        var delay = Task.Delay(timeout, cts.Token);
        var finishedTask = await Task.WhenAny(task, delay);
        if (finishedTask == task)
        {
            cts.Cancel();
            throw new TimeoutException($"Timeout waiting for operation: " + timeout.ToString());
        }
        return await task;
    }
    
}
