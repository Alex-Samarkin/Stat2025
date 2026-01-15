using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Variables;
using System.IO;
using System.Diagnostics;
using WpfApp1.DataSets;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 95% от экрана
            Width = screenWidth * 0.95;
            Height = screenHeight*0.85;

            // центрирование
            Left = (screenWidth - Width) / 2;
            // Top = (screenHeight - Height) / 2;
            Top = 5;
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            const int rowCount = 400;

            // 1. Описываем переменные (колонки)
            IVariable vId = new IntegerVariable("Id", description: "Row id");
            IVariable vValue = new NumericVariable("Value", precision: 18, scale: 4, description: "Random value");
            IVariable vFlag = new BoolVariable("IsActive", description: "Random flag");
            IVariable vName = new StringVariable("Name", description: "Synthetic name");
            IVariable vDate = new DateVariable("Date", description: "Sequential date");

            var factory = new VectorFactory(seed: 42);

            // 2. Генерируем данные для каждой колонки
            var vecId = factory.CreateFilled(
                vId, rowCount, VectorFillMode.Sequence,
                start: 1, step: 1); // 1,2,3,...

            var vecValue = factory.CreateFilled(
                vValue, rowCount, VectorFillMode.Random,
                min: -100m, max: 100m); // случайные decimal

            var vecFlag = factory.CreateFilled(
                vFlag, rowCount, VectorFillMode.Random); // случайные bool

            var vecName = factory.CreateFilled(
                vName, rowCount, VectorFillMode.Sequence,
                start: "item_0"); // item_0, item_1, ...

            var vecDate = factory.CreateFilled(
                vDate, rowCount, VectorFillMode.Sequence,
                start: new DateTime(2025, 1, 1),
                step: TimeSpan.FromDays(1)); // последовательность дат

            // 3. Собираем DataSet
            var dataSet = new DataSet(new[] { vecId, vecValue, vecFlag, vecName, vecDate });

            // 4. Сохранение в CSV + JSON-метаданные
            var csvPath = System.IO.Path.Combine(Environment.CurrentDirectory, "demo.csv");
            dataSet.SaveToCsvWithMetadata(csvPath); // создаст demo.csv и demo.json[web:294][web:295]

            // 5. Сохранение в Arrow IPC (.arrow)
            var arrowPath = System.IO.Path.Combine(Environment.CurrentDirectory, "demo.arrow");
            dataSet.SaveAsArrowFile(arrowPath); // ArrowFileWriter + RecordBatch[web:10][web:21]

            // 6. Сохранение в Parquet через ParquetSharp.Arrow
            var parquetPath = System.IO.Path.Combine(Environment.CurrentDirectory, "demo.parquet");
            dataSet.SaveAsParquet(parquetPath); // FileWriter(path, schema, props, arrowProps)[web:247][web:243]

            // 7. Пример загрузки обратно (опционально)

            // 7.1 CSV+JSON
            var dataSetFromCsv = DataSet.LoadFromCsvWithMetadata(csvPath);

            // 7.2 Arrow
            var dataSetFromArrow = DataSet.LoadFromArrowFile(arrowPath);

            // 7.3 Parquet (все батчи)
            var dataSetFromParquet = DataSet.LoadFromParquet(parquetPath);

            // Можно, например, вывести количество строк:
            Debug.WriteLine($"Original rows: {dataSet.RowCount}");
            Debug.WriteLine($"CSV rows:      {dataSetFromCsv.RowCount}");
            Debug.WriteLine($"Arrow rows:    {dataSetFromArrow.RowCount}");
            Debug.WriteLine($"Parquet rows:  {dataSetFromParquet.RowCount}");

            DataSetManager.Instance.Add(dataSet);
            DataSetManager.Instance.Add(dataSetFromCsv);
            DataSetManager.Instance.Add(dataSetFromArrow);
            DataSetManager.Instance.Add(dataSetFromParquet);
        }

        private void RibbonButton_Click_1(object sender, RoutedEventArgs e)
        {
            var f = new DataSetsWindow();
            f.ShowDialog();
        }
    }
}