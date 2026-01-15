using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfApp1.Variables;
using WpfApp1.DataSets;

namespace WpfApp1.ViewModels
{
   

    public sealed class DataSetsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DataSet> DataSets => DataSetManager.Instance.DataSets;

        private DataSet? _selectedDataSet;
        public DataSet? SelectedDataSet
        {
            get => _selectedDataSet;
            set
            {
                if (_selectedDataSet != value)
                {
                    _selectedDataSet = value;
                    DataSetManager.Instance.Current = value;
                    OnPropertyChanged();
                }
            }
        }

        public DataSetsViewModel()
        {
            // На случай, если хотите тестовые данные:
            if (DataSets.Count == 0)
            {
                DataSetManager.Instance.Add(new DataSet
                {
                    Name = "DataSet 1",
                    Author = "Автор 1",
                    Source = "Тест",
                    Description = "Первый тестовый датасет",
                    CreatedAt = DateTime.UtcNow
                });

                DataSetManager.Instance.Add(new DataSet
                {
                    Name = "DataSet 2",
                    Author = "Автор 2",
                    Source = "Тест",
                    Description = "Второй тестовый датасет",
                    CreatedAt = DateTime.UtcNow
                });
            }

            SelectedDataSet = DataSets.FirstOrDefault();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
