using BenchmarkDotNet.Running;
using CommunityToolkit.HighPerformance;
using CommunityToolkitSamples;
using OpenCvSharp;

/*
var f = new MedianFilter();
f.IterationSetup();
f.ByOpenCV();
f.ByUnsafe();
f.ByMemory2d();
f.ByImageSharp();
//*/

//BenchmarkRunner.Run<MedianFilter>();

//////////////////////////////////////////

/*
var f = new ColorToGrayscale();
f.BySingleThread();
f.ByParallelHelper();
f.ByImageSharp();
//*/

//BenchmarkRunner.Run<ColorToGrayscale>();

//////////////////////////////////////////

/*
var f = new IntegralImage();
var r1 = f.ByOpenCV();
var r2 = f.ByManagedCode();
Show2dArray(r1);
Console.WriteLine("-----");
Show2dArray(r2);
f.ToString();
//*/

//////////////////////////////////////////
///*
var f = new TemplateMatching();
var r1 = f.ByOpenCV();
var r2 = f.ByMemory2d();
var r3 = f.ByMemory2dParallel();
OutputFloatImage(r1, "dst_tm_opencv.png");
OutputFloatImage(r2, "dst_tm_memory2d.png");
OutputFloatImage(r2, "dst_tm_memory2d_parallel.png");
//ShowFloatImages(r1, r2, r3);
//*/

//BenchmarkRunner.Run<TemplateMatching>();


void Show2dArray<T>(T[,] array)
{
    int rows = array.GetLength(0);
    for (int r = 0; r < rows; r++)
    {
        Console.WriteLine(string.Join(",", array.GetRow(r).ToArray()));
    }
}

void OutputFloatImage(float[,] data, string outputFileName)
{
    using var mat = Mat.FromArray(data);
    mat.ConvertTo(mat, MatType.CV_8UC1, 255);
    Cv2.ApplyColorMap(mat, mat, ColormapTypes.Hot);
    mat.SaveImage(outputFileName);
}

void ShowFloatImages(params float[][,] data)
{
    var mats = data.Select(x => Mat.FromArray(x)).ToArray();
    Window.ShowImages(mats);
    foreach (var mat in mats)
    {
        mat.Dispose();
    }
}