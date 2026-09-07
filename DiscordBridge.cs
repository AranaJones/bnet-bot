using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using BNetDiscordBridge.Bncs;

namespace BNetDiscordBridge.Discord;

public sealed class DiscordBridge : IAsyncDisposable
{
    private readonly DiscordSocketClient _discord = new(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
    });

    private readonly BncsClient _bnet;
    private readonly ulong _bridgeChannelId;
    private readonly string _commandPrefix;
    private readonly ChatTriggerManager _triggerManager;

    public DiscordBridge(BncsClient bnet, ulong bridgeChannelId, string commandPrefix = "!", ChatTriggerManager? triggerManager = null)
    {
        _bnet = bnet;
        _bridgeChannelId = bridgeChannelId;
        _commandPrefix = commandPrefix;
        _triggerManager = triggerManager ?? new ChatTriggerManager();

        // Battle.net -> Discord
        _bnet.OnChatMessage += msg =>
        {
            var prefix = msg.Kind == ChatEventId.EID_EMOTE ? "* " : "";
            _ = PostToDiscordAsync($"**[BNet] {msg.Username}:** {prefix}{msg.Text}");

            // Check for auto-responses to Battle.net chat
            var responses = _triggerManager.GetMatchingResponses(msg.Text, fromDiscord: false);
            foreach (var response in responses)
            {
                _ = Task.Delay(500).ContinueWith(_ => _bnet.SayAsync(response));
            }
        };
        _bnet.OnUserJoined += name => _ = PostToDiscordAsync($"_→ {name} joined the channel_");
        _bnet.OnUserLeft += name => _ = PostToDiscordAsync($"_← {name} left the channel_");
        _bnet.OnServerInfo += text => _ = PostToDiscordAsync($"ℹ️ {text}");
        _bnet.OnServerError += text => _ = PostToDiscordAsync($"⚠️ {text}");
        _bnet.OnDisconnected += () => _ = PostToDiscordAsync("🔌 Disconnected from Battle.net.");

        _discord.MessageReceived += OnDiscordMessageAsync;
        _discord.Log += log => { Console.WriteLine(log.ToString()); return Task.CompletedTask; };
    }

    public async Task StartAsync(string discordToken)
    {
        await _discord.LoginAsync(TokenType.Bot, discordToken);
        await _discord.StartAsync();
    }

    private async Task PostToDiscordAsync(string content)
    {
        if (_discord.GetChannel(_bridgeChannelId) is IMessageChannel channel)
            await channel.SendMessageAsync(SanitizeForDiscord(content));
    }

    // Discord -> Battle.net, plus moderation commands issued from Discord.
    private async Task OnDiscordMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;
        if (message.Channel.Id != _bridgeChannelId) return;

        var content = message.Content;

        if (content.StartsWith(_commandPrefix))
        {
            await HandleModerationCommandAsync(content[_commandPrefix.Length..].Trim(), message);
            return;
        }

        // Check for auto-responses to Discord chat
        var responses = _triggerManager.GetMatchingResponses(content, fromDiscord: true);
        foreach (var response in responses)
        {
            await message.Channel.SendMessageAsync(SanitizeForDiscord(response));
        }

        // Plain chat relay: Discord display name + message -> BNCS channel.
        var author = message.Author is SocketGuildUser gUser ? gUser.DisplayName : message.Author.Username;
        var text = $"[Discord] {author}: {content}";
        // BNCS chat lines have a length cap (~255 bytes); trim defensively.
        if (text.Length > 250) text = text[..247] + "...";
        await _bnet.SayAsync(text);
    }

    /// <summary>
    /// !kick name / !ban name reason / !unban name / !whois name / !say text
    /// map straight onto Battle.net's own slash commands, which the server
    /// enforces based on the bot's own channel-op status — so the bot's
    /// Battle.net account needs to actually be a channel op for kick/ban to work.
    /// </summary>
    private async Task HandleModerationCommandAsync(string commandLine, SocketUserMessage message)
    {
        var parts = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        var cmd = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1] : "";

        // Restrict moderation commands to Discord users with kick/ban perms.
        var isMod = message.Author is SocketGuildUser gu &&
                    (gu.GuildPermissions.KickMembers || gu.GuildPermissions.BanMembers || gu.GuildPermissions.Administrator);

        switch (cmd)
        {
            case "kick" when isMod && rest.Length > 0:
                await _bnet.SayAsync($"/kick {rest}");
                break;
            case "ban" when isMod && rest.Length > 0:
                await _bnet.SayAsync($"/ban {rest}");
                break;
            case "unban" when isMod && rest.Length > 0:
                await _bnet.SayAsync($"/unban {rest}");
                break;
            case "whois" when rest.Length > 0:
                await _bnet.SayAsync($"/whois {rest}");
                break;
            case "say" when isMod && rest.Length > 0:
                await _bnet.SayAsync(rest);
                break;
            case "kick" or "ban" or "unban" or "say":
                await message.Channel.SendMessageAsync("You need kick/ban permissions in Discord to use that.");
                break;
            default:
                await message.Channel.SendMessageAsync(
                    $"Unknown command. Available: {_commandPrefix}say, {_commandPrefix}kick, {_commandPrefix}ban, {_commandPrefix}unban, {_commandPrefix}whois");
                break;
        }
    }

    private static string SanitizeForDiscord(string s) =>
        Regex.Replace(s, "(?<!\\\\)([_*~`|])", "\\$1"); // escape Discord markdown from BNet text

    public ChatTriggerManager GetTriggerManager() => _triggerManager;

    public async ValueTask DisposeAsync()
    {
        await _discord.StopAsync();
        _discord.Dispose();
    }
}
