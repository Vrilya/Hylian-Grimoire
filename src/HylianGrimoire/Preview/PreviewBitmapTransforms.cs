using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.Preview;

internal static class PreviewBitmapTransforms
{
    public static Bitmap ColorizeAlpha(Bitmap source, Color color)
    {
        return TransformPixels(source, (a, r, g, b) =>
            ((byte)(r * color.A / 255), color.R, color.G, color.B));
    }

    public static Bitmap ColorizeMultiply(Bitmap source, Color color)
    {
        return TransformPixels(source, (a, r, g, b) =>
            ((byte)(a * color.A / 255), (byte)(r * color.R / 255), (byte)(g * color.G / 255), (byte)(b * color.B / 255)));
    }

    public static Bitmap CreateTintedMask(Bitmap source, Color color, bool brighten)
    {
        return TransformPixels(source, (a, r, g, b) =>
            brighten
                ? (r, (byte)255, (byte)255, (byte)255)
                : (r, color.R, color.G, color.B));
    }

    public static Bitmap Scale(Bitmap source, float scale)
    {
        var scaled = new Bitmap((int)(source.Width * scale), (int)(source.Height * scale), PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(scaled);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = InterpolationMode.High;
        graphics.DrawImage(source, 0, 0, scaled.Width, scaled.Height);
        return scaled;
    }

    private static Bitmap TransformPixels(Bitmap source, Func<byte, byte, byte, byte, (byte A, byte R, byte G, byte B)> transform)
    {
        using Bitmap input = source.PixelFormat == PixelFormat.Format32bppArgb
            ? (Bitmap)source.Clone()
            : CloneAsArgb(source);

        var output = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, input.Width, input.Height);
        BitmapData inputData = input.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int inputStride = Math.Abs(inputData.Stride);
            int outputStride = Math.Abs(outputData.Stride);
            byte[] inputBytes = new byte[inputStride * input.Height];
            byte[] outputBytes = new byte[outputStride * output.Height];

            Marshal.Copy(inputData.Scan0, inputBytes, 0, inputBytes.Length);

            for (int y = 0; y < input.Height; y++)
            {
                int inputRow = GetRowOffset(inputData.Stride, inputStride, input.Height, y);
                int outputRow = GetRowOffset(outputData.Stride, outputStride, output.Height, y);
                for (int x = 0; x < input.Width; x++)
                {
                    int inputOffset = inputRow + x * 4;
                    byte b = inputBytes[inputOffset];
                    byte g = inputBytes[inputOffset + 1];
                    byte r = inputBytes[inputOffset + 2];
                    byte a = inputBytes[inputOffset + 3];
                    var pixel = transform(a, r, g, b);

                    int outputOffset = outputRow + x * 4;
                    outputBytes[outputOffset] = pixel.B;
                    outputBytes[outputOffset + 1] = pixel.G;
                    outputBytes[outputOffset + 2] = pixel.R;
                    outputBytes[outputOffset + 3] = pixel.A;
                }
            }

            Marshal.Copy(outputBytes, 0, outputData.Scan0, outputBytes.Length);
        }
        finally
        {
            input.UnlockBits(inputData);
            output.UnlockBits(outputData);
        }

        return output;
    }

    private static int GetRowOffset(int stride, int absoluteStride, int height, int y)
        => stride < 0 ? (height - 1 - y) * absoluteStride : y * absoluteStride;

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        return clone;
    }
}
