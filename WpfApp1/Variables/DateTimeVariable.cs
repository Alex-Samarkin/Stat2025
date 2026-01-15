using Apache.Arrow;
using Apache.Arrow.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public sealed class DateTimeVariable : VariableBase
    {
        // Можно хранить юнит и таймзону явно
        public TimeUnit TimeUnit { get; }
        public string? TimeZone { get; }

        public DateTimeVariable(
            string name,
            TimeUnit timeUnit = TimeUnit.Millisecond,
            string? timeZone = null,
            string? description = null,
            string? unit = null,
            string? formula = null)
            : base(name, description, unit, formula)
        {
            TimeUnit = timeUnit;
            TimeZone = timeZone;
        }

        public override Type ClrType => typeof(DateTime?);

        private Field? _field;
        public override Field ArrowField => _field ??= BuildField();

        private Field BuildField()
        {
            // 64-bit timestamp since UNIX epoch
            var dataType = new TimestampType(TimeUnit, TimeZone); 

        var metadata = new Dictionary<string, string>
        {
            ["logical_type"] = "datetime"
        };

            if (!string.IsNullOrWhiteSpace(Description))
                metadata["description"] = Description!;
            if (!string.IsNullOrWhiteSpace(Unit))
                metadata["unit"] = Unit!;
            if (!string.IsNullOrWhiteSpace(Formula))
                metadata["formula"] = Formula!;

            if (!string.IsNullOrWhiteSpace(TimeZone))
                metadata["timezone"] = TimeZone!;

            return new Field(
                name: Name,
                dataType: dataType,
                nullable: true,
                metadata: metadata);
        }
    }

}
