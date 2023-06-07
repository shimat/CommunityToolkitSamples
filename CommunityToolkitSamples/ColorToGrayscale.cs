using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkitSamples;

public class ColorToGrayscale
{
    private readonly Configuration customConfig;
    private readonly Image<Bgr24> srcImage;
    private readonly Image<L8> dstImage1;
    private readonly Image<L8> dstImage2;
    private Image dstImage3;

    public ColorToGrayscale()
    {
        customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        srcImage = Image.Load<Bgr24>("mandrill.png");
        dstImage1 = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        dstImage2 = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        dstImage3 = srcImage.Clone();
    }
    
    [Benchmark]
    public void BySingleThread()
    {
        if (!srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory))
            throw new NotSupportedException("Failed to get pixel memory.");
        if (!dstImage1.DangerousTryGetSinglePixelMemory(out var dstMemory))
            throw new NotSupportedException("Failed to get pixel memory.");

        var srcSpan = srcMemory.Span;
        var dstSpan = dstMemory.Span;
        if (srcSpan.Length != dstSpan.Length)
            throw new NotSupportedException($"{nameof(srcSpan)}.Length != {nameof(dstSpan)}.Length");

        for (int i = 0, length = srcSpan.Length; i < length; i++)
        {
            var bgr = srcSpan[i];
            dstSpan[i] = new L8((byte)(bgr.R * 0.299 + bgr.G * 0.587 + bgr.B * 0.114));
        }
        
        dstImage1.SaveAsPng("grayscale_dst1.png");
    }

    [Benchmark]
    public void ByParallelHelper()
    {
        if (!srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory))
            throw new NotSupportedException("Failed to get pixel memory.");
        if (!dstImage2.DangerousTryGetSinglePixelMemory(out var dstMemory))
            throw new NotSupportedException("Failed to get pixel memory.");

        using var srcHandle = srcMemory.Pin();
        using var dstHandle = dstMemory.Pin();
        //var srcSpan = srcMemory.Span;
        unsafe
        {
            var action = new GrayscaleConverter((Bgr24*)srcHandle.Pointer, (L8*)dstHandle.Pointer);
            ParallelHelper.For(0, srcMemory.Length, action);
        }

        dstImage2.SaveAsPng("grayscale_dst2.png");
    }

    [Benchmark]
    public void ByImageSharp()
    {
        dstImage3 = srcImage.Clone(customConfig);
        dstImage3.Mutate(x =>
        {
            x.Grayscale(GrayscaleMode.Bt601);
        });
        dstImage3.SaveAsPng("grayscale_dst3.png");
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

        L8* mutDstSpan = dstSpan;
        mutDstSpan[i] = new L8(y);
    }
}