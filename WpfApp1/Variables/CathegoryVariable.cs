using Apache.Arrow;
using Apache.Arrow.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public sealed class CategoryVariable : VariableBase
    {
        public IReadOnlyDictionary<int, string> Categories { get; }

        public CategoryVariable(
            string name,
            IReadOnlyDictionary<int, string> categories,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
            Categories = categories;
        }

        // В данных храним код категории
        public override Type ClrType => typeof(int?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            // Физически Int32, логически "категория"
            var dataType = Int32Type.Default; 

        var metadata = new Dictionary<string, string>
        {
            // словарь код -> метка
            ["categories"] =
                System.Text.Json.JsonSerializer.Serialize(Categories)
        };

            if (!string.IsNullOrWhiteSpace(Description))
                metadata["description"] = Description!;
            if (!string.IsNullOrWhiteSpace(Unit))
                metadata["unit"] = Unit!;
            if (!string.IsNullOrWhiteSpace(Formula))
                metadata["formula"] = Formula!;

            // Можно добавить явный логический тип
            metadata["logical_type"] = "category";

            return new Field(
                name: Name,
                dataType: dataType,
                nullable: true,
                metadata: metadata);
        }
    }

}
