using System;
using System.Collections.Generic;

/// <summary>
/// Centralised logger for recording suspicious actions. Whenever the player's
/// behaviour deviates from normal human patterns, an entry should be added
/// to this logger. The log can then be displayed to the player via the HUD.
/// </summary>
public static class SuspicionLogger
{
    // Internal list of log messages. Each entry contains a timestamp and the message.
    private static readonly List<string> logs = new List<string>();

    /// <summary>
    /// Append a new entry to the suspicion log. Prefixes the message with
    /// the current date and time for context.
    /// </summary>
    public static void Add(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        logs.Add($"[{timestamp}] {message}");
    }

    /// <summary>
    /// Returns the full log as a single string separated by newlines. If no
    /// entries have been logged yet, returns a default message.
    /// </summary>
    public static string GetLogText()
    {
        if (logs.Count == 0)
            return "No suspicious activity recorded.";
        return string.Join("\n", logs.ToArray());
    }

    /// <summary>
    /// Clears all log entries. This can be used when starting a new game.
    /// </summary>
    public static void Clear()
    {
        logs.Clear();
    }
}