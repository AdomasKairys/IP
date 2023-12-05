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
        [Params(1, 2, 4, 8, 16, 32, 64, 128)]
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
        public Result? Test0()
        {
            return Optimization.Start(AllGivPoints[0], AllNewPoints[0], OptimizationThreads);
        }
        [Benchmark]
        public Result? Test1()
        {
            return Optimization.Start(AllGivPoints[1], AllNewPoints[1], OptimizationThreads);
        }
        [Benchmark]
        public Result? Test2()
        {
            return Optimization.Start(AllGivPoints[2], AllNewPoints[2], OptimizationThreads);
        }
        //[Benchmark]
        //public Result? Test3()
        //{
        //    return Optimization.Start(AllGivPoints[3], AllNewPoints[3], OptimizationThreads);
        //}
        //[Benchmark]
        //public Result? Test4()
        //{
        //    return Optimization.Start(AllGivPoints[4], AllNewPoints[4], OptimizationThreads);
        //}
        //[Benchmark]
        //public Result? Test5()
        //{
        //    return Optimization.Start(AllGivPoints[5], AllNewPoints[5], OptimizationThreads);
        //}
        //[Benchmark]
        //public Result? Test6()
        //{
        //    return Optimization.Start(AllGivPoints[6], AllNewPoints[6], OptimizationThreads);
        //}
        //[Benchmark]
        //public Result? Test7()
        //{
        //    return Optimization.Start(AllGivPoints[7], AllNewPoints[7], OptimizationThreads);
        //}
        //[Benchmark]
        //public Result? Test8()
        //{
        //    return Optimization.Start(AllGivPoints[8], AllNewPoints[8], OptimizationThreads);
        //}

    }
    public class Program
    {
        //dotnet run --project IP.csproj -c Release
        //static void Main(string[] args)
        //{
        //    var summary = BenchmarkRunner.Run<Benchmark>();
        //    //Benchmark benchmark = new Benchmark();
        //    //benchmark.SetUp();

        //    //benchmark.Test1();


        //}
    }
}
