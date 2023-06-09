using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance;
using OpenCvSharp;

namespace CommunityToolkitSamples;

public class IntegralImage
{
    private readonly byte[,] srcData;

    public IntegralImage()
    {
        srcData = new byte[,]
        {
            {8,9,13,7,5,7 },
            {10,5,10,12,3,5 },
            {2,11,2,6,5,5},
            {12,10,6,1,10,1 },
            {7,3,6,5,5,13 }
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }

    [Benchmark]
    public int[,] ByOpenCV()
    {
        using var srcMat = Mat.FromArray(srcData);
        int[,] dstData = new int[srcMat.Rows, srcMat.Cols];
        using var dstMat = new Mat();

        Cv2.Integral(srcMat, dstMat);
        return dstData;
    }

    [Benchmark]
    public int[,] ByManagedCode()
    {
        int rows = srcData.GetLength(0);
        int cols = srcData.GetLength(1);
        if (rows == 0 || cols == 0)
            return new int[0, 0];

        int[,] dstData = new int[rows, cols];

        // 1行目
        for (int x = 0, sum = 0; x < cols; x++)
        {
            var s = srcData[0, x];
            dstData[0, x] = s + sum;
            sum += s;
        }

        // 2行目以降
        for (int y = 1; y < rows; y++)
        {
            for (int x = 0, sum = 0; x < cols; x++)
            {
                var s = srcData[y, x];
                dstData[0, x] = s + sum + srcData[y-1, x];
                sum += s;
            }
        }

        return dstData;
    }
}
