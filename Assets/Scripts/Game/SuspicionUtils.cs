using UnityEngine;

/// <summary>
/// Utility functions for applying suspicion penalties. This helper centralises
/// amplification logic when the player's suspicion bar is already high and
/// logs the cause to Agent Miller's log.
/// </summary>
public static class SuspicionUtils
{
    /// <summary>
    /// Applies a suspicion change to the player's stats, automatically
    /// amplifying positive deltas when the suspicion level is >= 50%. The
    /// amplification factor is 2. Logs the reason for the suspicion change.
    /// </summary>
    /// <param name="stats">The player stats instance.</param>
    /// <param name="baseDelta">The base change in suspicion (positive to increase, negative to decrease).</param>
    /// <param name="reason">A description of why the suspicion changed.</param>
    public static void ApplySuspicion(PlayerStats stats, float baseDelta, string reason)
    {
        if (stats == null || Mathf.Approximately(baseDelta, 0f)) return;
        float delta = baseDelta;
        bool amplified = false;
        // Only amplify increases when suspicion is at or above 50%
        if (baseDelta > 0f && stats.Suspicion >= 0.5f)
        {
            delta = baseDelta * 2f;
            amplified = true;
        }
        stats.ChangeSuspicion(delta);
        // Log the event, noting if it was amplified
        if (!string.IsNullOrEmpty(reason))
        {
            if (amplified)
                SuspicionLogger.Add(reason + " (suspicion amplified due to high level)");
            else
                SuspicionLogger.Add(reason);
        }
    }
}