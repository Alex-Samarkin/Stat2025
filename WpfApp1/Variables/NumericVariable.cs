using System;
using System.Collections.Generic;
using System.Text;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace WpfApp1.Variables
{
   

    public sealed class NumericVariable : VariableBase
    {
        // Настройки fixed-point
        public int Precision { get; }
        public int Scale { get; }

        public NumericVariable(
            string name,
            int precision = 18,
            int scale = 6,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
            Precision = precision;
            Scale = scale;
        }

        public override Type ClrType => typeof(decimal?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            var dataType = new Decimal128Type(Precision, Scale); // fixed-point decimal[web:16][web:111]

            var metadata = new Dictionary<string, string>();
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
