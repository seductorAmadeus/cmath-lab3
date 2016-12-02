using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagrangeInterpol
{
    static class InterpolationService
    {
        public static Func<double, double> LagrangeInterpolation(ICollection<ObservablePoint> pointCollection)
        {
            Func<double, double> mainFunc;
            Func<double, int, double>[] funcs = new Func<double, int, double>[pointCollection.Count];
            for (int i = 0; i < pointCollection.Count; ++i)
            {
                funcs[i] = (x, index) =>
                {
                    double k = 1;
                    for (int j = 0; j < pointCollection.Count; ++j)
                    {
                        if (j == index)
                            continue;
                        k *= (x - pointCollection.ElementAt(j).X) / (pointCollection.ElementAt(index).X - pointCollection.ElementAt(j).X);
                    }
                    return pointCollection.ElementAt(index).Y * k;
                };
            }
            mainFunc = x =>
            {
                double sum = 0;
                for (int i = 0; i < pointCollection.Count; ++i)
                    sum += funcs[i](x, i);
                return sum;
            };
            return mainFunc;
        }

        public static Func<double, double> NewtonInterpolation(ICollection<ObservablePoint> pointCollection, double convergeAccuracy)
        {
            Func<double, double> mainFunc = null;
            List<double[]> list = new List<double[]>();
            list.Add(new double[pointCollection.Count]);
            list.Add(new double[pointCollection.Count]);
            int i = 0;
            foreach (ObservablePoint point in pointCollection)
            {
                list[0][i] = point.X;
                list[1][i++] = point.Y;
            }

            bool converged = false;
            i = 0;
            do
            {
                int m = pointCollection.Count - ++i;
                list.Add(new double[m]);
                converged = true;
                for (int j = 0; j < m; ++j)
                {
                    list[i + 1][j] = list[i][j + 1] - list[i][j];
                }
                int convergeCount = 0;
                for (int j = 0; j < m - 1; ++j)
                {
                    if (list[i + 1].Length == 1)
                        break;
                    if (!(Math.Abs(list[i + 1][j] - list[i + 1][j + 1]) > convergeAccuracy))
                    {
                        ++convergeCount;
                    }
                }
                if (convergeCount < list[i + 1].Length / 2)
                    converged = false;
            } while (!converged);

            mainFunc = (x) =>
            {
                double res = list[1][0];
                i = list.Count - 2;
                double h = list[0][1] - list[0][0];
                double q = (x - list[0][0]) / h;
                for (int j = 0; j < i; ++j)
                {
                    double k = list[j + 2][0];
                    k *= q;
                    for (int p = 0; p < j; ++p)
                    {
                        k *= (q - p - 1);
                    }
                    k /= Factorial(j + 1);
                    res += k;
                }
                return res;
            };
            return mainFunc;
        }

        private static int Factorial(int factNo)
        {
            int temno = 1;

            for (int i = 1; i <= factNo; i++)
            {
                temno = temno * i;
            }

            return temno;
        }
    }
}
