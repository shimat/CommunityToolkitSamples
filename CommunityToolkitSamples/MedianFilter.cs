using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance;
using OpenCvSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using Point = SixLabors.ImageSharp.Point;

namespace CommunityToolkitSamples;

public class MedianFilter
{
    private readonly Configuration customConfig;
    private readonly Image<L8> srcImage;
    private readonly Mat srcMat;

    public MedianFilter()
    {
        customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        var srcOrgImage = Image.Load<L8>("mandrill_gray.png");
        //srcImage.Save("mandrill_gray.png");
        srcImage = new Image<L8>(customConfig, srcOrgImage.Width + 2, srcOrgImage.Height + 2);
        srcImage.Mutate(x =>
        {
            x.Fill(Color.Black);
            x.DrawImage(
                srcOrgImage,
                new Point(1, 1), 
                1f);
        });
        //srcImage.SaveAsPng("src_imagesharp.png");

        srcMat = new Mat("mandrill_gray.png", ImreadModes.Grayscale);
        Cv2.CopyMakeBorder(srcMat, srcMat, 1, 1, 1, 1, BorderTypes.Constant, Scalar.Black);
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }

    [Benchmark]
    public void ByOpenCV()
    {
        using var dstMat = new Mat();
        Cv2.MedianBlur(srcMat, dstMat, 3);
        //dstMat.SaveImage("dst_median_opencv.png");
    }

    [Benchmark]
    public void ByUnsafe()
    {
        using var dstImage = srcImage.Clone(customConfig);
        var width = srcImage.Width;
        var height = srcImage.Height;

        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);

        using var srcHandle = srcMemory.Pin();
        using var dstHandle = dstMemory.Pin();
        unsafe
        {
            var srcPointer = (byte*)srcHandle.Pointer;
            var dstPointer = (byte*)dstHandle.Pointer;
            Span<byte> buffer = stackalloc byte[9];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    buffer[0] = srcPointer[(y - 1) * width + (x - 1)];
                    buffer[1] = srcPointer[(y - 1) * width + (x - 0)];
                    buffer[2] = srcPointer[(y - 1) * width + (x + 1)];
                    buffer[3] = srcPointer[(y - 0) * width + (x - 1)];
                    buffer[4] = srcPointer[(y - 0) * width + (x - 0)];
                    buffer[5] = srcPointer[(y - 0) * width + (x + 1)];
                    buffer[6] = srcPointer[(y + 1) * width + (x - 1)];
                    buffer[7] = srcPointer[(y + 1) * width + (x - 0)];
                    buffer[8] = srcPointer[(y + 1) * width + (x + 1)];

                    buffer.Sort();
                    dstPointer[y * width + x] = buffer[4];
                }
            }
        }

        //dstImage.SaveAsPng("dst_median_unsafe.png");
    }

    [Benchmark]
    public void ByMemory2d()
    {
        using var dstImage = srcImage.Clone(customConfig);
        var width = srcImage.Width;
        var height = srcImage.Height;

        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);
        var srcMemory2d = srcMemory.AsMemory2D(height, width);
        var dstMemory2d = dstMemory.AsMemory2D(height, width);
        var dstSpan2d = dstMemory2d.Span;

        Span<L8> buffer = stackalloc L8[9];
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                var m = srcMemory2d.Slice(y - 1, x - 1, 3, 3);
                m.Span.CopyTo(buffer);
                buffer.AsBytes().Sort();
                dstSpan2d[y, x] = buffer[4];
            }
        }

        //dstImage.SaveAsPng("dst_median_memory2d.png");
    }

    [Benchmark]
    public void ByImageSharp()
    {
        using var dstImage = srcImage.Clone();
        dstImage.Mutate(x =>
        {
            x.MedianBlur(1, false);
        });
        //dstImage.SaveAsPng("dst_median_imagesharp.png");
    }
}
