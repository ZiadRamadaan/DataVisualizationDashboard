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
                LabelPoint = point => $"{point.Y:N0}" // تنسيق القيم
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
            };
            BarChart.AxisY.Add(axisY);
        }
        private void InitializeRowChart(string xColumn, string yColumn)
        {
            Dictionary<string, double> groupedData = new Dictionary<string, double>();

            // تجميع البيانات
            foreach (DataRow row in dataTable.Rows)
            {
                string label = row[xColumn].ToString(); // الحصول على الفئة (اسم العمود X)
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

            // إنشاء قائمة من القيم المجمعة للمحور X و Y
            List<string> xValues = groupedData.Keys.ToList(); // الفئات
            List<double> yValues = groupedData.Values.ToList(); // القيم المجمعة


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

            // إعداد لون الخط باستخدام تدرج لوني
            lineSeries.Stroke = new LinearGradientBrush(
                Color.FromRgb(40, 137, 252), // اللون الأول
                Color.FromRgb(0, 76, 153),   // اللون الثاني
                45);                         // زاوية التدرج

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
                    IsEnabled = true // تعطيل الخطوط الفاصلة
                }
            };
            CartesianChart.AxisX.Add(axisX);

            // إعداد المحور Y مع تنسيق القيم
            CartesianChart.AxisY.Clear();
            var axisY = new LiveCharts.Wpf.Axis
            {
                Title = yColumn,
                MinValue = 0 ,
                LabelFormatter = value => $"{value:N0}", // تنسيق القيم بأرقام صحيحة مع فاصلة
                Separator = new LiveCharts.Wpf.Separator
                {
                    StrokeThickness = 1 // إزالة الخطوط الفاصلة لمحور Y
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
        
    }
}