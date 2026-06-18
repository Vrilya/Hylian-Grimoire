namespace HylianGrimoire.Rom;

internal enum RomMessagePatchOperationKind
{
    SectionWrite,
    PointerTableWrite,
    UInt32Write,
    LuiAddiuAddressWrite,
}

internal sealed class RomMessagePatchPlan
{
    public RomMessagePatchPlan(
        int messageBankIndex,
        RomMessageSection section,
        MessageBankProfile bank,
        bool dropsFontOrderEntry,
        int tableByteCount,
        int messageByteCount,
        IReadOnlyList<RomMessagePatchOperation> operations)
    {
        MessageBankIndex = messageBankIndex;
        Section = section;
        Bank = bank;
        DropsFontOrderEntry = dropsFontOrderEntry;
        TableByteCount = tableByteCount;
        MessageByteCount = messageByteCount;
        Operations = operations.ToArray();
    }

    public int MessageBankIndex { get; }
    public RomMessageSection Section { get; }
    public MessageBankProfile Bank { get; }
    public bool DropsFontOrderEntry { get; }
    public int TableByteCount { get; }
    public int MessageByteCount { get; }
    public IReadOnlyList<RomMessagePatchOperation> Operations { get; }

    public void Apply(byte[] rom)
    {
        foreach (RomMessagePatchOperation operation in Operations)
        {
            operation.Apply(rom);
        }
    }
}

internal abstract class RomMessagePatchOperation
{
    protected RomMessagePatchOperation(RomMessagePatchOperationKind kind, string name, int offset, int length)
    {
        Kind = kind;
        Name = name;
        Offset = offset;
        Length = length;
    }

    public RomMessagePatchOperationKind Kind { get; }
    public string Name { get; }
    public int Offset { get; }
    public int Length { get; }

    public abstract void Apply(byte[] rom);

    public static RomMessageSectionWriteOperation WriteSection(
        int romLength,
        int offset,
        int capacity,
        byte[] payload,
        string name)
    {
        if (payload.Length > capacity)
        {
            throw new InvalidDataException(
                $"Encoded {name} is {payload.Length} bytes, but this ROM profile has room for {capacity} bytes.");
        }

        if (IsRangeOutside(romLength, offset, capacity))
        {
            throw new InvalidDataException($"ROM {name} section is outside the decompressed ROM buffer.");
        }

        return new RomMessageSectionWriteOperation(name, offset, capacity, payload);
    }

    public static RomMessagePointerTableWriteOperation WritePointerTable(
        int romLength,
        int pointerTableOffset,
        IReadOnlyList<int> messageOffsets)
    {
        int byteCount = checked((messageOffsets.Count + 1) * sizeof(uint));
        if (IsRangeOutside(romLength, pointerTableOffset, byteCount))
        {
            throw new InvalidDataException("ROM message pointer table is outside the decompressed ROM buffer.");
        }

        return new RomMessagePointerTableWriteOperation(pointerTableOffset, byteCount, messageOffsets);
    }

    public static RomMessageUInt32WriteOperation WriteUInt32(
        int romLength,
        int offset,
        uint value,
        string name)
    {
        const int length = sizeof(uint);
        if (IsRangeOutside(romLength, offset, length))
        {
            throw new InvalidDataException($"ROM {name} patch is outside the decompressed ROM buffer.");
        }

        return new RomMessageUInt32WriteOperation(name, offset, value);
    }

    public static RomMessageLuiAddiuAddressWriteOperation WriteLuiAddiuAddress(
        int romLength,
        int luiOffset,
        int addiuOffset,
        uint value,
        string name)
    {
        if (IsRangeOutside(romLength, luiOffset + 2, sizeof(ushort))
            || IsRangeOutside(romLength, addiuOffset + 2, sizeof(ushort)))
        {
            throw new InvalidDataException($"ROM {name} patch is outside the decompressed ROM buffer.");
        }

        return new RomMessageLuiAddiuAddressWriteOperation(name, luiOffset, addiuOffset, value);
    }

    private static bool IsRangeOutside(int romLength, int offset, int length) =>
        offset < 0 || length < 0 || offset > romLength - length;
}

internal sealed class RomMessageSectionWriteOperation : RomMessagePatchOperation
{
    private readonly byte[] _payload;

    public RomMessageSectionWriteOperation(string name, int offset, int capacity, byte[] payload)
        : base(RomMessagePatchOperationKind.SectionWrite, name, offset, capacity)
    {
        _payload = payload.ToArray();
    }

    public int PayloadLength => _payload.Length;
    public IReadOnlyList<byte> Payload => _payload;

    public override void Apply(byte[] rom)
    {
        rom.AsSpan(Offset, Length).Clear();
        _payload.CopyTo(rom.AsSpan(Offset, _payload.Length));
    }
}

internal sealed class RomMessagePointerTableWriteOperation : RomMessagePatchOperation
{
    private readonly int[] _messageOffsets;

    public RomMessagePointerTableWriteOperation(int pointerTableOffset, int byteCount, IReadOnlyList<int> messageOffsets)
        : base(RomMessagePatchOperationKind.PointerTableWrite, "message pointer table", pointerTableOffset, byteCount)
    {
        _messageOffsets = messageOffsets.ToArray();
    }

    public IReadOnlyList<int> MessageOffsets => _messageOffsets;

    public override void Apply(byte[] rom)
    {
        for (int i = 0; i < _messageOffsets.Length; i++)
        {
            DmaTable.WriteUInt32BigEndian(rom, Offset + (i * sizeof(uint)), 0x0700_0000u + (uint)_messageOffsets[i]);
        }

        DmaTable.WriteUInt32BigEndian(rom, Offset + (_messageOffsets.Length * sizeof(uint)), 0);
    }
}

internal sealed class RomMessageUInt32WriteOperation : RomMessagePatchOperation
{
    public RomMessageUInt32WriteOperation(string name, int offset, uint value)
        : base(RomMessagePatchOperationKind.UInt32Write, name, offset, sizeof(uint))
    {
        Value = value;
    }

    public uint Value { get; }

    public override void Apply(byte[] rom) =>
        DmaTable.WriteUInt32BigEndian(rom, Offset, Value);
}

internal sealed class RomMessageLuiAddiuAddressWriteOperation : RomMessagePatchOperation
{
    public RomMessageLuiAddiuAddressWriteOperation(string name, int luiOffset, int addiuOffset, uint value)
        : base(
            RomMessagePatchOperationKind.LuiAddiuAddressWrite,
            name,
            Math.Min(luiOffset, addiuOffset),
            checked((Math.Max(luiOffset, addiuOffset) + sizeof(uint)) - Math.Min(luiOffset, addiuOffset)))
    {
        LuiOffset = luiOffset;
        AddiuOffset = addiuOffset;
        Value = value;
    }

    public int LuiOffset { get; }
    public int AddiuOffset { get; }
    public uint Value { get; }

    public override void Apply(byte[] rom) =>
        WriteLuiAddiuAddress(rom, LuiOffset, AddiuOffset, Value);

    private static void WriteLuiAddiuAddress(byte[] data, int luiOffset, int addiuOffset, uint address)
    {
        ushort lo = (ushort)(address & 0xffff);
        ushort hi = (ushort)((address >> 16) & 0xffff);
        if (lo >= 0x8000)
        {
            hi++;
        }

        WriteUInt16BigEndian(data, luiOffset + 2, hi);
        WriteUInt16BigEndian(data, addiuOffset + 2, lo);
    }

    private static void WriteUInt16BigEndian(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }
}
