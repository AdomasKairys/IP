using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using Python.Runtime;
using System.Globalization;


namespace IP
{

    public class Result : IComparable<Result>
    {
        public (double, double)[] Points { get; private set; }
        public double Value { get; private set; }

        public Result((double, double)[] points, double value)
        {
            Points = points;
            Value = value;
        }

        public int CompareTo(Result? obj)
        {
            if(obj == null)
                return 1;
            return Value.CompareTo(obj.Value);
        }
    }
    internal class Optimization
    {
        //change these to get more accurate optimization
        public const double EPS = 1e-3;
        public const double STEP = 0.2;
        public const int MAX_ITER = 500;
        public const int MAX_RANDOMIZATION = 200;

        private static int THREAD_COUNT_CALC = 6; //thread count for calculations (target function, jacobian matrix)
        private static int THREAD_COUNT_OPT = 6; //thread count for optimizations (multiple optimization calls)

        private static double EuclideanNorm((double, double)[] val)
        {
            double sum = 0;
            for(int i = 0; i < val.Length; i++)
            {
                sum += Math.Pow(val[i].Item1, 2) + Math.Pow(val[i].Item2, 2);
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Calculates z value of a function f(x,y)
        /// </summary>
        /// <param name="points1">Point set on the plane (x,y)</param>
        /// <param name="point2">Point to calculate z at</param>
        /// <param name="priceBtwPoints">Function that calculates price of point2 in respect to other points</param>
        /// <param name="priceOfPoint">Function that calculates the value/price of point2</param>
        /// <returns>z value</returns>
        private static double Func((double, double)[] points1, (double, double) point2, 
            Func<(double, double), (double, double), double> priceBtwPoints, 
            Func<(double, double), double> priceOfPoint)
        {
            double f = priceOfPoint(point2);
            f += points1.Select(p => priceBtwPoints(p, point2)).Sum();
            return f;
        }

        //Calculates the sum of values at each new point
        private static double TargetFunc((double, double)[] points1, (double, double)[] points2)
        {
            double priceBtwPoints((double, double) p1, (double, double) p2) =>
                            Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2)));

            double priceOfPoint((double, double) p2) =>
                            0.4 + (Math.Pow(p2.Item1, 4) + Math.Pow(p2.Item2, 4) + 200 * (Math.Sin(p2.Item1) + Math.Cos(p2.Item2))) / 1000;

            double fullPrice = points2.AsParallel().WithDegreeOfParallelism(THREAD_COUNT_CALC).Select((p2,i) => {
                (double, double)[] comb = new (double, double)[points1.Length + points2.Length - 1];
                points1.CopyTo(comb, 0);
                points2.Where((val, indx) => indx != i).ToArray().CopyTo(comb, points1.Length);
                return Func(comb, p2, priceBtwPoints, priceOfPoint);
            }).Sum();

            return fullPrice;
        }


        private static (double, double)[] JacobianMatrix((double, double)[] points1, (double, double)[] points2)
        {

            //partial derivatives

            double dxSum((double, double) p1, (double, double) p2) =>
                    0.6 * Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2))) * (p1.Item1 - p2.Item1);
            
            double dySum((double, double) p1, (double, double) p2) =>
                    0.6 * Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2))) * (p1.Item2 - p2.Item2);

            double dxInit((double, double) p1) =>
                        (4 * Math.Pow(p1.Item1, 3) + 200 * Math.Cos(p1.Item1)) / 1000;

            double dyInit((double, double) p1) =>
                        (4 * Math.Pow(p1.Item2, 3) + 200 * Math.Cos(p1.Item2)) / 1000;


            (double, double)[] jac = points2.AsParallel().WithDegreeOfParallelism(THREAD_COUNT_CALC).Select((val,i) =>
            {
                (double, double)[] comb = new (double, double)[points1.Length + points2.Length - 1];
                points1.CopyTo(comb, 0);
                points2.Where((val, indx) => indx != i).ToArray().CopyTo(comb, points1.Length - 1);

                double dx = Func(comb, points2[i], dxSum, dxInit);
                double dy = Func(comb, points2[i], dySum, dyInit);
                return (dx, dy);

            }).ToArray();

            return jac;
        }

        public static Result Optimize((double, double)[] pointsGiv, (double, double)[] pointsOpt)
        {
            int iter = 0;
            double step = STEP;

            double objValOld = TargetFunc(pointsGiv, pointsOpt);
            var jac = JacobianMatrix(pointsGiv, pointsOpt);
            //Console.WriteLine("Initial " + objValOld);
            double euclid;
            while ((euclid = EuclideanNorm(jac)) > EPS && iter < MAX_ITER && step > EPS)
            {
                jac = jac.Select((j) => (j.Item1/ euclid, j.Item2/ euclid)).ToArray();
                var temp = jac.Select((j) => (step * j.Item1, step * j.Item2));
                pointsOpt = temp.Zip(pointsOpt, (j, p) => (p.Item1 - j.Item1, p.Item2 - j.Item2)).ToArray();
                double objValNew = TargetFunc(pointsGiv, pointsOpt);

                //Console.WriteLine(iter + " " + objValNew);

                if (objValOld < objValNew)
                {
                    pointsOpt = temp.Zip(pointsOpt, (j, p) => (p.Item1 + j.Item1, p.Item2 + j.Item2)).ToArray();
                    step *= 0.9;
                }
                else
                {
                    objValOld = objValNew;
                }

                

                jac = JacobianMatrix(pointsGiv, pointsOpt);
                iter++;
            }

            

            return new Result(pointsOpt,objValOld);
        }

        private static void GenDataFiles()
        {
            var r = new Random();

            //-10+r.NextDouble()*20 == -10+r.NextDouble()*(10-(-10), where 10 and -10 are the ranges of the function and shouldn't be changed


            //(double, double)[] initPoints = {(5.42641287, -9.58496101),
            //    (2.6729647, 4.97607765),
            //    (-0.02985975, -5.50406709),
            //    (-6.0387427,  5.21061424),
            //    (-6.61778327, - 8.23320372)};
            //(double, double)[] newPoints = { (3.70719637,  9.06786692),
            //     (-9.92103467,  0.24384527),
            //     ( 6.25241923,  2.25052134),
            //     ( 4.43510635, - 4.16247864)};



            for (int i = 0; i <= 8; i++)
            {
                (double, double)[] ranInitPoints = Enumerable.Range(0, i * i + 3).Select(i => (-10 + r.NextDouble() * 20, -10 + r.NextDouble() * 20)).ToArray();

                using (var writerx = new StreamWriter($"x{i}.txt"))
                using (var writery = new StreamWriter($"y{i}.txt"))
                {
                    foreach (var point in ranInitPoints)
                    {
                        writerx.WriteLine(point.Item1);
                        writery.WriteLine(point.Item2);
                    }
                }
            }
        }

        private static (double, double)[] ReadPoints(string fileNameX, string fileNameY)
        {
            using (var readery = new StreamReader(fileNameY))
            using (var readerx = new StreamReader(fileNameX))
            {
                var xx = readerx.ReadToEnd().Trim().Split("\r\n");
                var yy = readery.ReadToEnd().Trim().Split("\r\n");
                var points = xx.Zip(yy, (x, y) => (double.Parse(x, NumberStyles.Any), double.Parse(y, NumberStyles.Any))).ToArray();
                return points;
            }
        }
        public static (double, double)[] GenRandomPoints(int size, Random ran)
        {
            //-10+r.NextDouble()*20 == -10+r.NextDouble()*(10-(-10), where 10 and -10 are the ranges of the function and shouldn't be changed
            return Enumerable.Range(0, size).Select(i => (-10 + ran.NextDouble() * 20, -10 + ran.NextDouble() * 20)).ToArray();
        }

        public static Result? Start((double, double)[] initPoints, (double, double)[][] newPointsArray, int threadCountCalc = 1, int threadCountOpt = 1)
        {
            THREAD_COUNT_OPT = threadCountOpt;
            THREAD_COUNT_CALC = threadCountCalc;

            return newPointsArray.AsParallel().WithDegreeOfParallelism(THREAD_COUNT_OPT).Select(
                    (newPoints) => Optimize(initPoints, newPoints)).Min();
        }

        //static void Main(string[] args)
        //{
        //    //GenDataFiles();
        //    var r = new Random(Guid.NewGuid().GetHashCode());
        //    (double, double)[] initPoints;
        //    while (true)
        //    {
        //        try
        //        {
        //            Runtime.PythonDLL = @"C:\Users\kairy\AppData\Local\Programs\Python\Python312\python312.dll";
        //            PythonEngine.Initialize();
        //        }
        //        catch (Exception)
        //        {
        //            Console.WriteLine("Could not locate python dll file, continue (y/n)?");
        //            if (Console.ReadLine() == "y")
        //                continue;
        //            break;
        //        }



        //        Console.WriteLine("Choose data size 0-8 or generate new random 9 (or non-number to exit):");
        //        string? input = Console.ReadLine();

        //        if (!int.TryParse(input, out int choice))
        //            return;

        //        if (choice >= 0 && choice <= 8)
        //        {
        //            initPoints = ReadPoints($"x{choice}.txt", $"y{choice}.txt");
        //        }
        //        else if (choice == 9)
        //        {
        //            Console.Write("Choose random data size 3-100 (or non-number to exit):");
        //            input = Console.ReadLine();

        //            if (!int.TryParse(input, out int size))
        //                return;

        //            if (size < 3 || size > 100)
        //            {
        //                Console.WriteLine("*Please input correct number");
        //                continue;
        //            }

        //            initPoints = GenRandomPoints(size, r);
        //        }
        //        else
        //        {
        //            Console.WriteLine("*Please input correct number");
        //            continue;
        //        }


        //        Console.WriteLine("Choose the ammount of new points to create (0 <= for random or non-number to exit):");
        //        input = Console.ReadLine();

        //        if (!int.TryParse(input, out int newsize))
        //            return;

        //        if (newsize <= 0)
        //            newsize = initPoints.Length;

        //        (double, double)[][] newPoints = Enumerable.Range(0, MAX_RANDOMIZATION).Select((_) => GenRandomPoints(newsize, r)).ToArray();

        //        Stopwatch stopwatch = new();
        //        stopwatch.Start();

        //        var result = Start(initPoints, newPoints);

        //        if(result == null)
        //            break;

        //        stopwatch.Stop();
        //        TimeSpan ts = stopwatch.Elapsed;
        //        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",ts.Hours, ts.Minutes, ts.Seconds,ts.Milliseconds / 10);

        //        using(var writerx = new StreamWriter("xx.txt"))
        //        using (var writery = new StreamWriter("yy.txt"))
        //        {
        //            foreach (var point in result.Points)
        //            {
        //                Console.WriteLine(point.Item1 + " " + point.Item2);
        //                writerx.WriteLine(point.Item1);
        //                writery.WriteLine(point.Item2);

        //            }
        //        }

        //        Console.WriteLine("Target function value " + result.Value);
        //        Console.WriteLine("RunTime " + elapsedTime);

        //        using (Py.GIL())
        //        {
        //            var pyScript = Py.Import("Visualization");
        //            var pyString = new PyString(choice.ToString());
        //            pyScript.InvokeMethod("visualization", pyString);
        //        }


        //        break;
        //    }
        //}
    }
}