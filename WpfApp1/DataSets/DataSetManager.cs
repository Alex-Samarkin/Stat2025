using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WpfApp1.Variables;

namespace WpfApp1.DataSets
{
    

    public sealed class DataSetManager
    {
        private static readonly Lazy<DataSetManager> _instance =
            new(() => new DataSetManager());

        public static DataSetManager Instance => _instance.Value;

        // Коллекция для биндинга (ListBox, ComboBox, ItemsControl и т.п.)
        public ObservableCollection<DataSet> DataSets { get; } =
            new ObservableCollection<DataSet>();

        // Текущий выбранный датасет (можно биндинговать к SelectedItem)
        public DataSet? Current { get; set; }

        private DataSetManager() { }

        public void Add(DataSet dataSet)
        {
            if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

            DataSets.Add(dataSet);
            Current ??= dataSet;
        }

        public bool Remove(DataSet dataSet)
        {
            if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

            bool removed = DataSets.Remove(dataSet);
            if (removed && ReferenceEquals(Current, dataSet))
                Current = DataSets.FirstOrDefault();

            return removed;
        }

        public void Clear()
        {
            DataSets.Clear();
            Current = null;
        }

        public DataSet? GetById(int id)
        {
            return DataSets.FirstOrDefault(ds => ds.Id == id);
        }
    }

}
