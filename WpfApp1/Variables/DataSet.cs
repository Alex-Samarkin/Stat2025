using System;
using System.Collections.Generic;
using System.Text;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using ParquetSharp;
using ParquetSharp.Arrow;
using ParquetSharp.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using System.Text.Json;

using Encoding = System.Text.Encoding;
using System.Windows;


namespace WpfApp1.Variables
{


    public sealed class DataSet
    {
        private readonly List<Vector> _columns = new();

        public IReadOnlyList<Vector> Columns => _columns;
        public int RowCount => _columns.Count == 0 ? 0 : _columns[0].Length;

        /// <summary>
        /// metadata
        /// </summary>

        public string? Author { get; set; } = String.Empty;
        public string? Source { get; set; } = String.Empty;
        public string? Description { get; set; } = String.Empty;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;


        /// <summary>
        /// for DataSetManager
        /// </summary>

        private static int _nextId = 1;
        public int Id { get; } = _nextId++;

        private static string _name = "Dataset";
        public string? Name { get; set; } = _name+_nextId.ToString();

        public DataSet() { CreatedAt = DateTime.UtcNow; }

        public DataSet(IEnumerable<Vector> columns)
        {
            var cols = columns.ToList();
            if (cols.Count > 0)
            {
                int len = cols[0].Length;
                if (cols.Any(c => c.Length != len))
                    throw new InvalidOperationException("All columns must have same length.");
            }
            _columns.AddRange(cols);
            CreatedAt = DateTime.UtcNow;
        }

        public void AddColumn(Vector vector)
        {
            if (_columns.Count > 0 && vector.Length != RowCount)
                throw new InvalidOperationException("New column must have same length.");
            _columns.Add(vector);
        }

        // =====================================================
        // 1. Arrow: RecordBatch
        // =====================================================
        /// <summary>
        /// Assembly metadata
        /// </summary>
        /// <returns>metadata dictionary</returns>
        private IDictionary<string, string> BuildSchemaMetadata()
        {
            var md = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(Name))
                md["dataset.name"] = Name!;

            if (!string.IsNullOrWhiteSpace(Author))
                md["dataset.author"] = Author!;
            if (!string.IsNullOrWhiteSpace(Source))
                md["dataset.source"] = Source!;
            if (!string.IsNullOrWhiteSpace(Description))
                md["dataset.description"] = Description!;
            if (CreatedAt.HasValue)
                md["dataset.created_at"] = CreatedAt.Value.ToString("o"); // ISO 8601

            return md;
        }


        public RecordBatch ToRecordBatch()
        {
            var fields = _columns.Select(c => c.Variable.ArrowField).ToList();
            var arrays = _columns.Select(c => c.ArrowArray).ToList();
            var metadata = BuildSchemaMetadata();
            // var schema = new Schema(fields);
            // var schema = new Schema(fields, Enumerable.Empty<KeyValuePair<string, string>>());
            var schema = new Schema(fields, metadata);
            return new RecordBatch(schema, arrays, RowCount);
        }

        public static DataSet FromRecordBatch(RecordBatch batch, Func<Field, IVariable> variableFactory)
        {
            var vectors = new List<Vector>();

            for (int i = 0; i < batch.ColumnCount; i++)
            {
                var field = batch.Schema.GetFieldByIndex(i);
                var array = batch.Column(i);
                var variable = variableFactory(field);
                var vector = new Vector(variable, array);
                vectors.Add(vector);
            }

            var ds = new DataSet(vectors);

            var md = batch.Schema.Metadata ?? new Dictionary<string, string>();

            if (md.TryGetValue("dataset.name", out var name)) ds.Name = name;
            if (md.TryGetValue("dataset.author", out var author)) ds.Author = author;
            if (md.TryGetValue("dataset.source", out var source)) ds.Source = source;
            if (md.TryGetValue("dataset.description", out var desc)) ds.Description = desc;
            if (md.TryGetValue("dataset.created_at", out var created))
                ds.CreatedAt = DateTime.Parse(created, null, System.Globalization.DateTimeStyles.RoundtripKind);

            return ds;
        }

        // =====================================================
        // 2. DataTable (System.Data) для биндинга к DataGrid
        // =====================================================

        public DataTable ToDataTable()
        {
            /*
            var dt = new DataTable("DataSet");


            foreach (var col in _columns)
            {
                var clrType = col.Variable.ClrType;

                // Если Nullable<T> -> берём T
                if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    clrType = Nullable.GetUnderlyingType(clrType)!;

                dt.Columns.Add(col.Variable.Name, clrType);
            }


            for (int row = 0; row < RowCount; row++)
            {
                var values = new object?[_columns.Count];
                for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
                    values[colIndex] = _columns[colIndex].GetValue(row);
                dt.Rows.Add(values);
            }
            */

            var dt = new DataTable("DataSet");

    // Добавляем колонки
    foreach (var col in _columns)
    {
        var clrType = col.Variable.ClrType;

        if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
            clrType = Nullable.GetUnderlyingType(clrType)!;

        dt.Columns.Add(col.Variable.Name, clrType);
    }

    // Добавляем строки
    for (int row = 0; row < RowCount; row++)
    {
        var values = new object?[_columns.Count];
        for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
        {
            var value = _columns[colIndex].GetValue(row);
            values[colIndex] = value ?? DBNull.Value; // Ключевое исправление
        }
        dt.Rows.Add(values);
    }

            return dt;
        }

        public void UpdateFromDataTable(DataTable table)
        {
            if (table.Columns.Count != _columns.Count)
                throw new InvalidOperationException("Column count mismatch.");

            if (table.Rows.Count != RowCount)
                throw new InvalidOperationException("Row count mismatch.");

            for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
            {
                var vector = _columns[colIndex];
                for (int row = 0; row < RowCount; row++)
                {
                   // vector.SetValue(row, table.Rows[row][colIndex]);
                   object? rawValue = table.Rows[row][colIndex];
object? value = (rawValue == DBNull.Value) ? null : rawValue;
vector.SetValue(row, value);
                }

                vector.RebuildArrowArray();
            }
        }

        // =====================================================
        // 3. CSV + JSON метаданные
        // =====================================================

        private sealed class VariableMetadataDto
        {
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
            public string? Unit { get; set; }
            public string? Formula { get; set; }
            public string VariableType { get; set; } = null!;
            public Dictionary<string, string>? Categories { get; set; }
            public List<string>? OrderedCategories { get; set; }
            public int? Precision { get; set; }
            public int? Scale { get; set; }
            public string? TimeUnit { get; set; }
            public string? TimeZone { get; set; }
        }

        private sealed class DataSetMetadataDto
        {
            public string? Name { get; set; }
            public string? Author { get; set; }
            public string? Source { get; set; }
            public string? Description { get; set; }
            public DateTime? CreatedAt { get; set; }
            public List<VariableMetadataDto> Columns { get; set; } = new();
        }

        public void SaveToCsvWithMetadata(string csvPath)
        {
            try
            {
                using (var stream = File.Open(csvPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine(string.Join(";", _columns.Select(c => c.Variable.Name)));

                    for (int row = 0; row < RowCount; row++)
                    {
                        var cells = new string[_columns.Count];
                        for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
                        {
                            var v = _columns[colIndex].GetValue(row);
                            cells[colIndex] = FormatForCsv(v);
                        }

                        writer.WriteLine(string.Join(";", cells));
                    }
                }

                var meta = new DataSetMetadataDto
                {
                    Name = this.Name,
                    Author = this.Author,
                    Source = this.Source,
                    Description = this.Description,
                    CreatedAt = this.CreatedAt,
                    Columns = _columns.Select(c => ToMetadataDto(c.Variable)).ToList()
                };

                var jsonPath = Path.ChangeExtension(csvPath, ".json");
                var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(jsonPath, json, Encoding.UTF8);
            }
            catch
            {
                MessageBox.Show("Ошибка записи CSv файла.Проверьте, не открыт ли ваш файл в другой программе");
            }
        }

        public static DataSet LoadFromCsvWithMetadata(string csvPath)
        {
            var jsonPath = Path.ChangeExtension(csvPath, ".json");
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Metadata JSON not found", jsonPath);

            var json = File.ReadAllText(jsonPath, Encoding.UTF8);
            var meta = JsonSerializer.Deserialize<DataSetMetadataDto>(json)
                       ?? throw new InvalidOperationException("Invalid metadata JSON.");
            try
            {
                var lines = File.ReadAllLines(csvPath, Encoding.UTF8);

                if (lines.Length == 0)
                    return new DataSet();

                var header = lines[0].Split(';');
                if (header.Length != meta.Columns.Count)
                    throw new InvalidOperationException("Header/metadata column count mismatch.");

                var variables = meta.Columns.Select(FromMetadataDto).ToList();
                int columnCount = variables.Count;
                int rowCount = lines.Length - 1;

                var valuesPerColumn = new object?[columnCount][];
                for (int c = 0; c < columnCount; c++)
                    valuesPerColumn[c] = new object?[rowCount];

                for (int row = 0; row < rowCount; row++)
                {
                    var parts = lines[row + 1].Split(';');
                    for (int c = 0; c < columnCount; c++)
                    {
                        var text = c < parts.Length ? parts[c] : string.Empty;
                        valuesPerColumn[c][row] = ParseFromCsv(variables[c], text);
                    }
                }

                var vectors = new List<Vector>();
                for (int c = 0; c < columnCount; c++)
                {
                    var array = VectorHelper.BuildArrayFromValues(variables[c], valuesPerColumn[c]);
                    vectors.Add(new Vector(variables[c], array));
                }

                return new DataSet(vectors)
                {
                    Name = meta.Name,
                    Author = meta.Author,
                    Source = meta.Source,
                    Description = meta.Description,
                    CreatedAt = meta.CreatedAt
                };
            }
            catch
            {
                MessageBox.Show("Ошибка чтения CSV. Создан пустой датасет.");
                return new DataSet();
            }
        }

        private static string FormatForCsv(object? value)
        {
            if (value is null) return string.Empty;
            return value switch
            {
                DateTime dt => dt.ToString("o"),
                IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static object? ParseFromCsv(IVariable variable, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var ci = System.Globalization.CultureInfo.InvariantCulture;

            return variable switch
            {
                NumericVariable => decimal.Parse(text, ci),
                IntegerVariable => int.Parse(text, ci),
                CategoryVariable => int.Parse(text, ci),
                OrdinalCategoryVariable => int.Parse(text, ci),
                BoolVariable => bool.Parse(text),
                StringVariable => text,
                DateVariable => DateTime.Parse(text, ci, System.Globalization.DateTimeStyles.AssumeUniversal).Date,
                DateTimeVariable => DateTime.Parse(text, ci, System.Globalization.DateTimeStyles.RoundtripKind),
                _ => text
            };
        }

        private static VariableMetadataDto ToMetadataDto(IVariable v)
        {
            var dto = new VariableMetadataDto
            {
                Name = v.Name,
                Description = v.Description,
                Unit = v.Unit,
                Formula = v.Formula,
                VariableType = v.GetType().Name
            };

            switch (v)
            {
                case NumericVariable nv:
                    dto.Precision = nv.Precision;
                    dto.Scale = nv.Scale;
                    break;
                case CategoryVariable cv:
                    dto.Categories = cv.Categories?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
                    break;
                case OrdinalCategoryVariable ov:
                    dto.OrderedCategories = ov.OrderedCategories.ToList();
                    break;
                case DateTimeVariable dtv:
                    dto.TimeUnit = dtv.TimeUnit.ToString();
                    dto.TimeZone = dtv.TimeZone;
                    break;
            }

            return dto;
        }

        private static IVariable FromMetadataDto(VariableMetadataDto dto)
        {
            return dto.VariableType switch
            {
                nameof(NumericVariable) =>
                    new NumericVariable(dto.Name,
                        dto.Precision ?? 18,
                        dto.Scale ?? 6,
                        dto.Description,
                        dto.Unit,
                        dto.Formula),

                nameof(IntegerVariable) =>
                    new IntegerVariable(dto.Name, dto.Description, dto.Unit, dto.Formula),

                nameof(BoolVariable) =>
                    new BoolVariable(dto.Name, dto.Description, dto.Unit, dto.Formula),

                nameof(StringVariable) =>
                    new StringVariable(dto.Name, dto.Description, dto.Unit, dto.Formula),

                nameof(CategoryVariable) =>
                    new CategoryVariable(dto.Name,
                        dto.Categories?.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value)
                        ?? new Dictionary<int, string>(),
                        dto.Description,
                        dto.Unit,
                        dto.Formula),

                nameof(OrdinalCategoryVariable) =>
                    new OrdinalCategoryVariable(dto.Name,
                        dto.OrderedCategories ?? new List<string>(),
                        dto.Description,
                        dto.Unit,
                        dto.Formula),

                nameof(DateVariable) =>
                    new DateVariable(dto.Name, dto.Description, dto.Unit, dto.Formula),

                nameof(DateTimeVariable) =>
                    new DateTimeVariable(dto.Name,
                        Enum.TryParse<Apache.Arrow.Types.TimeUnit>(dto.TimeUnit, out var tu) ? tu : Apache.Arrow.Types.TimeUnit.Millisecond,
                        dto.TimeZone,
                        dto.Description,
                        dto.Unit,
                        dto.Formula),

                _ => throw new NotSupportedException($"Unknown VariableType: {dto.VariableType}")
            };
        }

        // =====================================================
        // 4. Arrow IPC (файл .arrow)
        // =====================================================

        public void SaveAsArrowFile(string path)
        {
            var batch = ToRecordBatch();

            using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
            using var writer = new ArrowFileWriter(stream, batch.Schema);

            writer.WriteRecordBatchAsync(batch).GetAwaiter().GetResult();
            writer.WriteEndAsync().GetAwaiter().GetResult(); // завершить файл[web:21][web:6]
        }

        public static DataSet LoadFromArrowFile(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new ArrowFileReader(stream);

            var batches = new List<RecordBatch>();
            RecordBatch? batch;
            while ((batch = reader.ReadNextRecordBatch()) != null)
                batches.Add(batch);

            if (batches.Count == 0)
                return new DataSet();

            IVariable VariableFactory(Field f) => VariableFromField(f);

            return FromRecordBatch(batches[0], VariableFactory);
        }

        private static IVariable VariableFromField(Field f)
        {
            var md = f.Metadata ?? new Dictionary<string, string>();

            md.TryGetValue("description", out var description);
            md.TryGetValue("unit", out var unit);
            md.TryGetValue("formula", out var formula);

            switch (f.DataType)
            {
                case Decimal128Type dec:
                    return new NumericVariable(f.Name, dec.Precision, dec.Scale, description, unit, formula);

                case Int32Type:
                    if (md.TryGetValue("logical_type", out var logical) && logical == "ordinal_category")
                    {
                        md.TryGetValue("ordered_categories", out var jsonOrd);
                        var list = jsonOrd is null
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(jsonOrd) ?? new List<string>();
                        return new OrdinalCategoryVariable(f.Name, list, description, unit, formula);
                    }
                    if (md.TryGetValue("logical_type", out var logical2) && logical2 == "category")
                    {
                        md.TryGetValue("categories", out var jsonCat);
                        var dict = jsonCat is null
                            ? new Dictionary<int, string>()
                            : JsonSerializer.Deserialize<Dictionary<int, string>>(jsonCat)
                              ?? new Dictionary<int, string>();
                        return new CategoryVariable(f.Name, dict, description, unit, formula);
                    }
                    return new IntegerVariable(f.Name, description, unit, formula);

                case BooleanType:
                    return new BoolVariable(f.Name, description, unit, formula);

                case StringType:
                    return new StringVariable(f.Name, description, unit, formula);

                case Date32Type:
                    return new DateVariable(f.Name, description, unit, formula);

                case TimestampType ts:
                    return new DateTimeVariable(f.Name, ts.Unit, ts.Timezone, description, unit, formula);

                default:
                    return new StringVariable(f.Name, description, unit, formula);
            }
        }

        // =====================================================
        // 5. Parquet через ParquetSharp.Arrow
        // =====================================================

        public void SaveAsParquet(string path)
        {
            var batch = ToRecordBatch();
            var schema = batch.Schema;

            using var writerPropsBuilder = new WriterPropertiesBuilder();
            using var writerProps = writerPropsBuilder
                .Compression(Compression.Snappy)
                .Build();

            using var arrowPropsBuilder = new ArrowWriterPropertiesBuilder();
            using var arrowProps = arrowPropsBuilder
                .StoreSchema() // сохраняет Arrow-схему в метаданных Parquet[web:247]
                .Build();

            using var fileWriter = new ParquetSharp.Arrow.FileWriter(
                path,
                schema,          // именно Apache.Arrow.Schema
                writerProps,
                arrowProps);

            fileWriter.WriteRecordBatch(batch);   // один батч -> один row group
            fileWriter.Close();
        }


        public static DataSet FromTableBatches(
    IReadOnlyList<RecordBatch> batches,
    Func<Field, IVariable> variableFactory)
        {
            if (batches == null || batches.Count == 0)
                return new DataSet();

            var first = batches[0];
            var schema = first.Schema;
            int fieldCount = schema.FieldsList.Count;

            // все батчи должны иметь одинаковую схему
            if (batches.Any(b => !b.Schema.Equals(schema)))
                throw new InvalidOperationException("All batches must have the same schema.");

            // создаём переменные по схеме
            var variables = schema.FieldsList.Select(variableFactory).ToList();

            // считаем общее число строк
            int totalRows = batches.Sum(b => b.Length);

            // буферы значений по колонкам
            var valuesPerColumn = new object?[fieldCount][];
            for (int col = 0; col < fieldCount; col++)
                valuesPerColumn[col] = new object?[totalRows];

            // заполняем по батчам
            int offset = 0;
            foreach (var batch in batches)
            {
                for (int col = 0; col < fieldCount; col++)
                {
                    var array = batch.Column(col);
                    var tmpVector = new Vector(variables[col], array);

                    for (int i = 0; i < batch.Length; i++)
                        valuesPerColumn[col][offset + i] = tmpVector.GetValue(i);
                }

                offset += batch.Length;
            }

            // строим вектора
            var vectors = new List<Vector>();
            for (int col = 0; col < fieldCount; col++)
            {
                var array = VectorHelper.BuildArrayFromValues(variables[col], valuesPerColumn[col]);
                vectors.Add(new Vector(variables[col], array));
            }

            return new DataSet(vectors);
        }


        public static DataSet LoadFromParquet(string path)
        {
            using var fileReader = new ParquetSharp.Arrow.FileReader(path);
            using var batchReader = fileReader.GetRecordBatchReader(); // IArrowArrayStream

            var batches = new List<RecordBatch>();

            while (true)
            {
                // Асинхронный вызов, разворачиваем в синхронном контексте
                var batch = batchReader.ReadNextRecordBatchAsync(default)
                                       .GetAwaiter()
                                       .GetResult();

                if (batch == null)
                    break;

                batches.Add(batch);
            }

            if (batches.Count == 0)
                return new DataSet();

            IVariable VariableFactory(Field f) => VariableFromField(f);

            // Если есть FromTableBatches, используем его; иначе можно взять первый батч
            return FromTableBatches(batches, VariableFactory);
        }






        // =====================================================
        // 6. Редактирование метаданных колонок
        // =====================================================

        public void RenameColumn(string oldName, string newName)
        {
            var col = _columns.FirstOrDefault(c => c.Variable.Name == oldName)
                      ?? throw new ArgumentException($"Column '{oldName}' not found.");

            var v = col.Variable;
            IVariable newVar = v switch
            {
                NumericVariable nv =>
                    new NumericVariable(newName, nv.Precision, nv.Scale, nv.Description, nv.Unit, nv.Formula),

                IntegerVariable iv =>
                    new IntegerVariable(newName, iv.Description, iv.Unit, iv.Formula),

                BoolVariable bv =>
                    new BoolVariable(newName, bv.Description, bv.Unit, bv.Formula),

                StringVariable sv =>
                    new StringVariable(newName, sv.Description, sv.Unit, sv.Formula),

                CategoryVariable cv =>
                    new CategoryVariable(newName, cv.Categories, cv.Description, cv.Unit, cv.Formula),

                OrdinalCategoryVariable ov =>
                    new OrdinalCategoryVariable(newName, ov.OrderedCategories, ov.Description, ov.Unit, ov.Formula),

                DateVariable dv =>
                    new DateVariable(newName, dv.Description, dv.Unit, dv.Formula),

                DateTimeVariable dtv =>
                    new DateTimeVariable(newName, dtv.TimeUnit, dtv.TimeZone, dtv.Description, dtv.Unit, dtv.Formula),

                _ => throw new NotSupportedException($"Unsupported variable type: {v.GetType().Name}")
            };

            var newVector = new Vector(newVar, col.ArrowArray);
            int idx = _columns.IndexOf(col);
            _columns[idx] = newVector;
        }

        public void UpdateDescription(string name, string? newDescription)
        {
            UpdateMetadata(name, (old, desc, unit, formula) => (desc: newDescription, unit, formula));
        }

        public void UpdateUnit(string name, string? newUnit)
        {
            UpdateMetadata(name, (old, desc, unit, formula) => (desc, unit: newUnit, formula));
        }

        public void UpdateFormula(string name, string? newFormula)
        {
            UpdateMetadata(name, (old, desc, unit, formula) => (desc, unit, formula: newFormula));
        }

        private void UpdateMetadata(
            string name,
            Func<IVariable, string?, string?, string?, (string? desc, string? unit, string? formula)> mutate)
        {
            var col = _columns.FirstOrDefault(c => c.Variable.Name == name)
                      ?? throw new ArgumentException($"Column '{name}' not found.");

            var v = col.Variable;
            var (newDesc, newUnit, newFormula) = mutate(v, v.Description, v.Unit, v.Formula);

            IVariable newVar = v switch
            {
                NumericVariable nv =>
                    new NumericVariable(nv.Name, nv.Precision, nv.Scale, newDesc, newUnit, newFormula),

                IntegerVariable iv =>
                    new IntegerVariable(iv.Name, newDesc, newUnit, newFormula),

                BoolVariable bv =>
                    new BoolVariable(bv.Name, newDesc, newUnit, newFormula),

                StringVariable sv =>
                    new StringVariable(sv.Name, newDesc, newUnit, newFormula),

                CategoryVariable cv =>
                    new CategoryVariable(cv.Name, cv.Categories, newDesc, newUnit, newFormula),

                OrdinalCategoryVariable ov =>
                    new OrdinalCategoryVariable(ov.Name, ov.OrderedCategories, newDesc, newUnit, newFormula),

                DateVariable dv =>
                    new DateVariable(dv.Name, newDesc, newUnit, newFormula),

                DateTimeVariable dtv =>
                    new DateTimeVariable(dtv.Name, dtv.TimeUnit, dtv.TimeZone, newDesc, newUnit, newFormula),

                _ => throw new NotSupportedException($"Unsupported variable type: {v.GetType().Name}")
            };

            var newVector = new Vector(newVar, col.ArrowArray);
            int idx = _columns.IndexOf(col);
            _columns[idx] = newVector;
        }
    }

}
