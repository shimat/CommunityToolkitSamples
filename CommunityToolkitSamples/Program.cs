using BenchmarkDotNet.Running;
using CommunityToolkitSamples;

/*var f = new MedianFilter();
f.IterationSetup();
f.Memory2d();
f.ImageSharp();*/

BenchmarkRunner.Run<MedianFilter>();


/*

var m = new Memory2D<byte>();
using var h = m.Pin();


var array = new float[10000];
ParallelHelper.ForEach<float, ByTwoMultiplier>(array);
array.ToString();

public readonly struct ByTwoMultiplier : IRefAction<float>
{
    public void Invoke(ref float x) => x *= 2;
}
*/
