using LiveCharts.Wpf;
using LiveCharts;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Data;
using System.IO;
using OfficeOpenXml;

namespace DataVisualizationDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable dataTable;
        private string dataFilePath;
        public MainWindow()
        {
            InitializeComponent();
            PointLabel = chartPoint =>
                 string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            DataContext = this;
        }
        public Func<ChartPoint, string> PointLabel { get; set; }

        private void Chart_OnDataClick(object sender, ChartPoint chartpoint)
        {
            var chart = (LiveCharts.Wpf.PieChart)chartpoint.ChartView;

            //clear selected slice.
            foreach (PieSeries series in chart.Series)
                series.PushOut = 0;

            var selectedSeries = (PieSeries)chartpoint.SeriesView;
            selectedSeries.PushOut = 8;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private bool IsMaximize = false;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximize)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1280;
                    this.Height = 780;

                    IsMaximize = false;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;

                    IsMaximize = true;
                }
            }
        }

        private void UploadData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx;*.csv",
                Title = "Select a Data File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                dataFilePath = openFileDialog.FileName;
                dataTable = LoadExcelFile(dataFilePath);
                PopulateDropdowns();
            }
        }

        private DataTable LoadExcelFile(string filePath)
        {
            var dt = new DataTable();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var worksheet = package.Workbook.Worksheets[0];
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    dt.Columns.Add(worksheet.Cells[1, col].Text);

                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    var newRow = dt.NewRow();
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        newRow[col - 1] = worksheet.Cells[row, col].Text;

                    dt.Rows.Add(newRow);
                }
            }
            return dt;
        }
        private void PopulateDropdowns()
        {
            var labelColumns = new List<string>();
            var valueColumns = new List<string>();

            foreach (DataColumn column in dataTable.Columns)
            {
                bool isNumericColumn = true;

                foreach (DataRow row in dataTable.Rows)
                {
                    var cellValue = row[column].ToString();

                    // التحقق إذا كانت القيمة ليست رقمية
                    if (!double.TryParse(cellValue, out _) && !string.IsNullOrWhiteSpace(cellValue))
                    {
                        isNumericColumn = false;
                        break;
                    }
                }

                if (isNumericColumn)
                {
                    valueColumns.Add(column.ColumnName); // إضافة الأعمدة الرقمية إلى القائمة
                }
                else
                {
                    labelColumns.Add(column.ColumnName); // إضافة الأعمدة النصية إلى القائمة
                }
            }

            // ضبط القوائم على الـ ComboBox
            LabelSelection.ItemsSource = labelColumns; // الأعمدة النصية
            ValueSelection.ItemsSource = valueColumns; // الأعمدة الرقمية
        }


        //private void PopulateDropdowns()
        //{
        //    var columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        //    LabelSelection.ItemsSource = columns;
        //    ValueSelection.ItemsSource = columns;
        //}
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (LabelSelection.SelectedItem == null || ValueSelection.SelectedItem == null)
            {
                MessageBox.Show("Please select both Label and Value columns.");
                return;
            }

            string xColumn = LabelSelection.SelectedItem.ToString();
            string yColumn = ValueSelection.SelectedItem.ToString();

            InitializeBarChart(xColumn, yColumn);
            InitializeLineChart(xColumn, yColumn);
            InitializePieChart(xColumn, yColumn);
            InitializeRowChart(xColumn, yColumn);
        }
        private void InitializeBarChart(string xColumn, string yColumn)
        {
            // Create lists to store the data for the X and Y axes
            List<string> xValues = new List<string>();
            List<double> yValues = new List<double>();

            // Loop through the rows in the DataTable and extract the values
            foreach (DataRow row in dataTable.Rows)
            {
                // Add values for the X-axis (labels)
                xValues.Add(row[xColumn].ToString());
                // Add values for the Y-axis (values)
                yValues.Add(Convert.ToDouble(row[yColumn]));
            }

            // Create a new ColumnSeries for the bar chart
            var columnSeries = new LiveCharts.Wpf.ColumnSeries
            {
                Title = $"{xColumn} vs {yColumn}",
                Values = new LiveCharts.ChartValues<double>(yValues)
            };

            // Clear any previous data on the BarChart
            BarChart.Series.Clear();

            // Add the new series to the BarChart
            BarChart.Series.Add(columnSeries);

            // Update the X-axis with the label values
            BarChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn,
                Labels = xValues
            };
            BarChart.AxisX.Add(axisX);

            // Update the Y-axis (optional formatting)
            BarChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                LabelFormatter = value => $"{value:N0}" // Format Y-axis as numeric with commas
            };
            BarChart.AxisY.Add(axisY);
        }
        private void InitializeRowChart(string xColumn, string yColumn)
        {
            List<string> xValues = new List<string>();
            List<double> yValues = new List<double>();

            foreach (DataRow row in dataTable.Rows)
            {
                xValues.Add(row[xColumn].ToString());
                yValues.Add(Convert.ToDouble(row[yColumn]));
            }

            var rowSeries = new LiveCharts.Wpf.RowSeries
            {
                Title = $"{xColumn} vs {yColumn}",
                Values = new LiveCharts.ChartValues<double>(yValues)
            };

            rowSeries.Fill = new LinearGradientBrush(
                Color.FromRgb(40, 137, 252), Color.FromRgb(255, 255, 255), 0);

            RowChart.Series.Clear();

            RowChart.Series.Add(rowSeries);

            RowChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn,
                Labels = xValues
            };
            RowChart.AxisX.Add(axisX);

            RowChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                LabelFormatter = value => $"{value:N0}"
            };
            RowChart.AxisY.Add(axisY);
        }
        private void InitializeLineChart(string xColumn, string yColumn)
        {
            List<string> xValues = new List<string>();
            List<double> yValues = new List<double>();

            foreach (DataRow row in dataTable.Rows)
            {
                xValues.Add(row[xColumn].ToString());
                yValues.Add(Convert.ToDouble(row[yColumn]));
            }

            var lineSeries = new LiveCharts.Wpf.LineSeries
            {
                Title = $"{xColumn} vs {yColumn}",
                Values = new LiveCharts.ChartValues<double>(yValues),
                Fill = System.Windows.Media.Brushes.Transparent,
                StrokeThickness = 3,
                PointGeometrySize = 0
            };

            lineSeries.Stroke = new LinearGradientBrush(
                Color.FromRgb(255, 255, 255), Color.FromRgb(40, 137, 252), 0);

            CartesianChart.Series.Clear();

            CartesianChart.Series.Add(lineSeries);

            CartesianChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn,
                Labels = xValues
            };
            CartesianChart.AxisX.Add(axisX);

            CartesianChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                LabelFormatter = value => $"{value:N0}"
            };
            CartesianChart.AxisY.Add(axisY);
        }
        private void InitializePieChart(string xColumn, string yColumn)
        {
            // تجميع البيانات في Dictionary
            Dictionary<string, double> aggregatedData = new Dictionary<string, double>();

            foreach (DataRow row in dataTable.Rows)
            {
                string label = row[xColumn]?.ToString();
                if (string.IsNullOrWhiteSpace(label)) continue; // تجاهل القيم الفارغة

                if (double.TryParse(row[yColumn]?.ToString(), out double value))
                {
                    if (aggregatedData.ContainsKey(label))
                    {
                        aggregatedData[label] += value; // جمع القيم المكررة
                    }
                    else
                    {
                        aggregatedData[label] = value; // إدخال قيمة جديدة
                    }
                }
            }

            // مسح السلسلة السابقة
            PieChart.Series.Clear();

            // إنشاء السلسلة الجديدة باستخدام البيانات المجمعة
            foreach (var data in aggregatedData)
            {
                var pieSeries = new LiveCharts.Wpf.PieSeries
                {
                    Title = data.Key,
                    Values = new LiveCharts.ChartValues<double> { data.Value },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})"
                };

                // اختيار لون مخصص (اختياري)
                //pieSeries.Fill = new SolidColorBrush(Color.FromRgb(40, 137, 252));
                var random = new Random();
                pieSeries.Fill = new SolidColorBrush(Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));


                // إضافة السلسلة إلى الرسم البياني
                PieChart.Series.Add(pieSeries);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LabelSelection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // لإغلاق النافذة
        }

    }
}