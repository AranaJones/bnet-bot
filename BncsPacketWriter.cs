using System.Text;

namespace BNetDiscordBridge.Bncs;

/// <summary>
/// Builds a single outgoing BNCS packet: [0xFF][msgId][u16 length incl. header][payload]
/// </summary>
public sealed class BncsPacketWriter
{
    private readonly byte _msgId;
    private readonly MemoryStream _body = new();

    public BncsPacketWriter(byte msgId) => _msgId = msgId;

    public BncsPacketWriter WriteByte(byte v) { _body.WriteByte(v); return this; }
    public BncsPacketWriter WriteUInt16(ushort v) { _body.Write(BitConverter.GetBytes(v)); return this; }
    public BncsPacketWriter WriteUInt32(uint v) { _body.Write(BitConverter.GetBytes(v)); return this; }

    /// <summary>Null-terminated ASCII string, as BNCS expects for most text fields.</summary>
    public BncsPacketWriter WriteCString(string s)
    {
        _body.Write(Encoding.ASCII.GetBytes(s));
        _body.WriteByte(0);
        return this;
    }

    public BncsPacketWriter WriteRaw(byte[] bytes) { _body.Write(bytes); return this; }

    public byte[] ToBytes()
    {
        var payload = _body.ToArray();
        ushort length = (ushort)(4 + payload.Length); // header (0xFF, msgId, u16 len) + payload
        using var full = new MemoryStream();
        full.WriteByte(0xFF);
        full.WriteByte(_msgId);
        full.Write(BitConverter.GetBytes(length));
        full.Write(payload);
        return full.ToArray();
    }
}
