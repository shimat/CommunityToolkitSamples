using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance;
using SixLabors.ImageSharp.Drawing.Processing;

namespace CommunityToolkitSamples;

public class MedianFilter
{
    private readonly Image<L8> srcImage;
    private readonly Image<L8> dstImage1;
    private readonly Image<L8> dstImage2;

    public MedianFilter()
    {
        var customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        var srcOrgImage = Image.Load<L8>("mandrill.png");
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
        srcImage.SaveAsPng("src_imagesharp.png");
        
        dstImage1 = srcImage.Clone(customConfig);
        dstImage2 = srcImage.Clone(customConfig);
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }

    [Benchmark]
    public void Memory2d()
    {
        var width = srcImage.Width;
        var height = srcImage.Height;
        if (!srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory))
            throw new NotSupportedException("Failed to get pixel memory.");
        if (!dstImage1.DangerousTryGetSinglePixelMemory(out var dstMemory))
            throw new NotSupportedException("Failed to get pixel memory.");
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

        //dstImage1.SaveAsPng("dst1.png");
    }

    [Benchmark]
    public void ImageSharp()
    {
        //dstImage2 = imageWithBorder.Clone(customConfig);
        dstImage2.Mutate(x =>
        {
            x.MedianBlur(1, false);
        });
        //dstImage2.SaveAsPng("dst2.png");
    }
}
