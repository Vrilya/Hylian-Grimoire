using HylianGrimoire.Rom;
using System.Buffers.Binary;
using System.IO.Compression;

namespace HylianGrimoire.Tweaks;

public static class ViSelectorTweak
{
    private const int RowSize = 0x30;

    private static readonly IReadOnlyDictionary<string, PatchProfile> Profiles = new Dictionary<string, PatchProfile>(StringComparer.Ordinal)
    {
        ["Retail PAL 1.0"] = new(
            "PAL 1.0",
            0x00B66C14,
            0x00B9D180,
            0x9C0,
            [0x00B9D180, 0x00B9DB40, 0x80800000, 0x808009C0, 0x00000000, 0x808007B0, 0x80800750, 0x00000000, 0x00000000, 0x00000000, 0x000001E8, 0x00000000],
            [0x00B9D180, 0x00B9DB40, 0x80800000, 0x808009C0, 0x00000000, 0x80800454, 0x808003F4, 0x00000000, 0x00000000, 0x00000000, 0x000001E8, 0x00000000],
            """
            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
            """,
            """
            eNrtVV1sFFUU/u6d2Xa2vbKbbTdMg5oxucMukYTWohacSLE740LAUsD4E2I0mhB/HkjwgQc116IJlm6njSS0CQ/zsDVIdjtEIcHE
            SjErIRGhD8SgaXShm9gH/kL6sCHFsXc7NRXlwXdOMrnnnvOd7zv3zplMajyY8U+jeXCATHMC0t8LJM/gEUsRpPboAvzN3ltWRIDH
            8KhFBEnpb3t8dDf31uzvTDUgi9ezpqsBKR3MXAVWTAezbktQLa4Lqq4WzC1vCKrFdDAnMRbZIDIfGyimAbcFanEd1DiwopIHLLZE
            qxGrpZbZ/w5qWo/v7zT7pNY+MzcAmP1g5k2w4o1gNpcLqsXrQTU3EMwl+4Jq8UYwJzEWEaJ4A8jloBavQy2tjOqVL4BCF/1EnsvV
            dz88jwFgmIWXaaWU3m1UsnS2dAxgINXKZlp1T6M5NQ5dKUMDgFR4X4cGYLRdjKGnjMihAzAGh8n0My0g3giZlvu26CB6DNTJemYA
            fh66m4fuCXL1PzhP+d+hVfFumf5prPWPY7WfR9a9gCyQNoFWM1eSyA6Tkbsr/BIs9yKyXEfAW0ByFcD/Bs3+KcQX8Yz03ZF49xws
            Ru6M5FQ6kRoPJlPHofvfI8OU9ucpDFM+TGl/zf0BGR5F4H+JuIzxCAJeh4ApbV/xegRhrJEpbRd4HbT5dyT4qHaOKe0f1XINCPxj
            /6idCmu3hbGHwlrm/ohsWH9p6D0yLfNWvRBJAcWqEyIOKPysNiV9flYrS43+PyJ77uFZ5v6ErJxZyTU0Rqb5qFaRmOQZqJJvCde1
            kOtWyPU+jyHw/b/7PcHrcCLs90QYqwt1kmGvs4tnvSe/fCEfVRfzTGmv9Rre8y7pu5eRcX+B5XfQCfd3rFUmZkx3Eqvd39Aq5yA1
            juzibDEDW+S8MANvuOHcWFQk+HP7JwcHyFWuIOAxkOQEovNzLLyuprLX1XSl0N20qyAwnNKfnuEtZLIwgnhhGOx+M1ybyTHoQxdJ
            uaeMBqnD6pOBxMQ7ociV0ZO8ttY3n5drDUOdFfxTMskQWVmL3YffWiYSQxdut1oxkeiZQJMnSHno5u02SxeJnjJicv+5fvuJlwAm
            fVafdGVP8U6oCzon+eJ3I+c8DlBvhJQl1oqKRG7VMLgGYjUIMZomcatRCHOz0m++q8xaTCQKfVALB6F5glwx+782hnL0qPxmLCIS
            bWO1+4vIc6yN/TklY5K/sOYwL3Qc5ow6ryzRbuYaAh4N7vIGWLwRJC/IVH6EXMofID/nPyOX833kV+8gueoJMn3vfSw1IbTjQmjf
            EjywB/bA/q852zZuMZ5szb4KYKv0n1rwsSljdxtbuzM2YO41zL0AnpV/dvx7j20bM+uNHfYWu2tnjXTNjp0bt+9cb3R1v+Bs2r5V
            hja8uOHDBcXIHkDpBLAnbOExG5hygGsZkLds0A4H1LJBBx3QYRv0iAPq2aD7HNAPbNADDuhRG3TKAa3YoDMO6DUbtOSAnrdBLzug
            s/b8b8qBkrahZBwo2QyUQxkoR2yoJQfqpA31vAP1Ui+AXkANe/H+Aghlyp0=
            """),
        ["Retail PAL 1.1"] = new(
            "PAL 1.1",
            0x00B66C54,
            0x00B9D1C0,
            0x9C0,
            [0x00B9D1C0, 0x00B9DB80, 0x80800000, 0x808009C0, 0x00000000, 0x808007B0, 0x80800750, 0x00000000, 0x00000000, 0x00000000, 0x000001E8, 0x00000000],
            [0x00B9D1C0, 0x00B9D690, 0x80800000, 0x808004D0, 0x00000000, 0x80800390, 0x80800368, 0x00000000, 0x00000000, 0x00000000, 0x000001E8, 0x00000000],
            """
            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
            """,
            """
            eNrtU89rHFUc/7w3s3HbvmaX/NAJiTKHeZ3VBtwlURMczNbsDovEkkKlVTxUDKRKEcWLF+GZVChldydKoZvbHBIJMsOs6MGDa7Ya
            RKG06U0x1DTZQw5tkkMOi0THvM0IsehfYD7weO/75fN9n++P98xauBEsomumTNYNAlKcArpv4HFLEaS1NAHjzaltKyZgJPCERQQx
            tbdcY37ScJ+ezppHUcCFAnfigKmB8SfB/FS44/SETX84bDrxcPexo2HTT4W7kmOREZH7SIefApweqP4w1CTQ25gDLHZA6xj6pRYv
            vo2W1snpLL8qtT7gpTLAi2B8C8zfDHdKpbDpPwibpXK42301bPqb4a7kWEQIfxMolaD6D6AunVDHG58B3ii9LOtytMm+PQ4AnXvn
            aWMpNak3CnRn6XOAgTQbL9Gms4guswZNWUUcAMyoX9fK0DO3Eziziti1K9BnKmT9+R4Qd5asSztzZAZndLTJeKYDwRw0Zw6aK8ja
            v9y5EHyLIcXd5sEirOALDAZVpIvTwMxUNJf3aN2shctdWcTMKjSrXQh+S9WTgBrcQsFKCMG3VCO4jULXKcSorvO9GoWpqakW52fk
            rB4hzGG1P/gFOUZ+nw2+x1lkr3CmDJyj0LlcTBl4h+opbsQRBgtISp8RQ2i0YYUpme+MRzAe+Y4zJXPXaAPbm5cw5tV0FF92fkDh
            IU57xMke4OSYMiCkHeVwSZ6dn3DWz9C68xsspb7BnWWknTsYdO5iSPbLrOG8RUWH8eLlpKEgNNpB3NHOVXe08553uvN1T6Cy34vn
            NgyNLHsVJD0H7ECvV4MqNAghe93HdIxJ3TIFigVatxK/tgEpztsR907Tug+ofgIoqzI6zRn5oxcY4kzHGzLuU42snQNUZ/8uv1XD
            IvqcKjSzBv3hd8Oo3Wt8TJYZYiek/7/el5UQHaWtSlbWJ+c4v0mSrflpdNscVi5acdHhVaB6s4i7gtzj8S8vfHKELhgMoUVER+Yp
            kO4sYlJr8NifK9KXBKh38vqE1399glH71b/fpHEcxC2RNVeQdZlP1GdN5vOC/B0Hdv6+bo+fGtOfSRdew779srSfjeyRV0Y+xD+g
            6tEhG+2P5oEVG7ifA5nIg3xtg3yTB/nRBrmZB1mxQVbzIPdtkO08aM4GLeRBL9qgl3JQkIMylIMylocyZ0Op5qF4NpSvALyLQxzi
            EIf4n+AvDzhQAA==
            """),
    };

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!Profiles.TryGetValue(profile.Name, out PatchProfile? patch))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "Unavailable");
        }

        if (!HasPatchRange(decompressedRom, patch))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        ReadOnlySpan<byte> row = decompressedRom.Slice(patch.RowOffset, RowSize);
        ReadOnlySpan<byte> block = decompressedRom.Slice(patch.BlockOffset, patch.BlockSize);
        bool rowOriginal = WordsEqual(row, patch.OriginalRowWords);
        bool rowPatched = WordsEqual(row, patch.PatchedRowWords);
        bool blockOriginal = block.SequenceEqual(patch.OriginalBlock);
        bool blockPatched = block.SequenceEqual(patch.PatchedBlock);

        if (rowOriginal && blockOriginal)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        if (rowPatched && blockPatched)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if ((rowOriginal || rowPatched) && (blockOriginal || blockPatched))
        {
            return new RomTweakStatus(RomTweakState.Mixed, "The VI selector is only partially applied.");
        }

        return new RomTweakStatus(RomTweakState.Unknown, "This ROM already has different startup overlay bytes.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!Profiles.TryGetValue(profile.Name, out PatchProfile? patch) || !HasPatchRange(decompressedRom, patch))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        if (enabled && status.State == RomTweakState.On || !enabled && status.State == RomTweakState.Off)
        {
            return;
        }

        Span<byte> row = decompressedRom.Slice(patch.RowOffset, RowSize);
        Span<byte> block = decompressedRom.Slice(patch.BlockOffset, patch.BlockSize);
        if (enabled)
        {
            WriteWords(row, patch.PatchedRowWords);
            patch.PatchedBlock.CopyTo(block);
            return;
        }

        WriteWords(row, patch.OriginalRowWords);
        patch.OriginalBlock.CopyTo(block);
    }

    private static bool HasPatchRange(ReadOnlySpan<byte> rom, PatchProfile patch) =>
        patch.RowOffset >= 0
        && patch.BlockOffset >= 0
        && patch.RowOffset + RowSize <= rom.Length
        && patch.BlockOffset + patch.BlockSize <= rom.Length;

    private static bool WordsEqual(ReadOnlySpan<byte> bytes, IReadOnlyList<uint> words)
    {
        if (bytes.Length != words.Count * sizeof(uint))
        {
            return false;
        }

        for (int i = 0; i < words.Count; i++)
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(i * sizeof(uint), sizeof(uint)));
            if (value != words[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteWords(Span<byte> destination, IReadOnlyList<uint> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(i * sizeof(uint), sizeof(uint)), words[i]);
        }
    }

    private static byte[] DecodeZlibBase64(string data)
    {
        byte[] packed = Convert.FromBase64String(string.Concat(data.Where(c => !char.IsWhiteSpace(c))));
        using var input = new MemoryStream(packed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private sealed class PatchProfile(
        string label,
        int rowOffset,
        int blockOffset,
        int blockSize,
        uint[] originalRowWords,
        uint[] patchedRowWords,
        string originalBlockBase64,
        string patchedBlockBase64)
    {
        private readonly Lazy<byte[]> _originalBlock = new(() => DecodeZlibBase64(originalBlockBase64));
        private readonly Lazy<byte[]> _patchedBlock = new(() => DecodeZlibBase64(patchedBlockBase64));

        public string Label { get; } = label;
        public int RowOffset { get; } = rowOffset;
        public int BlockOffset { get; } = blockOffset;
        public int BlockSize { get; } = blockSize;
        public uint[] OriginalRowWords { get; } = originalRowWords;
        public uint[] PatchedRowWords { get; } = patchedRowWords;
        public byte[] OriginalBlock => _originalBlock.Value;
        public byte[] PatchedBlock => _patchedBlock.Value;
    }
}
