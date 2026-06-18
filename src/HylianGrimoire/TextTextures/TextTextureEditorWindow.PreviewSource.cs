using System.Security.Cryptography;
using System.Text;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private sealed record TextTexturePreviewSourceSignature(string Value);

    private static TextTexturePreviewSourceSignature CreatePreviewSourceSignature(ReadOnlySpan<byte> rom, TextTextureTargetItem item)
    {
        StringBuilder builder = new(item.TargetKey);
        foreach (TextureDefinition texture in GetTargetTextures(item))
        {
            builder.Append('|');
            builder.Append(texture.Name);
            builder.Append(':');
            builder.Append(CreateTextureDataHash(TextureRomService.ReadRaw(rom, texture)));

            byte[] tlut = TextureRomService.ReadTlutRaw(rom, texture);
            if (tlut.Length > 0)
            {
                builder.Append(':');
                builder.Append(CreateTextureDataHash(tlut));
            }
        }

        return new TextTexturePreviewSourceSignature(builder.ToString());
    }

    private bool IsLastGeneratedCurrent(ReadOnlySpan<byte> rom, TextTextureTargetItem item)
        => _lastGeneratedSourceSignature is not null
            && _lastGeneratedSourceSignature == CreatePreviewSourceSignature(rom, item);

    private static IReadOnlyList<TextureDefinition> GetTargetTextures(TextTextureTargetItem item)
    {
        if (item.PauseHeaderTarget is { } pauseHeaderTarget)
        {
            return pauseHeaderTarget.Textures;
        }

        if (item.GameOverTarget is { } gameOverTarget)
        {
            return gameOverTarget.Textures;
        }

        if (item.Texture is { } texture)
        {
            return [texture];
        }

        throw new InvalidOperationException("No texture target is selected.");
    }

    private static string CreateTextureDataHash(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data));
}
