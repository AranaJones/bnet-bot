namespace BNetDiscordBridge.Bncs;

/// <summary>
/// BNLS (Battle.net Login Server) client for handling authentication.
/// BNLS is a separate server that handles the complex authentication
/// (CheckRevision, CD key hashing, broken SHA-1) so bots don't need
/// to implement game-specific code or reverse-engineer the algorithms.
/// </summary>
public sealed class BnlsClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private System.Net.Sockets.TcpClient? _tcp;
    private System.Net.Sockets.NetworkStream? _stream;

    // BNLS packet IDs
    private const byte BNLS_NULL = 0x00;
    private const byte BNLS_CDKEY = 0x01;
    private const byte BNLS_LOGONCHALLENGE = 0x02;
    private const byte BNLS_LOGONRESPONSE = 0x03;
    private const byte BNLS_CREATEACCOUNT = 0x04;
    private const byte BNLS_CHANGEEMAIL = 0x05;
    private const byte BNLS_CHANGEFORGOTTEN = 0x06;
    private const byte BNLS_UPGRADEEMAIL = 0x07;
    private const byte BNLS_QUERYREALMS = 0x08;
    private const byte BNLS_QUERYREALMS2 = 0x09;
    private const byte BNLS_QUERYGAMES = 0x0A;
    private const byte BNLS_QUERYGAMES2 = 0x0B;
    private const byte BNLS_GETFILETIME = 0x0C;
    private const byte BNLS_VERSIONCHECK = 0x0D;
    private const byte BNLS_AUTHORIZED = 0x0E;
    private const byte BNLS_GAMERESULT = 0x0F;

    public BnlsClient(string host = "localhost", int port = 9367)
    {
        _host = host;
        _port = port;
    }

    /// <summary>
    /// Connect to BNLS server.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _tcp = new System.Net.Sockets.TcpClient();
        await _tcp.ConnectAsync(_host, _port, ct);
        _stream = _tcp.GetStream();
        Console.WriteLine($"Connected to BNLS at {_host}:{_port}");
    }

    /// <summary>
    /// Hash a CD key using BNLS.
    /// </summary>
    public async Task<(uint keyLength, uint product, uint publicValue, byte[] hash)> HashCdKeyAsync(
        string cdKey, uint clientToken, uint serverToken, string productId, CancellationToken ct = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to BNLS");

        var request = new BncsPacketWriter(BNLS_CDKEY)
            .WriteUInt32(clientToken)
            .WriteUInt32(serverToken)
            .WriteCString(productId)
            .WriteCString(cdKey)
            .ToBytes();

        await _stream.WriteAsync(request, ct);

        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("BNLS disconnected during CD key hash");

        var status = response.ReadUInt32();
        if (status != 0)
            throw new InvalidOperationException($"BNLS CD key hash failed (status {status})");

        var keyLength = response.ReadUInt32();
        var product = response.ReadUInt32();
        var publicValue = response.ReadUInt32();
        var hash = response.ReadRemaining();

        return (keyLength, product, publicValue, hash);
    }

    /// <summary>
    /// Perform version check using BNLS.
    /// </summary>
    public async Task<(uint exeVersion, uint exeHash, string exeInfo)> VersionCheckAsync(
        string productId, string fileName, uint fileTime, string formula, uint clientToken, uint serverToken, CancellationToken ct = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to BNLS");

        var request = new BncsPacketWriter(BNLS_VERSIONCHECK)
            .WriteCString(productId)
            .WriteCString(fileName)
            .WriteUInt32(fileTime)
            .WriteUInt32((uint)formula.Length)
            .WriteCString(formula)
            .WriteUInt32(clientToken)
            .WriteUInt32(serverToken)
            .ToBytes();

        await _stream.WriteAsync(request, ct);

        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("BNLS disconnected during version check");

        var status = response.ReadUInt32();
        if (status != 0)
            throw new InvalidOperationException($"BNLS version check failed (status {status})");

        var exeVersion = response.ReadUInt32();
        var exeHash = response.ReadUInt32();
        var exeInfo = response.ReadCString();

        return (exeVersion, exeHash, exeInfo);
    }

    /// <summary>
    /// Get broken SHA-1 hash from BNLS.
    /// </summary>
    public async Task<byte[]> GetBrokenSha1Async(byte[] data, CancellationToken ct = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to BNLS");

        var request = new BncsPacketWriter(0x14) // custom BNLS command for SHA1
            .WriteUInt32((uint)data.Length)
            .WriteRaw(data)
            .ToBytes();

        await _stream.WriteAsync(request, ct);

        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)
            ?? throw new IOException("BNLS disconnected during SHA1 hash");

        return response.ReadRemaining();
    }

    public async ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _tcp?.Dispose();
        await Task.CompletedTask;
    }
}