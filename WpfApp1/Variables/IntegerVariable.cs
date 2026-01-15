using Apache.Arrow;
using Apache.Arrow.Types;
using System.Xml.Linq;


namespace WpfApp1.Variables
{
    public sealed class IntegerVariable : VariableBase
    {
        public IntegerVariable(
            string name,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
        }

        public override Type ClrType => typeof(int?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            var dataType = Int32Type.Default; // 32-битное целое[web:83]

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
