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

    public BnlsClient(string host = \"localhost\", int port = 9367)\n    {\n        _host = host;\n        _port = port;\n    }\n\n    /// <summary>\n    /// Connect to BNLS server.\n    /// </summary>\n    public async Task ConnectAsync(CancellationToken ct = default)\n    {\n        _tcp = new System.Net.Sockets.TcpClient();\n        await _tcp.ConnectAsync(_host, _port, ct);\n        _stream = _tcp.GetStream();\n        Console.WriteLine($\"Connected to BNLS at {_host}:{_port}\");\n    }\n\n    /// <summary>\n    /// Hash a CD key using BNLS.\n    /// </summary>\n    public async Task<(uint keyLength, uint product, uint publicValue, byte[] hash)> HashCdKeyAsync(\n        string cdKey, uint clientToken, uint serverToken, string productId, CancellationToken ct = default)\n    {\n        if (_stream == null)\n            throw new InvalidOperationException(\"Not connected to BNLS\");\n\n        var request = new BncsPacketWriter(BNLS_CDKEY)\n            .WriteUInt32(clientToken)\n            .WriteUInt32(serverToken)\n            .WriteCString(productId)\n            .WriteCString(cdKey)\n            .ToBytes();\n\n        await _stream.WriteAsync(request, ct);\n\n        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)\n            ?? throw new IOException(\"BNLS disconnected during CD key hash\");\n\n        var status = response.ReadUInt32();\n        if (status != 0)\n            throw new InvalidOperationException($\"BNLS CD key hash failed (status {status})\");\n\n        var keyLength = response.ReadUInt32();\n        var product = response.ReadUInt32();\n        var publicValue = response.ReadUInt32();\n        var hash = response.ReadRemaining();\n\n        return (keyLength, product, publicValue, hash);\n    }\n\n    /// <summary>\n    /// Perform version check using BNLS.\n    /// </summary>\n    public async Task<(uint exeVersion, uint exeHash, string exeInfo)> VersionCheckAsync(\n        string productId, string fileName, uint fileTime, string formula, uint clientToken, uint serverToken, CancellationToken ct = default)\n    {\n        if (_stream == null)\n            throw new InvalidOperationException(\"Not connected to BNLS\");\n\n        var request = new BncsPacketWriter(BNLS_VERSIONCHECK)\n            .WriteCString(productId)\n            .WriteCString(fileName)\n            .WriteUInt32(fileTime)\n            .WriteUInt32((uint)formula.Length)\n            .WriteCString(formula)\n            .WriteUInt32(clientToken)\n            .WriteUInt32(serverToken)\n            .ToBytes();\n\n        await _stream.WriteAsync(request, ct);\n\n        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)\n            ?? throw new IOException(\"BNLS disconnected during version check\");\n\n        var status = response.ReadUInt32();\n        if (status != 0)\n            throw new InvalidOperationException($\"BNLS version check failed (status {status})\");\n\n        var exeVersion = response.ReadUInt32();\n        var exeHash = response.ReadUInt32();\n        var exeInfo = response.ReadCString();\n\n        return (exeVersion, exeHash, exeInfo);\n    }\n\n    /// <summary>\n    /// Get broken SHA-1 hash from BNLS.\n    /// </summary>\n    public async Task<byte[]> GetBrokenSha1Async(byte[] data, CancellationToken ct = default)\n    {\n        if (_stream == null)\n            throw new InvalidOperationException(\"Not connected to BNLS\");\n\n        var request = new BncsPacketWriter(0x14) // custom BNLS command for SHA1\n            .WriteUInt32((uint)data.Length)\n            .WriteRaw(data)\n            .ToBytes();\n\n        await _stream.WriteAsync(request, ct);\n\n        var response = await BncsPacketReader.ReadFromStreamAsync(_stream, ct)\n            ?? throw new IOException(\"BNLS disconnected during SHA1 hash\");\n\n        return response.ReadRemaining();\n    }\n\n    public async ValueTask DisposeAsync()\n    {\n        _stream?.Dispose();\n        _tcp?.Dispose();\n        await Task.CompletedTask;\n    }\n}\n