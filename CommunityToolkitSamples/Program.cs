using BenchmarkDotNet.Running;
using CommunityToolkitSamples;
using OpenCvSharp;

/*
var f = new MedianFilter();
f.IterationSetup();
f.Memory2d();
f.ImageSharp();
//*/

/*
using var src = new Mat("mandrill_gray.png", ImreadModes.Grayscale);
using var dst = new Mat();
Cv2.CopyMakeBorder(src, dst, 1, 1, 1, 1, BorderTypes.Constant, Scalar.Black);
dst.SaveImage("src_opencv.png");
Cv2.MedianBlur(dst, dst, 3);
dst.SaveImage("dst_opencv.png");
*/

//BenchmarkRunner.Run<MedianFilter>();



/*
var f = new ColorToGrayscale();
f.BySingleThread();
f.ByParallelHelper();
f.ByImageSharp();
//*/

BenchmarkRunner.Run<ColorToGrayscale>();
