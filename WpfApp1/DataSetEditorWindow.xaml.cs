using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.ViewModels;
using WpfApp1.Variables;
using WpfApp1.DataSets;

using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;

namespace WpfApp1
{
    public partial class DataSetEditorWindow : Window
    {
        private readonly DataSetEditorViewModel _vm;

        public DataSetEditorWindow(WpfApp1.Variables.DataSet dataSet)
        {
            InitializeComponent();
            _vm = new DataSetEditorViewModel(dataSet);
            DataContext = _vm;
        }

        private void SaveMetadata_Click(object sender, RoutedEventArgs e)
        {
            _vm.SaveMetadata();
            MessageBox.Show("Метаданные сохранены.");
        }

        private void CancelMetadata_Click(object sender, RoutedEventArgs e)
        {
            _vm.CancelMetadata();
            MessageBox.Show("Изменения метаданных отменены.");
        }

        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            // DataGrid уже привязан к _vm.Table, достаточно применить к оригиналу
            _vm.SaveData();
            MessageBox.Show("Данные сохранены в DataSet.");
        }

        private void CancelData_Click(object sender, RoutedEventArgs e)
        {
            _vm.CancelData();
            MessageBox.Show("Изменения данных отменены.");
        }

        // Можно добавить кнопку "Сохранить в файл..."
        private void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Сохранить DataSet",
                Filter = "CSV с метаданными (*.csv)|*.csv|Arrow (*.arrow)|*.arrow|Parquet (*.parquet)|*.parquet"
            };

            if (dlg.ShowDialog() != true)
                return;

            string path = dlg.FileName;
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            var dataSet = GetOriginalDataSet(); // доступ к _original из VM

            switch (ext)
            {
                case ".csv":
                    dataSet.SaveToCsvWithMetadata(path);
                    break;
                case ".arrow":
                    dataSet.SaveAsArrowFile(path);
                    break;
                case ".parquet":
                    dataSet.SaveAsParquet(path);
                    break;
            }

            MessageBox.Show("DataSet сохранён в файл.");
        }

        private WpfApp1.Variables.DataSet GetOriginalDataSet()
        {
            // если нужно, добавьте в VM публичное свойство Original
            return typeof(DataSetEditorViewModel)
                .GetField("_original", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_vm) as WpfApp1.Variables.DataSet ?? throw new InvalidOperationException();
        }
    }
}
