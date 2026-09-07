using System.Text;

namespace BNetDiscordBridge.Bncs;

/// <summary>
/// A single fully-buffered incoming BNCS packet, with cursor-based readers
/// for pulling fields out in order (mirrors how you write them on the way in).
/// </summary>
public sealed class BncsPacketReader
{
    public byte MsgId { get; }
    private readonly byte[] _payload;
    private int _pos;

    public BncsPacketReader(byte msgId, byte[] payload)
    {
        MsgId = msgId;
        _payload = payload;
    }

    public byte ReadByte() => _payload[_pos++];

    public ushort ReadUInt16()
    {
        var v = BitConverter.ToUInt16(_payload, _pos);
        _pos += 2;
        return v;
    }

    public uint ReadUInt32()
    {
        var v = BitConverter.ToUInt32(_payload, _pos);
        _pos += 4;
        return v;
    }

    public string ReadCString()
    {
        int start = _pos;
        while (_pos < _payload.Length && _payload[_pos] != 0) _pos++;
        var s = Encoding.ASCII.GetString(_payload, start, _pos - start);
        _pos++; // skip null terminator
        return s;
    }

    public byte[] ReadRemaining() => _payload[_pos..];

    /// <summary>
    /// Reads one full packet (header + payload) synchronously from a stream.
    /// Returns null on clean disconnect.
    /// </summary>
    public static async Task<BncsPacketReader?> ReadFromStreamAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[4];
        if (!await ReadExactAsync(stream, header, ct)) return null;

        if (header[0] != 0xFF)
            throw new InvalidDataException($"Expected BNCS packet start 0xFF, got 0x{header[0]:X2} — stream is desynced.");

        byte msgId = header[1];
        ushort length = BitConverter.ToUInt16(header, 2);
        int payloadLen = length - 4;
        var payload = new byte[Math.Max(0, payloadLen)];
        if (payloadLen > 0 && !await ReadExactAsync(stream, payload, ct)) return null;

        return new BncsPacketReader(msgId, payload);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (read == 0) return false; // disconnected
            offset += read;
        }
        return true;
    }
}
