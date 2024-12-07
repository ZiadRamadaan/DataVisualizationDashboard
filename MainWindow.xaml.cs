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

            // Populate ChartSelection with available chart types
            ChartSelection.ItemsSource = new List<string> { "Bar Chart", "Line Chart", "Pie Chart", "Scatter Plot", "All" };
            ChartSelection.SelectionChanged += ChartSelection_SelectionChanged; // Bind event
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
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

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

        private string selectedChartType; // Class-level variable to track the selected chart type

        private void ChartSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChartSelection.SelectedItem == null)
            {
                MessageBox.Show("Please select a Chart Type.");
                return;
            }

            selectedChartType = ChartSelection.SelectedItem.ToString(); // Store the selected chart type

        }
        private void PopulateDropdowns()
        {
            var numericColumns = new List<string>();
            var allColumns = new List<string>();

            // Separate numeric and non-numeric columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bool isNumeric = true;

                foreach (DataRow row in dataTable.Rows)
                {
                    if (!double.TryParse(row[column].ToString(), out _))
                    {
                        isNumeric = false;
                        break;
                    }
                }

                if (isNumeric)
                {
                    numericColumns.Add(column.ColumnName);
                }

                allColumns.Add(column.ColumnName);
            }

            // Assign to dropdowns
            LabelSelection.ItemsSource = allColumns; // All columns for labels
            ValueSelection.ItemsSource = numericColumns; // Only numeric columns for values
        }
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (LabelSelection.SelectedItem == null || ValueSelection.SelectedItem == null || string.IsNullOrEmpty(selectedChartType))
            {
                MessageBox.Show("Please select a Chart Type, Label column, and Value column.");
                return;
            }

            string xColumn = LabelSelection.SelectedItem.ToString();
            string yColumn = ValueSelection.SelectedItem.ToString();

            try
            {

                // Generate the selected chart type
                switch (selectedChartType)
                {
                    case "Bar Chart":
                        InitializeBarChart(xColumn, yColumn);
                        break;

                    case "Line Chart":
                        InitializeLineChart(xColumn, yColumn);
                        break;

                    case "Pie Chart":
                        InitializePieChart(xColumn, yColumn);
                        break;

                    case "Scatter Plot":
                        InitializeScatterPlot(xColumn, yColumn);
                        break;
                    case "All":
                        InitializeBarChart(xColumn, yColumn);
                        InitializeLineChart(xColumn, yColumn);
                        InitializePieChart(xColumn, yColumn);
                        InitializeScatterPlot(xColumn, yColumn);
                        break;

                    default:
                        MessageBox.Show("Invalid chart type selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InitializeBarChart(string xColumn, string yColumn)
        {
            // استخدام Dictionary لتجميع القيم حسب الفئة (المحور X)
            Dictionary<string, double> groupedData = new Dictionary<string, double>();

            // تجميع البيانات
            foreach (DataRow row in dataTable.Rows)
            {
                string label = row[xColumn]?.ToString(); // الحصول على الفئة (اسم العمود X)
                if (string.IsNullOrWhiteSpace(label)) continue; // تجاهل القيم الفارغة

                if (double.TryParse(row[yColumn]?.ToString(), out double value)) // تحويل القيمة
                {
                    if (groupedData.ContainsKey(label))
                    {
                        groupedData[label] += value; // تجميع القيم المكررة
                    }
                    else
                    {
                        groupedData[label] = value; // إضافة قيمة جديدة
                    }
                }
            }

            // ترتيب القيم (اختياري) لضمان عرض القيم بشكل منطقي
            var sortedData = groupedData.OrderBy(kv => kv.Key).ToList();

            // إنشاء قائمة القيم المجمعة للمحور X و Y
            List<string> xValues = sortedData.Select(kv => kv.Key).ToList(); // الفئات
            List<double> yValues = sortedData.Select(kv => kv.Value).ToList(); // القيم المجمعة

            // التأكد من تطابق عدد القيم بين المحورين X وY
            if (xValues.Count != yValues.Count)
            {
                MessageBox.Show("Error: Mismatch between X and Y values!", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // إنشاء ColumnSeries جديدة للرسم البياني
            var columnSeries = new LiveCharts.Wpf.ColumnSeries
            {
                Title = $"{xColumn} vs {yColumn}",
                Values = new LiveCharts.ChartValues<double>(yValues), // القيم Y
                DataLabels = true, // عرض القيم فوق الأعمدة
                LabelPoint = point => $"{point.Y:N0}", // تنسيق القيم
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ddf82")) // Set custom color
            };

            // مسح أي بيانات سابقة في BarChart
            BarChart.Series.Clear();

            // إضافة السلسلة الجديدة إلى BarChart
            BarChart.Series.Add(columnSeries);

            // تحديث المحور X بالعناوين
            BarChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn, // عنوان المحور
                Labels = xValues, // القيم المجمعة للمحور X
                Separator = new LiveCharts.Wpf.Separator // إضافة تباعد بين القيم
                {
                    Step = 1, // خطوة واحدة بين القيم
                    IsEnabled = false // إخفاء الخطوط بين القيم
                }
            };
            BarChart.AxisX.Add(axisX);

            // تحديث المحور Y (مع التنسيق الاختياري)
            BarChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                LabelFormatter = value => $"{value:N0}", // تنسيق القيم Y
                Separator = new LiveCharts.Wpf.Separator() // ضبط التباعد
                {
                    IsEnabled = true, // إخفاء الخطوط بين القيم
                    StrokeThickness = 0.5
                }
            };
            BarChart.AxisY.Add(axisY);
        }
        private void InitializeScatterPlot(string xColumn, string yColumn)
        {
            // Create lists to store the data for the X and Y axes
            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();

            // Extract values from the DataTable
            foreach (DataRow row in dataTable.Rows)
            {
                if (double.TryParse(row[xColumn].ToString(), out double xValue) &&
                    double.TryParse(row[yColumn].ToString(), out double yValue))
                {
                    xValues.Add(xValue);
                    yValues.Add(yValue);
                }
            }

            // Create a ScatterSeries for the scatter plot
            var scatterSeries = new LiveCharts.Wpf.ScatterSeries
            {
                Title = $"{yColumn} / {xColumn}",
                Values = new LiveCharts.ChartValues<LiveCharts.Defaults.ObservablePoint>(
                    xValues.Zip(yValues, (x, y) => new LiveCharts.Defaults.ObservablePoint(x, y))),
                // Scatter points will have a default shape, which is a circle
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#65f5fa")), // Customize the color
                Stroke = Brushes.Transparent,         // Customize the border of points
                StrokeThickness = 1             // Customize the border thickness
            };

            // Clear any previous data on the ScatterChart
            ScatterChart.Series.Clear();

            // Add the new series to the ScatterChart
            ScatterChart.Series.Add(scatterSeries);

            // Update the X-axis with appropriate formatting
            ScatterChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn,
                LabelFormatter = value => $"{value:N2}", // Format X-axis as numeric
                /* Separator = new LiveCharts.Wpf.Separator
                 {
                     IsEnabled = false // تعطيل الخطوط الفاصلة
                 }*/
            };
            ScatterChart.AxisX.Add(axisX);

            // Update the Y-axis with appropriate formatting
            ScatterChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                LabelFormatter = value => $"{value:N2}",// Format Y-axis as numeric
                /*Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0 // إزالة الخطوط الفاصلة لمحور Y
                }*/
            };
            ScatterChart.AxisY.Add(axisY);
        }
        private void InitializeLineChart(string xColumn, string yColumn)
        {
            // استخدام Dictionary لتجميع القيم حسب الفئات (المحور X)
            Dictionary<string, double> groupedData = new Dictionary<string, double>();

            // تجميع البيانات
            foreach (DataRow row in dataTable.Rows)
            {
                string label = row[xColumn]?.ToString(); // الحصول على الفئة (المحور X)
                if (string.IsNullOrWhiteSpace(label)) continue; // تجاهل القيم الفارغة

                if (double.TryParse(row[yColumn]?.ToString(), out double value)) // تحويل القيمة
                {
                    if (groupedData.ContainsKey(label))
                    {
                        groupedData[label] += value; // تجميع القيم المكررة
                    }
                    else
                    {
                        groupedData[label] = value; // إضافة قيمة جديدة
                    }
                }
            }

            // استخراج القيم المجمعة والمرتبة
            List<string> xValues = groupedData.Select(kv => kv.Key).ToList();
            List<double> yValues = groupedData.Select(kv => kv.Value).ToList();

            // إنشاء LineSeries جديدة
            var lineSeries = new LiveCharts.Wpf.LineSeries
            {
                Title = $"{xColumn} vs {yColumn}",
                Values = new LiveCharts.ChartValues<double>(yValues), // القيم Y
                Fill = System.Windows.Media.Brushes.Transparent, // عدم تلوين المساحة تحت الخط
                StrokeThickness = 3, // سمك الخط
                PointGeometrySize = 5 // حجم النقاط على الخط
            };

            // إعداد لون الخط 
            lineSeries.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00bf63"));

            // مسح البيانات القديمة من الرسم
            CartesianChart.Series.Clear();

            // إضافة السلسلة الجديدة
            CartesianChart.Series.Add(lineSeries);

            // إعداد المحور X
            CartesianChart.AxisX.Clear();
            var axisX = new LiveCharts.Wpf.Axis
            {
                Title = xColumn,
                Labels = xValues, // القيم المجمعة
                LabelsRotation = 45, // تدوير القيم لتحسين العرض
                Separator = new LiveCharts.Wpf.Separator
                {
                    Step = 1, // عرض كل القيم بشكل منتظم
                    IsEnabled = false // تعطيل الخطوط الفاصلة
                }
            };
            CartesianChart.AxisX.Add(axisX);

            // إعداد المحور Y مع تنسيق القيم
            CartesianChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                MinValue = 0,
                LabelFormatter = value => $"{value:N0}", // تنسيق القيم بأرقام صحيحة مع فاصلة
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0.5 // إزالة الخطوط الفاصلة لمحور Y
                }
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

            // الألوان المحددة
            string[] colors = { "#6237b6", "#4d5062", "#036d3a", "#383a49" };
            int colorIndex = 0;

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

                // اختيار لون من الألوان المحددة
                pieSeries.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[colorIndex]));

                // تحديث الفهرس للتنقل بين الألوان
                colorIndex = (colorIndex + 1) % colors.Length;

                // Set the Stroke to transparent
                pieSeries.Stroke = new SolidColorBrush(Colors.Transparent); // Transparent stroke

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
        private void PowerIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {

        }

        private void ChartSelection_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }
        private void RestoreInitialCharts()
        {
            BarChart.Series.Clear();
            BarChart.AxisX.Clear();
            BarChart.AxisY.Clear();
            PieChart.Series.Clear();
            CartesianChart.Series.Clear();
            CartesianChart.AxisX.Clear();
            CartesianChart.AxisY.Clear();
            ScatterChart.Series.Clear();
            ScatterChart.AxisX.Clear();
            ScatterChart.AxisY.Clear();
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Clear selected chart type and dropdown selections
            ChartSelection.SelectedItem = null;
            LabelSelection.SelectedItem = null;
            ValueSelection.SelectedItem = null;
            // Restore original charts setup
            RestoreInitialCharts();
        }

       /* private void RestoreInitialCharts()
        {
            // Reinitialize the line chart
            CartesianChart.Series.Clear();
            CartesianChart.AxisX.Clear();
            CartesianChart.AxisY.Clear();

            CartesianChart.Series.Add(new LiveCharts.Wpf.LineSeries
            {
                Values = new LiveCharts.ChartValues<double> { 50, 120, 110, 160, 150, 180, 120, 170, 165, 180 },
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                PointGeometrySize = 0,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00bf63"))
            });

            CartesianChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A3B2")),
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0,
                    Step = 2
                }
            });

            CartesianChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                MinValue = 40,
                MaxValue = 350,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A3B2")),
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0
                }
            });

            // Reinitialize the bar chart
            BarChart.Series.Clear();
            BarChart.AxisX.Clear();
            BarChart.AxisY.Clear();

            BarChart.Series.Add(new LiveCharts.Wpf.ColumnSeries
            {
                Values = new LiveCharts.ChartValues<double> { 6.5, 3, 6, 5 },
                MaxColumnWidth = 20,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ddf82"))
            });

            BarChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A3B2")),
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0,
                    Step = 1
                }
            });

            BarChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                MinValue = 0,
                MaxValue = 8,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A3B2")),
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 0,
                    Step = 2
                }
            });

            // Reinitialize the pie chart
            PieChart.Series.Clear();
            PieChart.Series.Add(new LiveCharts.Wpf.PieSeries
            {
                Values = new LiveCharts.ChartValues<double> { 3 },
                DataLabels = true,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383a49")),
                Foreground = Brushes.White,
                Stroke = Brushes.Transparent
            });
            PieChart.Series.Add(new LiveCharts.Wpf.PieSeries
            {
                Values = new LiveCharts.ChartValues<double> { 4 },
                DataLabels = true,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4d5062")),
                Foreground = Brushes.White,
                Stroke = Brushes.Transparent
            });
            PieChart.Series.Add(new LiveCharts.Wpf.PieSeries
            {
                Values = new LiveCharts.ChartValues<double> { 6 },
                DataLabels = true,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#036d3a")),
                Foreground = Brushes.White,
                Stroke = Brushes.Transparent
            });
            PieChart.Series.Add(new LiveCharts.Wpf.PieSeries
            {
                Values = new LiveCharts.ChartValues<double> { 2 },
                DataLabels = true,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6237b6")),
                Foreground = Brushes.White,
                Stroke = Brushes.Transparent
            });

            // Reinitialize the scatter chart
            ScatterChart.Series.Clear();
            ScatterChart.AxisX.Clear();
            ScatterChart.AxisY.Clear();

        }*/

    }
}