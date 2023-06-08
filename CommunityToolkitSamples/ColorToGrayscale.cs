using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Helpers;
using System.Diagnostics;

namespace CommunityToolkitSamples;

public class ColorToGrayscale
{
    private readonly Configuration customConfig;
    private readonly Image<Bgr24> srcImage;

    public ColorToGrayscale()
    {
        customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;
        srcImage = Image.Load<Bgr24>("mandrill_x10.png");
    }
    
    [Benchmark]
    public void BySingleThread()
    {
        using var dstImage = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);

        var srcSpan = srcMemory.Span;
        var dstSpan = dstMemory.Span;
        Debug.Assert(srcSpan.Length == dstSpan.Length);

        for (int i = 0, length = srcSpan.Length; i < length; i++)
        {
            var bgr = srcSpan[i];
            dstSpan[i] = new L8((byte)(bgr.R * 0.299 + bgr.G * 0.587 + bgr.B * 0.114));
        }
        
        //dstImage.SaveAsPng("grayscale_dst1.png");
    }

    [Benchmark]
    public void ByParallelHelper()
    {
        using var dstImage = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);

        using var srcHandle = srcMemory.Pin();
        using var dstHandle = dstMemory.Pin();
        unsafe
        {
            var action = new GrayscaleConverter((Bgr24*)srcHandle.Pointer, (L8*)dstHandle.Pointer);
            ParallelHelper.For(0, srcMemory.Length, action);
        }

        //dstImage.SaveAsPng("grayscale_dst2.png");
    }

    [Benchmark]
    public void ByImageSharp()
    {
        using var dstImage = srcImage.Clone();
        dstImage.Mutate(x =>
        {
            x.Grayscale(GrayscaleMode.Bt601);
        });
        //dstImage.SaveAsPng("grayscale_dst3.png");
    }
}

public unsafe struct GrayscaleConverter : IAction
{
    private readonly Bgr24 *srcSpan;
    private readonly L8 *dstSpan;

    public GrayscaleConverter(Bgr24 *srcSpan, L8 *dstSpan)
    {
        this.srcSpan = srcSpan;
        this.dstSpan = dstSpan;
    }

    public void Invoke(int i)
    {
        Bgr24 bgr = srcSpan[i];
        byte y = (byte)(bgr.R * 0.299 + bgr.G * 0.587 + bgr.B * 0.114);

        byte* mutDstSpan = (byte*)dstSpan;
        mutDstSpan[i] = y;
    }
}