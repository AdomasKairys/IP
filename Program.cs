using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace IP
{
    internal class Program
    {
        static double EuclideanNorm((double, double)[] val)
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
        /// <param name="totalSum">Function that calculates price of point2 in respect to other points</param>
        /// <param name="initialVal">Function that calculates the value/price of point2</param>
        /// <returns>z value</returns>
        static double Func((double, double)[] points1, (double, double) point2, 
            Func<(double, double), (double, double), double> totalSum, 
            Func<(double, double), double> initialVal)
        {
            double f = initialVal(point2);
            int n = points1.Length;
            f = Enumerable.Range(0, n).AsParallel().WithDegreeOfParallelism(8).Select((p,i) => totalSum(points1[i], point2)).Sum();
            return f;
        }

        //Calculates the sum of values at each new point
        static double TargetFunc((double, double)[] points1, (double, double)[] points2)
        {
            double priceBtwTwoPnts((double, double) p1, (double, double) p2) =>
                            Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2)));

            double initialPrice((double, double) p2) =>
                            0.4 + (Math.Pow(p2.Item1, 4) + Math.Pow(p2.Item2, 4) + 200 * (Math.Sin(p2.Item1) + Math.Cos(p2.Item2))) / 1000;

            double fullPrice = 0;
            int n = points2.Length;
            for (int i = 0; i < n; i++)
            {
                (double, double)[] comb = new (double, double)[points1.Length + points2.Length - 1];
                points1.CopyTo(comb, 0);
                points2.Where((val, indx) => indx != i).ToArray().CopyTo(comb, points1.Length - 1);


                fullPrice += Func(comb, points2[i], priceBtwTwoPnts, initialPrice);
            }
            return fullPrice;
        }

        
        static (double, double)[] JacobianMatrix((double, double)[] points1, (double, double)[] points2)
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




            (double, double)[] jac = ((double, double)[])points2.Clone();
            int n = jac.Length;

            for(int i = 0; i < n; i++)
            {
                (double, double)[] comb = new (double, double)[points1.Length + points2.Length - 1];
                points1.CopyTo(comb, 0);
                points2.Where((val, indx) => indx != i).ToArray().CopyTo(comb, points1.Length - 1);
                int m = comb.Length;

                var dx = Task.Run( () => Func(comb, points2[i], dxSum, dxInit));
                double dy = Func(comb, points2[i], dySum, dyInit);
                dx.Wait();
                jac[i] = (dx.Result, dy);
            }
            return jac;
        }

        static void Optimization((double, double)[] pointsGiv, (double, double)[] pointsOpt)
        {
            int iter = 0;
            double step = 0.1;
            double eps = 1e-6;

            double objValOld = TargetFunc(pointsGiv, pointsOpt);
            var jac = JacobianMatrix(pointsGiv, pointsOpt);
            Console.WriteLine("Initial " + objValOld);

            while (EuclideanNorm(jac) > eps && iter < 20 && step > eps)
            {
                jac = jac.Select((j) => (j.Item1/EuclideanNorm(jac), j.Item2/EuclideanNorm(jac))).ToArray();
                var temp = jac.Select((j) => (step * j.Item1, step * j.Item2)).ToArray();
                pointsOpt = temp.Zip(pointsOpt, (j, p) => (p.Item1 - j.Item1, p.Item2 - j.Item2)).ToArray();
                double objValNew = TargetFunc(pointsGiv, pointsOpt);

                Console.WriteLine(iter + " " + objValNew);

                if (objValOld < objValNew)
                {
                    pointsOpt = temp.Zip(pointsOpt, (j, p) => (p.Item1 + j.Item1, p.Item2 + j.Item2)).ToArray();
                    step *= 0.9;
                }
                else
                    objValOld = objValNew;

                jac = JacobianMatrix(pointsGiv, pointsOpt);
                iter++;
            }

            foreach(var point in pointsOpt)
            {
                Console.WriteLine(point.Item1 + " " + point.Item2);
            }
        }

        static void Main(string[] args)
        {
            var r = new Random();
            (double, double)[] ranInitPoints = Enumerable.Range(0, 1000).Select(i => (-10+r.NextDouble(),-10+r.NextDouble()*20)).ToArray();
            (double, double)[] ranNewPoints = Enumerable.Range(0, 1000).Select(i => (-10+r.NextDouble(), -10+r.NextDouble() * 20)).ToArray();


            //(double, double)[] initPoints = {(5.42641287, -9.58496101),
            //    (2.6729647, 4.97607765),
            //    (-0.02985975, -5.50406709),
            //    (-6.0387427,  5.21061424),
            //    (-6.61778327, - 8.23320372)};
            //(double, double)[] newPoints = { (3.70719637,  9.06786692),
            //     (-9.92103467,  0.24384527),
            //     ( 6.25241923,  2.25052134),
            //     ( 4.43510635, - 4.16247864)};


            using (var writerx = new StreamWriter("x.txt"))
            using (var writery = new StreamWriter("y.txt"))
            using (var writerxx = new StreamWriter("xx.txt"))
            using (var writeryy = new StreamWriter("yy.txt"))
            {
                foreach (var point in ranInitPoints)
                {
                    writerx.WriteLine(point.Item1);
                    writery.WriteLine(point.Item2);
                }
                foreach (var point in ranNewPoints)
                {
                    writerxx.WriteLine(point.Item1);
                    writeryy.WriteLine(point.Item2);
                }
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Optimization(ranInitPoints, ranNewPoints);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

        }
    }
}