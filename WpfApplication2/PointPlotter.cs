using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagrangeInterpol
{
    public static class PointPlotter
    {
        public static ObservablePoint[] PlotPoints(double beg, double end, double step, Func<double, double> function)
        {
            bool doInc = false;
            if ((end - beg) % step != 0)
                doInc = true;
            int partitionNum = (int)Math.Truncate((end - beg) / step);
            ObservablePoint[] arr = new ObservablePoint[partitionNum + (doInc ? 2 : 1)];
            for (int i = 0; i <= partitionNum; ++i)
            {
                arr[i] = new ObservablePoint(beg + step * i, function(beg + step * i));
            }
            if (doInc)
                arr[arr.Length - 1] = new ObservablePoint(end, function(end));
            return arr;
        }
    }
}
