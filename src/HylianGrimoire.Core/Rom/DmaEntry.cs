namespace HylianGrimoire.Rom;

public sealed record DmaEntry(
    int Index,
    uint VirtualStart,
    uint VirtualEnd,
    uint PhysicalStart,
    uint PhysicalEnd)
{
    public const uint DeletedMarker = 0xffffffff;

    public int VirtualSize => checked((int)(VirtualEnd - VirtualStart));

    public int PhysicalSize => IsCompressed ? checked((int)(PhysicalEnd - PhysicalStart)) : VirtualSize;

    public bool IsDeleted =>
        VirtualStart == DeletedMarker
        || VirtualEnd == DeletedMarker
        || PhysicalStart == DeletedMarker
        || PhysicalEnd == DeletedMarker;

    public bool IsEmpty => VirtualEnd <= VirtualStart;

    public bool IsCompressed => PhysicalEnd != 0 && PhysicalEnd != DeletedMarker;
}
