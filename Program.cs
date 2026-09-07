using Microsoft.Extensions.Configuration;
using BNetDiscordBridge;
using BNetDiscordBridge.Bncs;
using BNetDiscordBridge.Discord;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables(prefix: "BRIDGE_")
    .Build();

var config = new BridgeConfig();
configuration.Bind(config);

if (string.IsNullOrWhiteSpace(config.DiscordToken))
    throw new InvalidOperationException("Set DiscordToken in appsettings.json or BRIDGE_DiscordToken env var.");
if (string.IsNullOrWhiteSpace(config.BnetUsername))
    throw new InvalidOperationException("Set BnetUsername/BnetPassword — see README for CD key handling.");
if (config.DiscordOwnerId == 0)
    throw new InvalidOperationException("Set DiscordOwnerId in appsettings.json or BRIDGE_DiscordOwnerId env var (your Discord user ID).");

// TODO: replace with a real IGameAuthProvider backed by BNCSutil (or a port
// of it) for the exact game/version you're connecting as. See
// Bncs/IGameAuthProvider.cs for why this can't be a generic implementation.
IGameAuthProvider auth = throw new NotImplementedException(
    "Wire up an IGameAuthProvider implementation (CD key hash + CheckRevision) before running. See README.md.");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Create trigger manager and add some example triggers
var triggerManager = new ChatTriggerManager();
triggerManager.AddTrigger(new ChatTrigger
{
    Id = "hello",
    Pattern = "hello",
    Response = "Hello! Welcome to the channel.",
    IsRegex = false,
    CooldownSeconds = 5
});

triggerManager.AddTrigger(new ChatTrigger
{
    Id = "how are you",
    Pattern = "how are you",
    Response = "I'm doing great, thanks for asking!",
    IsRegex = false,
    CooldownSeconds = 10
});

triggerManager.AddTrigger(new ChatTrigger
{
    Id = "bot alive",
    Pattern = "^bot alive",
    Response = "Yes, I'm alive and listening!",
    IsRegex = true,
    CooldownSeconds = 5
});

await using var bnet = new BncsClient(
    config.BnetHost, config.BnetPort,
    config.BnetUsername, config.BnetPassword,
    config.BnetChannel, auth);

await using var bridge = new DiscordBridge(bnet, config.DiscordBridgeChannelId, config.DiscordOwnerId, config.CommandPrefix, triggerManager);

await bridge.StartAsync(config.DiscordToken);
await bnet.ConnectAndLoginAsync(cts.Token);

Console.WriteLine("Bridge running. Press Ctrl+C to stop.");
Console.WriteLine("Active triggers:");
foreach (var trigger in triggerManager.GetAllTriggers())
{
    Console.WriteLine($"  - {trigger.Id}: '{trigger.Pattern}' -> '{trigger.Response}'");
}

try { await Task.Delay(Timeout.Infinite, cts.Token); }
catch (TaskCanceledException) { }
