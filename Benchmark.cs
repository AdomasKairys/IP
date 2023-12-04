using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using System;
using System.Globalization;
using System.IO;

namespace IP
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount:2, iterationCount: 4)]
    public class Benchmark
    {
        [Params(1, 2, 4, 8)]
        public int CalulationThreads { get; set; }

        [Params(1, 2, 4, 8)]
        public int OptimizationThreads { get; set; }


        public (double, double)[][] AllGivPoints = new (double, double)[9][];
        public (double, double)[][][] AllNewPoints = new (double, double)[9][][];

        [GlobalSetup]
        public void SetUp()
        {
            var r = new Random();
            string folder = "../../../../../../../bin/Debug/net6.0";
            for (int i = 0; i <= 8; i++)
            {
                using (var readerx = new StreamReader($"{folder}/x{i}.txt"))
                using (var readery = new StreamReader($"{folder}/y{i}.txt"))
                {
                    var x = readerx.ReadToEnd().Trim().Split("\r\n");
                    var y = readery.ReadToEnd().Trim().Split("\r\n");

                    AllGivPoints[i] = new (double, double)[x.Length];

                    AllGivPoints[i] = x.Zip(y, (x, y) => (double.Parse(x, NumberStyles.Any), double.Parse(y, NumberStyles.Any))).ToArray();
                    AllNewPoints[i] = Enumerable.Range(0, Optimization.MAX_RANDOMIZATION).Select((_) => Optimization.GenRandomPoints(x.Length, r)).ToArray();
                }
            }
        }

        [Benchmark]
        public Result? Test1()
        {
            return Optimization.Start(AllGivPoints[1], AllNewPoints[1], CalulationThreads, OptimizationThreads);
        }
        
    }
    public class Program
    {
        //dotnet run --project IP.csproj -c Release
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();
            //Benchmark benchmark = new Benchmark();
            //benchmark.SetUp();

            //benchmark.Test1();


        }
    }
}
