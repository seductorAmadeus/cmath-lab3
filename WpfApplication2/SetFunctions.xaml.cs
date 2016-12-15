using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Shapes;

namespace LagrangeInterpol
{
    public partial class Window1 : Window
    {
        public ObservableCollection<ObservablePoint>[] points = new ObservableCollection<ObservablePoint>[3];
        public int currIndex;

        public Func<double, double> sinFunc = (x) => { return Math.Sin(x); };
        public Func<double, double> eFunc = (x) => { return Math.Pow(Math.E, Math.Sin(x)) + 0.1 * x* x; };

        private double X;
        private double Y;

        public int genNumber { get; set; }
        public double begInterval { get; set; }
        public double endInterval { get; set; }

        private bool initCheck;

        public double XSet { get { return X; }
            set
            {
                X = value;
                if ((bool)radioButton1.IsChecked)
                {
                    funcTextBox.Text = sinFunc(X).ToString();
                    YSet = sinFunc(X);
                }
                else
                {
                    funcTextBox.Text = eFunc(X).ToString();
                    YSet = eFunc(X);
                }
            }
        }
        public double YSet { get { return Y; } set { Y = value; } }

        private double thickness;

        public Window1(ICollection<ObservablePoint>[] initialSet)
        {
            InitializeComponent();
            if (initialSet.Length != 3)
                throw new ArgumentException("Array should be of size 3");
            for (int i = 0; i < 3; ++i)
                points[i] = new ObservableCollection<ObservablePoint>(initialSet[i]);
            thickness = argTextBox.BorderThickness.Bottom;
            currIndex = 0;
            initCheck = true;
            dataGrid.ItemsSource = points[0];
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedIndex == -1)
                return;
            points[currIndex].RemoveAt(dataGrid.SelectedIndex);
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            BindingExpression be1 = argTextBox.GetBindingExpression(TextBox.TextProperty);
            BindingExpression be2 = funcTextBox.GetBindingExpression(TextBox.TextProperty);
            if (be1.HasValidationError || be2.HasValidationError)
                return;
            foreach (ObservablePoint point in points[currIndex])
            {
                if (point.X == X && point.Y == Y)
                {
                    argTextBox.ToolTip = "Value already exists";
                    argTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    argTextBox.BorderThickness = new Thickness(thickness * 1.5);
                    funcTextBox.ToolTip = "Value already exists";
                    funcTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    funcTextBox.BorderThickness = new Thickness(thickness * 1.5);
                    return;
                }
            }
            points[currIndex].Add(new ObservablePoint(X, Y));
        }

        private void argTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.Clear();
            textBox.BorderThickness = new Thickness(thickness);
            textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(171,173,179));
        }

        private void dataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                deleteButton_Click(sender, null);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public ICollection<ObservablePoint>[] returnPoints()
        {
            ICollection<ObservablePoint>[] ret = new ICollection<ObservablePoint>[3];
            for (int i = 0; i < 3; ++i)
                ret[i] = points[i].ToArray();
            return ret;
        }

        private void graphButton1_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            dataGrid.ItemsSource = null;
            if (radio == graphButton1)
            {
                dataGrid.ItemsSource = points[0];
                currIndex = 0;
            }
            else if (radio == graphButton2)
            {
                dataGrid.ItemsSource = points[1];
                currIndex = 1;
            }
            else
            {
                dataGrid.ItemsSource = points[2];
                currIndex = 2;
            }
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            double X;
            double Y;
            if (e.Column.DisplayIndex != 0)
                return;
            if (!double.TryParse((e.EditingElement as TextBox).Text, out X))
                return;
            
            if ((bool)radioButton1.IsChecked)
            {
                Y = sinFunc(X);
                var cell = dataGrid.Columns[1].GetCellContent(e.Row);
                (cell as TextBlock).Text = sinFunc(X).ToString();
            }
            else
            {
                (dataGrid.Columns[1].GetCellContent(e.Row) as TextBlock).Text = eFunc(X).ToString();
                Y = eFunc(X);
            }
            points[currIndex][e.Row.GetIndex()].Y = Y;
        }

        private void textBox_Copy1_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Equals("N"))
            {
                textBox.Clear();
                Binding bind = new Binding();
                bind.Mode = BindingMode.TwoWay;
                bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                bind.ElementName = "window";
                bind.Path = new PropertyPath("genNumber");
                bind.ValidationRules.Add(new IntegerValidation());
                textBox.SetBinding(TextBox.TextProperty, bind);
            }
            textBox.SelectAll();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            points[currIndex].Clear();
            double step = (endInterval - begInterval) / genNumber;
            if (endInterval == begInterval)
                return;
            for (double i = 0; i <= genNumber; ++i)
            {
                double X = begInterval + i * step;
                points[currIndex].Add(new ObservablePoint(X, (bool)radioButton1.IsChecked ?  sinFunc(X) : eFunc(X)));
            }
        }

        private void radioButton2_Checked(object sender, RoutedEventArgs e)
        {
            if (initCheck)
            {
                initCheck = false;
                return;
            }
            for (int i = 0; i < 3; ++i)
            {
                if (points[i] != null)
                {
                    points[i].Clear();
                }
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Equals("0"))
            {
                textBox.Clear();
                return;
            }
            textBox.SelectAll();
        }
    }

    public class NumberValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double temp;
            if (double.TryParse((string)value, out temp))
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Not a double value");
        }
    }

    public class IntegerValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int temp;
            if (int.TryParse((string)value, out temp) && temp >= 0)
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Not a double value");
        }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double temp;
            if (double.TryParse((string)value, out temp))
            {
                return temp;
            }
            throw new ApplicationException("Double value expected");
        }
    }
}
