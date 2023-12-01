using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

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

        static double TargetFunc((double, double)[] points1, (double, double)[] points2)
        {
            Func<(double, double), (double, double), double> priceBtwTwoPnts = (p1, p2) =>
                            Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2)));

            Func<(double, double), double> initialPrice = (p2) =>
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

        static double Func((double, double)[] points1, (double, double) point2, 
            Func<(double, double), (double, double), double> totalSum, 
            Func<(double, double), double> initialVal)
        {
            double f = initialVal(point2);
            int n = points1.Length;
            for (int i = 0; i < n; i++)
                f += totalSum(points1[i], point2);
            return f;
        }

        static (double, double)[] JacobianMatrix((double, double)[] points1, (double, double)[] points2)
        {

            Func<(double, double), (double, double), double> dxSum = (p1, p2) =>
                    0.6 * Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2))) * (p1.Item1 - p2.Item1);

            Func<(double, double), (double, double), double> dySum = (p1, p2) =>
                    0.6 * Math.Exp(-0.3 * (Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2))) * (p1.Item2 - p2.Item2);

            Func<(double, double), double> dxInit = (p1) =>
                        (4 * Math.Pow(p1.Item1, 3) + 200 * Math.Cos(p1.Item1)) / 1000;

            Func<(double, double), double> dyInit = (p1) =>
                        (4 * Math.Pow(p1.Item2, 3) + 200 * Math.Cos(p1.Item2)) / 1000;


            (double, double)[] jac = ((double, double)[])points2.Clone();
            int n = jac.Length;

            for(int i = 0; i < n; i++)
            {
                (double, double)[] comb = new (double, double)[points1.Length + points2.Length - 1];
                points1.CopyTo(comb, 0);
                points2.Where((val, indx) => indx != i).ToArray().CopyTo(comb, points1.Length - 1);
                int m = comb.Length;

                double dx = Func(comb, points2[i], dxSum, dxInit);
                double dy = Func(comb, points2[i], dySum, dyInit);
                jac[i] = (dx, dy);
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

            while (EuclideanNorm(jac) > eps && iter < 1000 && step > eps)
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
            (double, double)[] initPoints = {(5.42641287, -9.58496101),
                (2.6729647, 4.97607765),
                (-0.02985975, -5.50406709),
                (-6.0387427,  5.21061424),
                (-6.61778327, - 8.23320372)};
            (double, double)[] newPoints = { (3.70719637,  9.06786692),
                 (-9.92103467,  0.24384527),
                 ( 6.25241923,  2.25052134),
                 ( 4.43510635, - 4.16247864)};

            Optimization(initPoints, newPoints);
            
        }
    }
}