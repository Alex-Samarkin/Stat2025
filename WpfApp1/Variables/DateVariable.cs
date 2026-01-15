using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    using Apache.Arrow;
    using Apache.Arrow.Types;

    public sealed class DateVariable : VariableBase
    {
        public DateVariable(
            string name,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
        }

        // В UI/модели удобно работать с DateTime?, но интерпретировать только дату
        public override Type ClrType => typeof(DateTime?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            // days since UNIX epoch
            var dataType = Date32Type.Default; 

        var metadata = new Dictionary<string, string>
        {
            ["logical_type"] = "date"
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
