using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.ComponentModel;

namespace LagrangeInterpol
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public SeriesCollection GlobalCollection { get; set; }

        public Func<double, double> sinFunc;
        public Func<double, double> eFunc;

        private double _to;             //These values control Y-axis zoom
        private double _from;

        private double _x;              //These values control func values
        private double _y;

        private LineSeries rawFunctionSeries;
        private ChartValues<ObservablePoint> rawFunctionValues;

        private LineSeries[] interpolFuncSeries = new LineSeries[3]; //Actual graphing points
        private ChartValues<ObservablePoint>[] interpolFuncValues = new ChartValues<ObservablePoint>[3];

        private LineSeries[] pointSeries = new LineSeries[3]; //Binding-points for graphs
        private ChartValues<ObservablePoint>[] pointValues = new ChartValues<ObservablePoint>[3];

        private Func<double, double>[] lagrFunc = new Func<double, double>[3];

        private bool drawRawFunction = true;                                    //Check-boxes' inner representation
        private bool drawFunc1 = true, drawFunc2 = false, drawFunc3 = false;
        
        //Sine or E-func? Used for initializing SetFunctions window and drawing the actual function
        private int currFunction = -1;                                          

        public bool? RawFuncCheck
        {
            get { return drawRawFunction; }
            set
            {
                if ((bool)value)
                    drawRawFunction = true;
                else
                    drawRawFunction = false;
            }
        }
        public bool? Func1Check
        {
            get { return drawFunc1; }
            set
            {
                if ((bool)value)
                    drawFunc1 = true;
                else
                    drawFunc1 = false;
            }
        }
        public bool? Func2Check
        {
            get { return drawFunc2; }
            set
            {
                if ((bool)value)
                    drawFunc2 = true;
                else
                    drawFunc2 = false;
            }
        }
        public bool? Func3Check
        {
            get { return drawFunc3; }
            set
            {
                if ((bool)value)
                    drawFunc3 = true;
                else
                    drawFunc3 = false;
            }
        }

        public MainWindow()
        {
           
            InitializeComponent();
         
            Random rand = new Random();
            DataContext = this;
            chart.ChartLegend = null;
            //Setting functions
            sinFunc = (x) => { return Math.Sin(x); };
            eFunc = (x) => { return Math.Pow(Math.E, Math.Sin(x)) + 0.1 * x * x; };
            //Setting up chart
            for (int i = 0; i < 3; ++i)
            {
                pointValues[i] = new ChartValues<ObservablePoint>();
                pointSeries[i] = new LineSeries
                {
                    Values = pointValues[i],
                    LineSmoothness = 0,
                    Fill = new SolidColorBrush(Color.FromArgb(2, 41, 12, 125)),
                    Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    PointForeround = new SolidColorBrush(Color.FromRgb((byte)rand.Next(0,255), (byte)rand.Next(0, 255), (byte)rand.Next(0, 255)))
                };
                interpolFuncValues[i] = new ChartValues<ObservablePoint>();
                interpolFuncSeries[i] = new LineSeries
                {
                    Values = interpolFuncValues[i],
                    LineSmoothness = 0,
                    Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    PointGeometrySize = 0
                };
            }
            rawFunctionValues = new ChartValues<ObservablePoint>();
            rawFunctionSeries = new LineSeries
            {
                Values = rawFunctionValues,
                LineSmoothness = 0,
                PointGeometrySize = 0
            };
            GlobalCollection = new SeriesCollection
            {
            };
            //Setting up chart viewing region
            From = 0;
            To = 20;
        }

        private void setGraphsButton_Click(object sender, RoutedEventArgs e)
        {
            double min = Double.MaxValue, max = Double.MinValue; //Used later on for setting the actual function's draw interval

            List<ObservablePoint>[] points = new List<ObservablePoint>[3];
            for (int i = 0; i < 3; ++i)
            {
                ObservablePoint[] temp1 = pointValues[i].ToArray();
                points[i] = new List<ObservablePoint>(temp1);
            }

            //This window will get us the points we use for interpolation from the user
            Window1 newWindow = new Window1(points);
            if (currFunction != 0)                          //Setting up SetFunction's RadioButtons as for which function we are currently using
                newWindow.radioButton2.IsChecked = true;
            newWindow.ShowDialog();
            ICollection<ObservablePoint>[] temp = newWindow.returnPoints();
            List<ObservablePoint>[] newPoints = new List<ObservablePoint>[3];
            for (int i = 0; i < 3; ++i)
            {
                newPoints[i] = temp[i].ToList();
                newPoints[i].Sort(new PointXSorter());

                //If the points we got from the user did not change from the previous ones, then we don't need to redraw the graph
                if (compareLists(newPoints[i], points[i]))
                    continue;
                pointValues[i].Clear();
                interpolFuncValues[i].Clear();
                if (newPoints[i].Count == 0)
                {
                    lagrFunc[i] = null;
                    continue;
                }

                if (newPoints[i][0].X < min)
                    min = newPoints[i][0].X;
                if (newPoints[i][newPoints[i].Count - 1].X > max)
                    max = newPoints[i][newPoints[i].Count - 1].X;

                pointValues[i].AddRange(newPoints[i]);
                lagrFunc[i] = InterpolationService.LagrangeInterpolation(newPoints[i]);  //This will give us the function that was created by InterpolationService
                interpolFuncValues[i].AddRange(                                          //This will give us the values with the given step, interval 
                    PointPlotter.PlotPoints(pointValues[i][0].X, pointValues[i][pointValues[i].Count - 1].X, 0.01, // and function and set it for graphing automatically
                    lagrFunc[i]));
            }

            //Updating radiobutton information. Do we need to change the base function we are drawing?
            int newFuncIndex = -1;
            if ((bool)newWindow.radioButton1.IsChecked)
                newFuncIndex = 0;
            else
                newFuncIndex = 1;

            //If we received no points for the user, then just set the drawing interval of the base function to some random values
            if (min == Double.MaxValue || max == Double.MinValue)
            {
                min = -6;
                max = 6;
            }

            //Do we need to redraw the base function? If yes, then please do
            if (newFuncIndex != currFunction || min != rawFunctionValues[0].X || max != rawFunctionValues[rawFunctionValues.Count - 1].X)
            {
                rawFunctionValues.Clear();
                currFunction = newFuncIndex;
                if (newFuncIndex == 0)
                {
                    rawFunctionValues.AddRange(PointPlotter.PlotPoints(min, max, 0.1, sinFunc));
                }
                else
                    rawFunctionValues.AddRange(PointPlotter.PlotPoints(min, max, 0.1, eFunc));
                double maxFunc = Double.MinValue;
                double minFunc = Double.MaxValue;
                foreach(ObservablePoint point in rawFunctionValues)
                {
                    if (point.Y > maxFunc)
                        maxFunc = point.Y;
                    if (point.Y < minFunc)
                        minFunc = point.Y;
                }
                //Setting viewing intervals
                From = Math.Round(minFunc * 1.1, 2);
                To = Math.Round(maxFunc * 1.1, 2);
            }
        }

        private void basicFuncCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            if (check == basicFuncCheckBox)
            {
                if (drawRawFunction)
                {
                    GlobalCollection.Add(rawFunctionSeries);
                }
                else
                    GlobalCollection.Remove(rawFunctionSeries);
            }
            else if (check == graphCheckBox1)
            {
                if (drawFunc1)
                {
                    GlobalCollection.Add(interpolFuncSeries[0]);
                    GlobalCollection.Add(pointSeries[0]);
                }
                else
                {
                    GlobalCollection.Remove(interpolFuncSeries[0]);
                    GlobalCollection.Remove(pointSeries[0]);
                }
            }
            else if (check == graphCheckBox2)
            {
                if (drawFunc2)
                {
                    GlobalCollection.Add(interpolFuncSeries[1]);
                    GlobalCollection.Add(pointSeries[1]);
                }
                else
                {
                    GlobalCollection.Remove(interpolFuncSeries[1]);
                    GlobalCollection.Remove(pointSeries[1]);
                }
            }
            else if (check == graphCheckBox3)
            {
                if (drawFunc3)
                {
                    GlobalCollection.Add(interpolFuncSeries[2]);
                    GlobalCollection.Add(pointSeries[2]);
                }
                else
                {
                    GlobalCollection.Remove(interpolFuncSeries[2]);
                    GlobalCollection.Remove(pointSeries[2]);
                }
            }
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void chart_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var point = chart.ConvertToChartValues(e.GetPosition(chart));

                X = point.X;
            }
            catch
            {
                return;
            }
        }

        private bool compareLists(List<ObservablePoint> listA, List<ObservablePoint> listB)
        {
            if (listA.Count != listB.Count)
                return false;
            for (int i = 0; i < listA.Count; ++i)
            {
                if (!(listA[i].X == listB[i].X && listA[i].Y == listB[i].Y))
                    return false;
            }
            return true;
        }

        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                Func<double, double> func = (currFunction == 0) ? sinFunc : eFunc;
                switch (comboBox.SelectedIndex)
                {
                    case 0:
                        Y = func(value);
                        break;
                    case 1:
                    case 2:
                    case 3:
                        if (lagrFunc[comboBox.SelectedIndex - 1] == null)
                        {
                            Y = Double.NaN;
                            return;
                        }
                        Y = lagrFunc[comboBox.SelectedIndex - 1](value);
                        break;
                    default:
                        break;
                }
                OnPropertyChanged("X");
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        public double From
        {
            get { return _from; }
            set
            {
                _from = value;
                OnPropertyChanged("From");
            }
        }

        public double To
        {
            get { return _to; }
            set
            {
                _to = value;
                OnPropertyChanged("To");
            }
        }

        private void XBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Clear();
        }

        private void XBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox textBox = sender as TextBox;
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
                be.UpdateSource();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PointXSorter : IComparer<ObservablePoint>
    {
        public int Compare(ObservablePoint x, ObservablePoint y)
        {
            if (x.X < y.X)
                return -1;
            else if (x.X == y.X)
                return 0;
            else
                return 1;
        }
    }
}
