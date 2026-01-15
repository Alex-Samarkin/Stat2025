using Apache.Arrow;
using Apache.Arrow.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public sealed class StringVariable : VariableBase
    {
        public StringVariable(
            string name,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
        }

        public override Type ClrType => typeof(string);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            var dataType = StringType.Default; // UTF-8 строки[web:83]

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
