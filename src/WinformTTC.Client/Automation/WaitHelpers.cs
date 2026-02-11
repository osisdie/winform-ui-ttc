using System.Runtime.InteropServices;

namespace WinformTTC.Client.Automation;

public static class WaitHelpers
{
    public static async Task<bool> WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var poll = pollInterval ?? TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (condition())
                    return true;
            }
            catch (COMException)
            {
                // UIA COM calls can time out when the target app is
                // temporarily unresponsive (e.g. during heavy streaming).
                // Swallow and keep polling until our own deadline expires.
            }
            catch (InvalidOperationException)
            {
                // FlaUI may throw when the element tree is temporarily
                // unavailable. Safe to retry.
            }

            await Task.Delay(poll);
        }

        // Final attempt — let exceptions propagate on the last check
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
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
