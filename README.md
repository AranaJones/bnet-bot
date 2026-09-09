# BNetDiscordBridge - Complete Bot

A C#/.NET bot that relays chat both ways between a Discord channel and a
classic Battle.net (BNCS) chat channel, plus moderation commands from Discord,
automatic triggers, and owner notifications.

## Features

### Core Features
- ✅ Two-way chat relay between Discord and Battle.net
- ✅ Moderation commands: `!kick`, `!ban`, `!unban`, `!whois`, `!say`
- ✅ Owner notifications via DM for important events
- ✅ DM forwarding: Any user DMs to the bot are sent to the owner
- ✅ Battle.net contact detection: When someone says "contact member", owner is notified
- ✅ Bot master command: Owner-only `!botmaster` to send messages to Battle.net
- ✅ Chat triggers: Automatic responses to keywords/regex patterns with cooldowns
- ✅ Permission-gated commands: Only mods can use moderation features
- ✅ BNLS support: Uses Battle.net Login Server for authentication

### What's Implemented
- BNCS packet framing and protocol handling
- Full Battle.net login handshake (SID_AUTH_INFO → SID_LOGONRESPONSE2 → SID_JOINCHANNEL)
- Chat event parsing (joins/parts/talk/emote/whisper/errors)
- Discord integration with slash commands and DM handling
- Chat trigger system with regex and substring matching
- Owner notification system with DMs and event tracking
- BNLS client for game-independent authentication

## Setup

### Prerequisites
- .NET 8.0 SDK or later
- A Discord bot token
- A Battle.net account (for testing, use PvPGN first)
- Your Discord user ID (for owner notifications)
- A CD key for your target game (StarCraft, Diablo II, etc.)

### Installation

**Option 1: Automated (Windows)**
1. Build `install.exe` from this repository (see "Build Windows installer" below)
2. Follow the setup wizard
3. Edit `appsettings.json` with your settings
4. Run `run.bat`

### Build Windows installer (contributors/CI)
1. Install [NSIS](https://nsis.sourceforge.io/download)
2. Run:
   ```bat
   build-installer.bat
   ```
3. Use the generated `install.exe` in the repository root

`create-installer.bat` and `build-exe-installer.bat` are compatibility wrappers that call `build-installer.bat`.

**Option 2: Manual Setup**
1. Clone the repository
   ```bash
   git clone https://github.com/AranaJones/bnet-bot.git
   cd bnet-bot
   ```

2. Copy example config
   ```bash
   cp appsettings.example.json appsettings.json
   ```

3. Create Discord bot
   - Go to https://discord.com/developers
   - Create new application
   - Enable "Message Content" privileged intent
   - Create bot user and copy token
   - Invite to server with: Send Messages, Read Messages, Read Message History

4. Configure `appsettings.json`
   ```json
   {
     "BnetHost": "useast.battle.net",
     "BnetPort": 6112,
     "BnetUsername": "YourBotAccount",
     "BnetPassword": "your-password",
     "BnetChannel": "Clan Home",
     "BnetProductId": "STAR",
     
     "BnlsHost": "localhost",
     "BnlsPort": 9367,
     
     "DiscordToken": "your-discord-token",
     "DiscordBridgeChannelId": 123456789012345678,
     "DiscordOwnerId": 987654321098765432,
     "CommandPrefix": "!"
   }
   ```

5. Set up BNLS server
   - Download from: https://github.com/HarpyWar/bncsutil
   - Start BNLS (default: localhost:9367)

6. Run the bot
   - Windows: Double-click `run.bat`
   - Linux/Mac: Run `./run.sh`
   - Or: `dotnet run`

### Environment Variables (Recommended)
```bash
export BRIDGE_DiscordToken="your-token"
export BRIDGE_BnetPassword="your-password"
export BRIDGE_DiscordOwnerId="987654321098765432"
```

## Commands

### Discord Commands (in bridge channel)

**Moderation (requires kick/ban permissions):**
- `!kick username` — Kick user from Battle.net
- `!ban username reason` — Ban user from Battle.net
- `!unban username` — Unban user from Battle.net
- `!say message` — Send message to Battle.net
- `!whois username` — Get user info on Battle.net

**Owner Only:**
- `!botmaster message` — Send message directly to Battle.net

### Battle.net Events

**Owner gets DM notifications for:**
- ✅ Bot comes online
- 👤 Player joins/leaves channel
- ℹ️ Server info messages
- ⚠️ Server errors
- 🔌 Bot disconnects
- 📞 Someone says "contact member"

## Chat Triggers

Add automatic responses in `Program.cs`:

```csharp
triggerManager.AddTrigger(new ChatTrigger
{
    Id = "hello",
    Pattern = "hello",
    Response = "Hello there!",
    IsRegex = false,
    CooldownSeconds = 5
});
```

## Architecture

- **Program.cs** — Entry point, configuration, triggers
- **DiscordBridge.cs** — Discord integration, commands
- **BncsClient.cs** — Battle.net protocol client
- **BnlsClient.cs** — BNLS authentication client
- **BncsPacket*.cs** — Protocol framing
- **ChatTrigger.cs** — Trigger system

## Troubleshooting

### "BNLS disconnected" or auth fails
- Make sure BNLS server is running on configured host/port
- Check firewall settings
- Verify `BnlsHost` and `BnlsPort` in config

### Owner not receiving DMs
- Verify `DiscordOwnerId` is correct (right-click user, Copy User ID)
- Enable "Message Content" intent in Discord Developer Portal
- Check bot has permission to DM owner

### Cannot connect to real Battle.net
- Test with [PvPGN](https://pvpgn.pro) first
- Some games now use updated gateways
- Check if game version is still supported

## Requirements

- .NET 8.0+
- Discord.Net 3.14.1+
- Microsoft.Extensions.Configuration 8.0.0+

## License

See LICENSE file.

## Terms of Service

- Only use with owned accounts and CD keys
- No gameplay automation or spam
- Battle.net enforces protocol compliance
- Chat relay bots are different from game-automation bots
- Unauthorized automation can result in bans

---

**Need help?** Check the code comments or open an issue on GitHub.
