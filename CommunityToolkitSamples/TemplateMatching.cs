using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance;
using OpenCvSharp;
using System.Diagnostics;

namespace CommunityToolkitSamples;

[MemoryDiagnoser]
public class TemplateMatching
{
    private readonly Image<L8> srcImage;
    private readonly Image<L8> tmplImage;
    private readonly Mat srcMat;
    private readonly Mat tmplMat;

    public TemplateMatching()
    {
        var customConfig = Configuration.Default.Clone();
        customConfig.PreferContiguousImageBuffers = true;

        srcImage = Image.Load<L8>("mandrill_gray.png");
        tmplImage = Image.Load<L8>("mandrill_gray_template.png");

        srcMat = new Mat("mandrill_gray.png");
        tmplMat = new Mat("mandrill_gray_template.png");
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }

    [Benchmark]
    public float[,] ByOpenCV()
    {
        using var dstMat = new Mat();
        Cv2.MatchTemplate(srcMat, tmplMat, dstMat, TemplateMatchModes.SqDiffNormed);

        dstMat.GetRectangularArray(out float[,] dstData);
        return dstData;
    }

    [Benchmark]
    public float[,] ByMemory2D()
    {
        var w = srcImage.Width;
        var h = srcImage.Height;
        var tw = tmplImage.Width;
        var th = tmplImage.Height;

        srcImage.DangerousTryGetSinglePixelMemory(out var srcMemory);
        tmplImage.DangerousTryGetSinglePixelMemory(out var tmplMemory);
        Debug.Assert(!srcMemory.IsEmpty);
        Debug.Assert(!tmplMemory.IsEmpty);

        var srcMemory2d = srcMemory.AsBytes().AsMemory2D(h, w);
        var tmplMemory2d = tmplMemory.AsBytes().AsMemory2D(th, tw);
        var tmplSpan2d = tmplMemory2d.Span;

        var ylim = h - th + 1;
        var xlim = w - tw + 1;
        var dstData = new float[ylim, xlim];

        for (int y = 0; y < ylim; y++)
        {
            for (int x = 0; x < xlim; x++)
            {
                var srcSlice = srcMemory2d.Slice(y, x, th, tw);
                var srcSpan2d = srcSlice.Span;

                int diffSqSum = 0;
                long srcSqSum = 0;
                long tmplSqSum = 0;
                for (int ty = 0; ty < th; ty++)
                {
                    for (int tx = 0; tx < tw; tx++)
                    {
                        var srcVal = srcSpan2d[ty, tx];
                        var tmplVal = tmplSpan2d[ty, tx];

                        diffSqSum += (tmplVal - srcVal) * (tmplVal - srcVal);
                        srcSqSum += srcVal * srcVal;
                        tmplSqSum += tmplVal * tmplVal;
                    }
                }

                var numerator = Math.Sqrt(srcSqSum * tmplSqSum);
                dstData[y, x] = (float)(diffSqSum / numerator);
            }
        }

        return dstData;
    }
}
