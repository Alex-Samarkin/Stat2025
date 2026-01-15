using Apache.Arrow;
using Apache.Arrow.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public sealed class OrdinalCategoryVariable : VariableBase
    {
        // Порядок важен: индекс в списке = код
        public IReadOnlyList<string> OrderedCategories { get; }

        public OrdinalCategoryVariable(
            string name,
            IReadOnlyList<string> orderedCategories,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
            OrderedCategories = orderedCategories;
        }

        // В данных храним код категории (0..N-1), но можно и 1..N
        public override Type ClrType => typeof(int?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            var dataType = Int32Type.Default; // физически int32[web:16]

            var metadata = new Dictionary<string, string>
            {
                // последовательность категорий в порядке возрастания
                ["ordered_categories"] =
                    System.Text.Json.JsonSerializer.Serialize(OrderedCategories),
                ["logical_type"] = "ordinal_category"
            };

            if (!string.IsNullOrWhiteSpace(Description))
                metadata["description"] = Description!;
            if (!string.IsNullOrWhiteSpace(Unit))
                metadata["unit"] = Unit!;
            if (!string.IsNullOrWhiteSpace(Formula))
                metadata["formula"] = Formula!;

            return new Field(
                name: Name,
                dataType: dataType,
                nullable: true,
                metadata: metadata);
        }
    }

}
