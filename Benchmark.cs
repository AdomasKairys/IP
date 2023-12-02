using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using System;
using System.Globalization;
using System.IO;

namespace IP
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 6)]
    public class Benchmark
    {
        [Params(2, 8, 16, 32)]
        public int ThredCount { get; set; }

        public (double, double)[][] AllGivPoints = new (double, double)[9][];
        public (double, double)[][] AllNewPoints = new (double, double)[9][];

        [GlobalSetup]
        public void SetUp()
        {
            for (int i = 0; i <= 8; i++)
            {
                using (var readerx = new StreamReader($"../../../../../../../Data/x{i}.txt")) // ../../../../../../../Data/
                using (var readery = new StreamReader($"../../../../../../../Data/y{i}.txt"))
                using (var readerxx = new StreamReader($"../../../../../../../Data/xx{i}.txt"))
                using (var readeryy = new StreamReader($"../../../../../../../Data/yy{i}.txt"))
                {
                    var x = readerx.ReadToEnd().Trim().Split("\r\n");
                    var xx = readerxx.ReadToEnd().Trim().Split("\r\n");
                    var y = readery.ReadToEnd().Trim().Split("\r\n");
                    var yy = readeryy.ReadToEnd().Trim().Split("\r\n");

                    AllGivPoints[i] = new (double, double)[x.Length];
                    AllNewPoints[i] = new (double, double)[x.Length];

                    AllGivPoints[i] = x.Zip(x, (x, y) => (double.Parse(x, NumberStyles.Any), double.Parse(y, NumberStyles.Any))).ToArray();
                    AllNewPoints[i] = xx.Zip(yy, (x, y) => (double.Parse(x, NumberStyles.Any), double.Parse(y, NumberStyles.Any))).ToArray();
                }
            }
        }

        [Benchmark]
        public double Test1()
        {
            return Optimization.Optimize(AllGivPoints[0], AllNewPoints[0], ThredCount);
        }
        [Benchmark]
        public double Test2()
        {
            return Optimization.Optimize(AllGivPoints[1], AllNewPoints[1], ThredCount);
        }
        [Benchmark]
        public double Test3()
        {
            return Optimization.Optimize(AllGivPoints[2], AllNewPoints[2], ThredCount);
        }
        [Benchmark]
        public double Test4()
        {
            return Optimization.Optimize(AllGivPoints[3], AllNewPoints[3], ThredCount);
        }
        [Benchmark]
        public double Test5()
        {
            return Optimization.Optimize(AllGivPoints[4], AllNewPoints[4], ThredCount);
        }
        //[Benchmark]
        //public double Test6()
        //{
        //    return Optimization.Optimize(AllGivPoints[5], AllNewPoints[5], ThredCount);
        //}
        //[Benchmark]
        //public double Test7()
        //{
        //    return Optimization.Optimize(AllGivPoints[6], AllNewPoints[6], ThredCount);
        //}
        //[Benchmark]
        //public double Test8()
        //{
        //    return Optimization.Optimize(AllGivPoints[7], AllNewPoints[7], ThredCount);
        //}
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
