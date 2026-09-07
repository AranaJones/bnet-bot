namespace BNetDiscordBridge;

/// <summary>
/// Defines a trigger pattern that causes the bot to automatically respond with a message.
/// Can match on keywords or regex patterns in incoming chat.
/// </summary>
public sealed class ChatTrigger
{
    /// <summary>Unique identifier for this trigger.</summary>
    public string Id { get; set; } = "";

    /// <summary>If true, the pattern is a regex. If false, it's a case-insensitive substring match.</summary>
    public bool IsRegex { get; set; }

    /// <summary>The pattern to match (substring or regex).</summary>
    public string Pattern { get; set; } = "";

    /// <summary>The response message the bot will send when triggered.</summary>
    public string Response { get; set; } = "";

    /// <summary>If true, only respond to Discord messages. If false, respond to both Discord and Battle.net.</summary>
    public bool DiscordOnly { get; set; } = false;

    /// <summary>If true, only respond to Battle.net messages. If false, respond to both.</summary>
    public bool BnetOnly { get; set; } = false;

    /// <summary>Cooldown in seconds before this trigger can fire again.</summary>
    public int CooldownSeconds { get; set; } = 0;
}

/// <summary>
/// Manages chat triggers and tracks their last-fired times for cooldown enforcement.
/// </summary>
public sealed class ChatTriggerManager
{
    private readonly List<ChatTrigger> _triggers = new();
    private readonly Dictionary<string, DateTime> _lastFiredTimes = new();
    private readonly object _lock = new();

    public void AddTrigger(ChatTrigger trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger.Pattern))
            throw new ArgumentException("Trigger pattern cannot be empty.");
        lock (_lock)
        {
            _triggers.Add(trigger);
        }
    }

    public void RemoveTrigger(string triggerId)
    {
        lock (_lock)
        {
            _triggers.RemoveAll(t => t.Id == triggerId);
            _lastFiredTimes.Remove(triggerId);
        }
    }

    public IReadOnlyList<ChatTrigger> GetAllTriggers()
    {
        lock (_lock)
        {
            return _triggers.AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if a message matches any active triggers and returns the response(s).
    /// Takes into account source (Discord/BNet), cooldowns, and pattern matching.
    /// </summary>
    public List<string> GetMatchingResponses(string message, bool fromDiscord)
    {
        var responses = new List<string>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            foreach (var trigger in _triggers)
            {
                // Check source filter
                if (trigger.DiscordOnly && !fromDiscord) continue;
                if (trigger.BnetOnly && fromDiscord) continue;

                // Check cooldown
                if (_lastFiredTimes.TryGetValue(trigger.Id, out var lastFired))
                {
                    var secondsElapsed = (now - lastFired).TotalSeconds;
                    if (secondsElapsed < trigger.CooldownSeconds)
                        continue;
                }

                // Check pattern match
                if (!MatchesPattern(message, trigger.Pattern, trigger.IsRegex))
                    continue;

                // Trigger matched!
                responses.Add(trigger.Response);
                _lastFiredTimes[trigger.Id] = now;
            }
        }

        return responses;
    }

    private static bool MatchesPattern(string text, string pattern, bool isRegex)
    {
        if (isRegex)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
