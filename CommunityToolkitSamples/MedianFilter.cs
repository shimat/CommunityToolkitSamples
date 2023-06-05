using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Helpers;
using SixLabors.ImageSharp.Drawing.Processing;

namespace CommunityToolkitSamples;

public class MedianFilter
{
    private readonly Configuration customConfig;
    private readonly Image<L8> imageWithBorder;
    private readonly Image<L8> dstImage1;
    private Image<L8> dstImage2;

    public MedianFilter()
    {
        customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        var image = Image.Load<L8>("mandrill.png");
        imageWithBorder = new Image<L8>(customConfig, image.Width + 2, image.Height + 2);
        imageWithBorder.Mutate(x =>
        {
            x.Fill(Color.Black);
            x.DrawImage(
                image, 
                new Rectangle(1, 1, image.Width, image.Height), 
                1f);
        });
        imageWithBorder.SaveAsPng("dst_org.png");
        
        dstImage1 = imageWithBorder.Clone(customConfig);
        dstImage2 = imageWithBorder.Clone(customConfig);
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }
    
    [Benchmark]
    public void Memory2d()
    {
        unsafe
        {
            var width = imageWithBorder.Width;
            var height = imageWithBorder.Height;
            imageWithBorder.DangerousTryGetSinglePixelMemory(out var srcMemory);
            dstImage1.DangerousTryGetSinglePixelMemory(out var dstMemory);
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
    }

    [Benchmark]
    public void ImageSharp()
    {
        dstImage2 = imageWithBorder.Clone(customConfig);
        dstImage2.Mutate(x =>
        {
            x.MedianBlur(1, false);
        });
        //dstImage2.SaveAsPng("dst2.png");
    }
}
