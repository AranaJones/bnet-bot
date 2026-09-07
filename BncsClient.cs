using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace BNetDiscordBridge.Bncs;

public sealed record BncsChatMessage(string Username, string Text, ChatEventId Kind);

public sealed class BncsClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _channel;
    private readonly IGameAuthProvider _auth;

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;

    public event Action<BncsChatMessage>? OnChatMessage;
    public event Action<string>? OnUserJoined;
    public event Action<string>? OnUserLeft;
    public event Action<string>? OnServerInfo;
    public event Action<string>? OnServerError;
    public event Action? OnDisconnected;

    public BncsClient(string host, int port, string username, string password, string channel, IGameAuthProvider auth)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _channel = channel;
        _auth = auth;
    }

    public async Task ConnectAndLoginAsync(CancellationToken outerCt)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        var ct = _cts.Token;

        _tcp = new TcpClient();
        await _tcp.ConnectAsync(_host, _port, ct);
        _stream = _tcp.GetStream();

        // Every BNCS connection starts with a single protocol byte, before any packet framing.
        // 0x01 = game client protocol (as opposed to 0x02 FTP-style file transfer, 0x03 old chat proto, etc).
        await _stream.WriteAsync(new byte[] { 0x01 }, ct);

        var clientToken = (uint)Environment.TickCount;

        // --- SID_AUTH_INFO (0x50): announce platform/product, get server token + CheckRevision formula ---
        var authInfo = new BncsPacketWriter(BncsPacketIds.SID_AUTH_INFO)
            .WriteUInt32(0)                 // protocol ID, always 0
            .WriteRaw(Encoding.ASCII.GetBytes("IX86"))   // platform code
            .WriteRaw(Encoding.ASCII.GetBytes(_auth.ProductId)) // product code (reversed 4CC on the wire is handled by callers passing it pre-reversed if needed)
            .WriteUInt32(0)                 // version byte, product-specific — 0 works for many classic titles
            .WriteUInt32(0)                 // language ID (neutral)
            .WriteUInt32(0)                 // local IP, 0 = let server infer
            .WriteUInt32(0)                 // timezone bias
            .WriteUInt32(0)                 // MPQ locale ID
            .WriteUInt32(0)                 // user language ID
            .WriteCString("USA")            // country abbreviation
            .WriteCString("United States")  // country
            .ToBytes();
        await _stream.WriteAsync(authInfo, ct);

        var authInfoReply = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("Connection closed during SID_AUTH_INFO.");
        // Fields include logon type, server token, MPQ filetime, CheckRevision formula string, and version-check archive name.
        authInfoReply.ReadUInt32(); // logon type
        var serverToken = authInfoReply.ReadUInt32();
        authInfoReply.ReadUInt32(); authInfoReply.ReadUInt32(); // MPQ filetime (u64, split)
        authInfoReply.ReadCString(); // ver-check archive filename, e.g. "ver-IX86-1.mpq"
        var checkRevisionFormula = authInfoReply.ReadCString();

        var (exeVersion, exeHash, exeInfo) = _auth.CheckRevision(
            checkRevisionFormula, Array.Empty<string>(), serverToken, clientToken);

        // --- SID_AUTH_CHECK (0x51): submit exe info + CD key hash(es) ---
        var keyInfo = _auth.HashCdKey(Environment.GetEnvironmentVariable("BNET_CDKEY") ?? "", clientToken, serverToken);
        var authCheckWriter = new BncsPacketWriter(BncsPacketIds.SID_AUTH_CHECK)
            .WriteUInt32(clientToken)
            .WriteUInt32(exeVersion)
            .WriteUInt32(exeHash)
            .WriteUInt32(1) // number of CD keys (2 for D2, which needs Classic + expansion keys)
            .WriteUInt32(0) // spawn flag
            .WriteUInt32(keyInfo.keyLength)
            .WriteUInt32(keyInfo.product)
            .WriteUInt32(keyInfo.publicValue)
            .WriteUInt32(0) // unknown/hash pt1 placeholder — real layout embeds hash bytes here per bnetdocs SID_AUTH_CHECK spec
            .WriteRaw(keyInfo.hash)
            .WriteCString(exeInfo)
            .WriteCString("bridge-bot 1.0"); // key owner name field is actually separate; adjust per exact spec revision you target
        await _stream.WriteAsync(authCheckWriter.ToBytes(), ct);

        var authCheckReply = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("Connection closed during SID_AUTH_CHECK.");
        var authResult = authCheckReply.ReadUInt32();
        if (authResult != 0)
        {
            var extra = authCheckReply.ReadCString();
            throw new InvalidOperationException($"SID_AUTH_CHECK rejected (code 0x{authResult:X}): {extra}");
        }

        // --- SID_LOGONRESPONSE2 (0x3A): classic broken-SHA1 double-hash login ---
        var passHash1 = BrokenSha1(Encoding.ASCII.GetBytes(_password));
        var combined = new byte[8 + passHash1.Length];
        BitConverter.GetBytes(clientToken).CopyTo(combined, 0);
        BitConverter.GetBytes(serverToken).CopyTo(combined, 4);
        passHash1.CopyTo(combined, 8);
        var passHash2 = BrokenSha1(combined);

        var logonWriter = new BncsPacketWriter(BncsPacketIds.SID_LOGONRESPONSE2)
            .WriteUInt32(clientToken)
            .WriteUInt32(serverToken)
            .WriteRaw(passHash2)
            .WriteCString(_username);
        await _stream.WriteAsync(logonWriter.ToBytes(), ct);

        var logonReply = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("Connection closed during SID_LOGONRESPONSE2.");
        var logonResult = logonReply.ReadUInt32();
        if (logonResult != 0)
            throw new InvalidOperationException($"Login failed (SID_LOGONRESPONSE2 result 0x{logonResult:X}). 0=ok, 1=bad user, 2=bad pass.");

        // --- SID_ENTERCHAT (0x0A) then SID_JOINCHANNEL (0x0C) ---
        await _stream.WriteAsync(new BncsPacketWriter(BncsPacketIds.SID_ENTERCHAT)
            .WriteCString("").WriteCString("").ToBytes(), ct);
        _ = await BncsPacketReader.ReadFromStreamAsync(_stream, ct); // SID_ENTERCHAT reply (unique name, statstring)

        await JoinChannelAsync(_channel, ct);

        _ = Task.Run(() => ReceiveLoopAsync(ct), ct);
        _ = Task.Run(() => KeepAliveLoopAsync(ct), ct);
    }

    public async Task JoinChannelAsync(string channel, CancellationToken ct)
    {
        var pkt = new BncsPacketWriter(BncsPacketIds.SID_JOINCHANNEL)
            .WriteUInt32(0x02) // "no create" join flag; use 0x01 first-join / 0x00 forced create as needed
            .WriteCString(channel)
            .ToBytes();
        await _stream!.WriteAsync(pkt, ct);
    }

    /// <summary>Send a chat line. Prefix with '/' to issue a BNCS slash command
    /// (e.g. "/kick name", "/ban name reason", "/whisper name text") — the
    /// server interprets these exactly like a real client would.</summary>
    public async Task SayAsync(string text, CancellationToken ct = default)
    {
        if (_stream is null) return;
        var pkt = new BncsPacketWriter(BncsPacketIds.SID_CHATCOMMAND)
            .WriteCString(text)
            .ToBytes();
        await _stream.WriteAsync(pkt, ct == default ? CancellationToken.None : ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var pkt = await BncsPacketReader.ReadFromStreamAsync(_stream!, ct);
                if (pkt is null) break; // disconnected
                Handle(pkt);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (IOException) { /* connection dropped */ }
        finally
        {
            OnDisconnected?.Invoke();
        }
    }

    private void Handle(BncsPacketReader pkt)
    {
        if (pkt.MsgId != BncsPacketIds.SID_CHATEVENT) return;

        var eventId = (ChatEventId)pkt.ReadUInt32();
        pkt.ReadUInt32(); // user flags
        pkt.ReadUInt32(); // ping
        pkt.ReadUInt32(); pkt.ReadUInt32(); pkt.ReadUInt32(); // IP, account #, reg auth — legacy/unused fields
        var username = pkt.ReadCString();
        var text = pkt.ReadCString();

        switch (eventId)
        {
            case ChatEventId.EID_TALK:
            case ChatEventId.EID_EMOTE:
            case ChatEventId.EID_WHISPER:
                OnChatMessage?.Invoke(new BncsChatMessage(username, text, eventId));
                break;
            case ChatEventId.EID_JOIN:
                OnUserJoined?.Invoke(username);
                break;
            case ChatEventId.EID_LEAVE:
                OnUserLeft?.Invoke(username);
                break;
            case ChatEventId.EID_INFO:
                OnServerInfo?.Invoke(text);
                break;
            case ChatEventId.EID_ERROR:
                OnServerError?.Invoke(text);
                break;
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
                var pkt = new BncsPacketWriter(BncsPacketIds.SID_NULL).ToBytes();
                await _stream!.WriteAsync(pkt, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }

    /// <summary>
    /// Classic Battle.net "broken SHA-1": same core compression function as
    /// real SHA-1 but with a documented word-scheduling bug, used only for
    /// this legacy password exchange. Do not use for anything else.
    /// </summary>
    private static byte[] BrokenSha1(byte[] data)
    {
        // Battle.net's broken-SHA1 matches standard SHA-1 output for the
        // message-padding/length cases used here; the divergence from real
        // SHA-1 is in bit rotation direction inside the compression function.
        // For a drop-in, use a verified broken-SHA1 implementation (BNCSutil
        // ships one) rather than System.Security.Cryptography.SHA1, which
        // implements the *correct* algorithm and will produce wrong hashes
        // the server will silently reject.
        throw new NotImplementedException(
            "Plug in a verified broken-SHA1 (BNCSutil's hashB/nls_ functions or a ported equivalent) here. " +
            "Standard SHA1.Create() will NOT interoperate with real Battle.net.");
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _stream?.Dispose();
        _tcp?.Dispose();
        await Task.CompletedTask;
    }
}
