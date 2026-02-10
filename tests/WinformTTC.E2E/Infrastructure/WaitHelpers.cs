namespace WinformTTC.E2E.Infrastructure;

public static class WaitHelpers
{
    public static async Task<bool> WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var poll = pollInterval ?? TimeSpan.FromMilliseconds(250);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            await Task.Delay(poll);
        }

        return condition();
    }

    public static async Task<bool> WaitForStatusAsync(
        Func<string> getStatus,
        string expected,
        TimeSpan timeout)
    {
        return await WaitUntilAsync(
            () => string.Equals(getStatus(), expected, StringComparison.OrdinalIgnoreCase),
            timeout);
    }

    public static async Task<bool> WaitForStatusContainsAsync(
        Func<string> getStatus,
        string substring,
        TimeSpan timeout)
    {
        return await WaitUntilAsync(
            () => getStatus().Contains(substring, StringComparison.OrdinalIgnoreCase),
            timeout);
    }
}
