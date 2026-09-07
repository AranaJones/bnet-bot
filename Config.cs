namespace BNetDiscordBridge;

public sealed class BridgeConfig
{
    // Battle.net side
    public string BnetHost { get; set; } = "useast.battle.net"; // or your PvPGN server's address/port
    public int BnetPort { get; set; } = 6112;
    public string BnetUsername { get; set; } = "";
    public string BnetPassword { get; set; } = "";
    public string BnetChannel { get; set; } = "Clan Home";
    public string BnetProductId { get; set; } = "STAR"; // 4CC for your game

    // Discord side
    public string DiscordToken { get; set; } = "";
    public ulong DiscordBridgeChannelId { get; set; }
    public ulong DiscordOwnerId { get; set; } // Owner's Discord user ID for DM notifications
    public string CommandPrefix { get; set; } = "!";
}
