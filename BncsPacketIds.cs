namespace BNetDiscordBridge.Bncs;

/// <summary>
/// SID_* message IDs for the classic Battle.net Chat Server (BNCS) protocol.
/// Source: public protocol documentation (bnetdocs.org) used by every
/// open-source BNCS client (PvPGN, bnetdocs sample bots, etc).
/// </summary>
public static class BncsPacketIds
{
    public const byte SID_NULL = 0x00;              // keepalive, no payload
    public const byte SID_CLIENTID = 0x05;
    public const byte SID_STARTVERSIONING = 0x06;
    public const byte SID_REPORTVERSION = 0x07;
    public const byte SID_ENTERCHAT = 0x0A;
    public const byte SID_GETCHANNELLIST = 0x0B;
    public const byte SID_JOINCHANNEL = 0x0C;
    public const byte SID_CHATCOMMAND = 0x0E;        // client -> server: chat text / slash command
    public const byte SID_CHATEVENT = 0x0F;          // server -> client: everything happening in channel
    public const byte SID_LEAVECHAT = 0x10;
    public const byte SID_PING = 0x25;
    public const byte SID_LOGONRESPONSE2 = 0x3A;     // OLS login (most 1.16+ / D2 / WC3 style)
    public const byte SID_AUTH_INFO = 0x50;          // NLS/CheckRevision negotiation start
    public const byte SID_AUTH_CHECK = 0x51;         // CD key + exe version submit
}

/// <summary>
/// Sub-IDs seen inside a SID_CHATEVENT (0x0F) payload's "event id" field.
/// </summary>
public enum ChatEventId : uint
{
    EID_SHOWUSER = 0x01,
    EID_JOIN = 0x02,
    EID_LEAVE = 0x03,
    EID_WHISPER = 0x04,
    EID_TALK = 0x05,
    EID_BROADCAST = 0x06,
    EID_CHANNEL = 0x07,
    EID_USERFLAGS = 0x09,
    EID_WHISPERSENT = 0x0A,
    EID_CHANNELFULL = 0x0D,
    EID_CHANNELDOESNOTEXIST = 0x0E,
    EID_CHANNELRESTRICTED = 0x0F,
    EID_INFO = 0x12,
    EID_ERROR = 0x13,
    EID_EMOTE = 0x17,
}
