namespace HylianGrimoire.Codecs;

internal readonly record struct EncodedMessageTableEntry(
    int Id,
    byte TypePosition,
    byte Reserved,
    int Bank,
    int Offset)
{
    public const int Size = 8;

    public int Type => (TypePosition >> 4) & 0x0f;

    public int Position => TypePosition & 0x0f;

    public static EncodedMessageTableEntry FromFields(
        int id,
        int type,
        int position,
        int bank,
        int offset)
    {
        return new EncodedMessageTableEntry(
            id & 0xffff,
            (byte)(((type & 0x0f) << 4) | (position & 0x0f)),
            Reserved: 0,
            bank & 0xff,
            offset & 0x00ffffff);
    }

    public static EncodedMessageTableEntry FromTypePosition(
        int id,
        byte typePosition,
        int bank,
        int offset)
    {
        return new EncodedMessageTableEntry(
            id & 0xffff,
            typePosition,
            Reserved: 0,
            bank & 0xff,
            offset & 0x00ffffff);
    }

    public static EncodedMessageTableEntry Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Size)
        {
            throw new ArgumentException("A message table entry requires 8 bytes.", nameof(bytes));
        }

        int id = (bytes[0] << 8) | bytes[1];
        int pointer = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
        return new EncodedMessageTableEntry(
            id,
            bytes[2],
            bytes[3],
            (pointer >> 24) & 0xff,
            pointer & 0x00ffffff);
    }

    public void WriteTo(List<byte> output)
    {
        output.Add((byte)((Id >> 8) & 0xff));
        output.Add((byte)(Id & 0xff));
        output.Add(TypePosition);
        output.Add(Reserved);
        output.Add((byte)(Bank & 0xff));
        output.Add((byte)((Offset >> 16) & 0xff));
        output.Add((byte)((Offset >> 8) & 0xff));
        output.Add((byte)(Offset & 0xff));
    }
}
