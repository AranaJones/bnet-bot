# BNetDiscordBridge

A C#/.NET bot that relays chat both ways between a Discord channel and a
classic Battle.net (BNCS) chat channel, plus moderation commands from Discord
(`!kick`, `!ban`, `!unban`, `!whois`, `!say`).

## What's here vs. what you need to add

This gives you a complete, working implementation of:
- BNCS packet framing (`Bncs/BncsPacketReader.cs`, `BncsPacketWriter.cs`)
- The full connect → SID_AUTH_INFO → SID_AUTH_CHECK → SID_LOGONRESPONSE2 →
  SID_ENTERCHAT → SID_JOINCHANNEL handshake (`Bncs/BncsClient.cs`)
- Chat event parsing (joins/parts/talk/emote/whisper/errors)
- Slash-command passthrough (kick/ban/whisper/etc — same as typing them in a
  real client)
- Full Discord side: relay, markdown-escaping, permission-gated moderation
  commands (`Discord/DiscordBridge.cs`)

Two pieces are **intentionally stubbed**, because they're game-version-specific
and wrong-by-default if hand-rolled without test access to the real client:

1. **`BncsClient.BrokenSha1`** — Battle.net's login hash isn't standard SHA-1
   (there's a documented bug in the reference implementation that real
   Battle.net still expects). `System.Security.Cryptography.SHA1` will *not*
   interoperate. Use BNCSutil's `hashB`/broken-SHA1 routine, or a verified
   .NET port of it.
2. **`IGameAuthProvider`** (`Bncs/IGameAuthProvider.cs`) — CD key hashing and
   the CheckRevision exe-version challenge. These formulas differ per game
   and per client version and are normally computed *from your actual game
   files* at runtime. [BNCSutil](https://github.com/HarpyWar/bncsutil) is the
   standard library for this — wrap it (native P/Invoke, or a .NET port) and
   implement the interface.

Once those two are filled in, `Program.cs` just needs a real
`IGameAuthProvider` instance instead of the `throw` placeholder.

## Setup

1. `cp appsettings.example.json appsettings.json` and fill in your Discord
   bot token, target Discord channel ID, and Battle.net account/channel.
   **Don't commit real secrets** — prefer the `BRIDGE_DiscordToken` /
   `BRIDGE_BnetPassword` etc. environment variables (the config loader reads
   both; env vars win).
2. Create a Discord bot application at https://discord.com/developers,
   enable the **Message Content** privileged intent, invite it to your
   server with Send Messages / Read Message History permissions.
3. Implement `IGameAuthProvider` for your target game using BNCSutil, and
   pass it into `BncsClient` in `Program.cs`.
4. **Test against a PvPGN server first.** PvPGN (https://pvpgn.pro) speaks
   the exact same classic BNCS protocol and is trivial to spin up locally,
   so you can validate the whole handshake without touching production
   Battle.net or risking your real account. Once login/join/chat/kick/ban
   all work against PvPGN, point `BnetHost`/`BnetPort` at Battle.net.
5. For kick/ban to actually work, your bot's Battle.net account needs to be
   a **channel operator** in the target channel — the server enforces that
   server-side regardless of what the bot sends.

## Real Battle.net vs. PvPGN

Blizzard's backend for the original games (Diablo, StarCraft, WC2, WC3
classic) has changed multiple times since 1997, and I can't confirm from
here whether the raw legacy BNCS handshake this code implements is still
accepted for your specific game/version today — some titles now route
through updated gateways rather than the original binary protocol. If
`BnetHost`/`BnetPort` against real Battle.net doesn't get you past
`SID_AUTH_INFO`, that's the likely reason; PvPGN remains a reliable target
either way since it deliberately preserves the classic protocol.

## A note on ToS

Only connect with an account and CD key you actually own, and don't use this
to automate gameplay, spam, or evade bans — Battle.net actively enforces
protocol compliance and unauthorized automation can get an account or IP
banned. A chat relay/moderation bot operating as a normal client (which is
what this is) is a different thing from a game-automation bot.
