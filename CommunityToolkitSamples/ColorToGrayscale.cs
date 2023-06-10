using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using CommunityToolkit.HighPerformance.Helpers;
using System.Diagnostics;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Drawing.Processing;

namespace CommunityToolkitSamples;

[MemoryDiagnoser]
public class ColorToGrayscale
{
    private readonly Configuration customConfig;
    private readonly Image<Bgr24> srcImage;

    public ColorToGrayscale()
    {
        customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        // 連続領域を強制したい
        using var srcOrgImage = Image.Load<Bgr24>("mandrill_x10.png");
        srcImage = new Image<Bgr24>(customConfig, srcOrgImage.Width, srcOrgImage.Height);
        srcImage.Mutate(x =>
        {
            x.DrawImage(srcOrgImage, 1f);
        });
    }
    
    [Benchmark]
    public void BySingleThread()
    {
        using var dstImage = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);
        Debug.Assert(!srcMemory.IsEmpty);
        Debug.Assert(!dstMemory.IsEmpty);

        var srcSpan = srcMemory.Span;
        var dstSpan = dstMemory.Span;
        Debug.Assert(srcSpan.Length == dstSpan.Length);

        for (int i = 0, length = srcSpan.Length; i < length; i++)
        {
            var bgr = srcSpan[i];
            dstSpan[i] = new L8((byte)(bgr.R * 0.299 + bgr.G * 0.587 + bgr.B * 0.114));
        }
        
        dstImage.SaveAsPng("dst_grayscale_singlethread.png");
    }

    [Benchmark]
    public void ByParallelHelper()
    {
        using var dstImage = new Image<L8>(customConfig, srcImage.Width, srcImage.Height);
        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        dstImage.DangerousTryGetSinglePixelMemory(out var dstMemory);
        Debug.Assert(!srcMemory.IsEmpty);
        Debug.Assert(!dstMemory.IsEmpty);

        using var srcHandle = srcMemory.Pin();
        using var dstHandle = dstMemory.Pin();
        unsafe
        {
            var action = new GrayscaleConverter((Bgr24*)srcHandle.Pointer, (L8*)dstHandle.Pointer);
            ParallelHelper.For(0, srcMemory.Length, action);
        }

        dstImage.SaveAsPng("dst_grayscale_parallel.png");
    }

    [Benchmark]
    public void ByImageSharp()
    {
        using var dstImage = srcImage.Clone();
        dstImage.Mutate(x =>
        {
            x.Grayscale(GrayscaleMode.Bt601);
        });
        dstImage.SaveAsPng("dst_grayscale_imagesharp.png");
    }
}

public readonly unsafe struct GrayscaleConverter : IAction
{
    private readonly Bgr24 *srcPointer;
    private readonly L8 *dstPointer;

    public GrayscaleConverter(Bgr24 *srcPointer, L8 *dstPointer)
    {
        this.srcPointer = srcPointer;
        this.dstPointer = dstPointer;
    }

    public void Invoke(int i)
    {
        Bgr24 bgr = srcPointer[i];
        byte y = (byte)(bgr.R * 0.299 + bgr.G * 0.587 + bgr.B * 0.114);

        byte* mutDstSpan = (byte*)dstPointer;
        mutDstSpan[i] = y;
    }
}

