using HylianGrimoire.Games;

namespace HylianGrimoire.Services;

public enum MessageByteSegmentKind
{
    TableField,
    HeaderField,
    Text,
    RawByte,
    LineBreak,
    ControlCode,
    Parameter,
    Terminator,
    Padding,
}

public enum MessageByteSectionKind
{
    MessageTableEntry,
    MessageHeader,
    MessageData,
}

public sealed record MessageByteInspection(
    GameKind GameKind,
    int MessageId,
    IReadOnlyList<byte> Bytes,
    IReadOnlyList<MessageByteSegment> Segments,
    IReadOnlyList<MessageByteSection> Sections);

public sealed record MessageByteSection(
    MessageByteSectionKind Kind,
    string Title,
    IReadOnlyList<byte> Bytes,
    IReadOnlyList<MessageByteSegment> Segments);

public sealed record MessageByteSegment(
    int Offset,
    int Length,
    MessageByteSegmentKind Kind,
    string Label,
    string Description,
    string? EditorSyntax)
{
    public bool Contains(int offset)
        => offset >= Offset && offset < Offset + Length;
}
