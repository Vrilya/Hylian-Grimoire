using System.IO.Compression;

namespace HylianGrimoire.Compression;

public static class RawDeflateCodec
{
    public static byte[] Decode(ReadOnlySpan<byte> source, int expectedSize)
    {
        using var input = new MemoryStream(source.ToArray());
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(expectedSize > 0 ? expectedSize : 0);
        deflate.CopyTo(output);

        byte[] decoded = output.ToArray();
        if (expectedSize >= 0 && decoded.Length != expectedSize)
        {
            throw new InvalidDataException($"Raw deflate decoded {decoded.Length} bytes, expected {expectedSize}.");
        }

        return decoded;
    }

    public static byte[] Encode(ReadOnlySpan<byte> data, CompressionLevel level = CompressionLevel.SmallestSize) =>
        RawDeflateEncoder.Encode(data);
}
