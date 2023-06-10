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
        int rows = srcMat.Rows;
        int cols = srcMat.Cols;

        using var dstMat = new Mat();

        Cv2.Integral(srcMat, dstMat, MatType.CV_32SC1);

        dstMat.GetRectangularArray(out int[,] dstData);
        return dstData;
    }

    [Benchmark]
    public int[,] ByManagedCode()
    {
        int rows = srcData.GetLength(0);
        int cols = srcData.GetLength(1);
        if (rows == 0 || cols == 0)
            return new int[0, 0];

        int[,] dstData = new int[rows + 1, cols + 1];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0, sum = 0; x < cols; x++)
            {
                sum += srcData[y, x];
                dstData[y + 1, x + 1] = sum + dstData[y, x + 1];
            }
        }

        return dstData;
    }
}
