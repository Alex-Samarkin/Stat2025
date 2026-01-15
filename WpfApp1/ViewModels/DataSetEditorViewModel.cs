using System;
using System.Collections.Generic;
using System.Text;

using System;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

using WpfApp1.Variables;
using WpfApp1.DataSets;

namespace WpfApp1.ViewModels
{
   

    public sealed class DataSetEditorViewModel : INotifyPropertyChanged
    {
        private readonly WpfApp1.Variables.DataSet _original;

        // Редактируемые метаданные (копия)
        private string? _name;
        private string? _author;
        private string? _source;
        private string? _description;
        private DateTime? _createdAt;

        public string? Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string? Author
        {
            get => _author;
            set { _author = value; OnPropertyChanged(); }
        }

        public string? Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime? CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        // Редактируемые данные
        public DataTable Table { get; private set; }

        // Для колонок
        public IReadOnlyList<Vector> Columns => _original.Columns;
        private Vector? _selectedColumn;
        public Vector? SelectedColumn
        {
            get => _selectedColumn;
            set { _selectedColumn = value; OnPropertyChanged(); }
        }

        public DataSetEditorViewModel(WpfApp1.Variables.DataSet original)
        {
            _original = original ?? throw new ArgumentNullException(nameof(original));
            LoadMetadataFromOriginal();
            LoadTableFromOriginal();
            SelectedColumn = Columns.FirstOrDefault();
        }

        private void LoadMetadataFromOriginal()
        {
            Name = _original.Name;
            Author = _original.Author;
            Source = _original.Source;
            Description = _original.Description;
            CreatedAt = _original.CreatedAt;
        }

        private void LoadTableFromOriginal()
        {
            Table = _original.ToDataTable();
            OnPropertyChanged(nameof(Table));
        }

        // --- Метаданные ---

        public void SaveMetadata()
        {
            _original.Name = Name;
            _original.Author = Author;
            _original.Source = Source;
            _original.Description = Description;
            _original.CreatedAt = CreatedAt;
        }

        public void CancelMetadata()
        {
            LoadMetadataFromOriginal();
        }

        // --- Данные ---

        public void SaveData()
        {
            _original.UpdateFromDataTable(Table);
        }

        public void CancelData()
        {
            LoadTableFromOriginal();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
