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
using WpfApp1.DataSets;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для DataSetsWindow.xaml
    /// </summary>
    public partial class DataSetsWindow : Window
    {
        private readonly DataSetsViewModel _vm = new DataSetsViewModel();

        public DataSetsWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ds = DataSetManager.Instance.Current;
            if (ds == null)
            {
                MessageBox.Show("Не выбран ни один DataSet.");
                return;
            }

            try
            {
                var editor = new DataSetEditorWindow(ds);
                editor.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

    }
}
