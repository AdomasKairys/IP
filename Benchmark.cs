using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace IP
{
    [MemoryDiagnoser]
    public class Benchmark
    {
        [Params(2, 4, 6, 8, 10, 12)]
        public int ThredCount { get; set; }

        public (double, double)[][] AllGivPoints { get; set; }
        public (double, double)[][] AllNewPoints { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            for (int i = 0; i <= 8; i++)
            {
                using (var readerx = new StreamReader($"../../../../../../../Data/x{i}.txt"))
                using (var readery = new StreamReader($"../../../../../../../Data/y{i}.txt"))
                using (var readerxx = new StreamReader($"../../../../../../../Data/xx{i}.txt"))
                using (var readeryy = new StreamReader($"../../../../../../../Data/yy{i}.txt"))
                {
                    AllGivPoints[i] = readerx.ReadToEnd().Split("\n").Zip(readery.ReadToEnd().Split("\n"), (x, y) => (double.Parse(x.Replace(',','.')), double.Parse(y.Replace(',', '.')))).ToArray();
                    AllNewPoints[i] = readerxx.ReadToEnd().Split("\n").Zip(readeryy.ReadToEnd().Split("\n"), (x, y) => (double.Parse(x.Replace(',', '.')), double.Parse(y.Replace(',', '.')))).ToArray();
                }
            }
        }

        [Benchmark]
        public string Test1()
        {
            return Optimization.Optimize(AllGivPoints[0], AllNewPoints[0], ThredCount);
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();
        }
    }
}
