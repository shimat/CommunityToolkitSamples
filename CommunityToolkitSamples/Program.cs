using BenchmarkDotNet.Running;
using CommunityToolkitSamples;

/*
var f = new MedianFilter();
f.IterationSetup();
f.ByOpenCV();
f.ByUnsafe();
f.ByMemory2d();
f.ByImageSharp();
//*/

//BenchmarkRunner.Run<MedianFilter>();



/*
var f = new ColorToGrayscale();
f.BySingleThread();
f.ByParallelHelper();
f.ByImageSharp();
//*/

BenchmarkRunner.Run<ColorToGrayscale>();
