using System.Drawing;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static Bitmap DecodeReference(ReadOnlySpan<byte> rom, TextTextureTargetItem item)
    {
        if (item.PauseHeaderTarget is { } pauseTarget)
        {
            using Bitmap left = TextureRomService.Decode(rom, pauseTarget.Left);
            using Bitmap middle = TextureRomService.Decode(rom, pauseTarget.Middle);
            using Bitmap right = TextureRomService.Decode(rom, pauseTarget.Right);
            return PauseHeaderTextureRenderer.CombineTriplet([left, middle, right]);
        }

        if (item.GameOverTarget is { } gameOverTarget)
        {
            if (gameOverTarget.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying)
            {
                return TextureRomService.Decode(rom, gameOverTarget.Texture);
            }

            using Bitmap part1 = TextureRomService.Decode(rom, gameOverTarget.Part1);
            using Bitmap part2 = TextureRomService.Decode(rom, gameOverTarget.Part2);
            using Bitmap part3 = TextureRomService.Decode(rom, gameOverTarget.Part3);
            return GameOverTextureRenderer.CombineTriplet([part1, part2, part3]);
        }

        return item.Texture is { } texture
            ? TextureRomService.Decode(rom, texture)
            : throw new InvalidOperationException("No texture target is selected.");
    }

    private static void ApplyPauseHeaderToRom(byte[] rom, PauseHeaderTextureTarget target, Bitmap row)
        => ApplyCompositeToRom(rom, target.Textures, row, PauseHeaderTextureRenderer.SplitTriplet);

    private static void ApplyGameOverToRom(byte[] rom, GameOverTextureTarget target, Bitmap row)
    {
        if (target.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying)
        {
            TextureRomService.EncodeAndWrite(rom, target.Texture, row);
            return;
        }

        ApplyCompositeToRom(rom, target.Textures, row, GameOverTextureRenderer.SplitTriplet);
    }

    private static void ApplyCompositeToRom(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        Bitmap row,
        Func<Bitmap, IReadOnlyList<Bitmap>> split)
    {
        byte[] romCopy = rom.ToArray();
        IReadOnlyList<Bitmap> images = split(row);
        try
        {
            if (images.Count != textures.Count)
            {
                throw new InvalidDataException("Generated texture part count does not match the selected ROM targets.");
            }

            for (int index = 0; index < images.Count; index++)
            {
                TextureRomService.EncodeAndWrite(romCopy, textures[index], images[index]);
            }

            romCopy.AsSpan().CopyTo(rom);
        }
        finally
        {
            foreach (Bitmap image in images)
            {
                image.Dispose();
            }
        }
    }
}
