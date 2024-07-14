using System;
using System.Threading.Tasks;

namespace KoC.utils;

public static class RetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int maxRetries)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                attempt++;
                Plugin.Instance.GetLogger().LogError($"Attempt {attempt} to execute action failed: {e.Message}");

                if (attempt >= maxRetries)
                {
                    Plugin.Instance.GetLogger().LogError("Max retries reached. Unable to complete action.");
                    throw;
                }

                await Task.Delay(1000);
            }
        }

        throw new InvalidOperationException("This code should not be reachable.");
    }
}