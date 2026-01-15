using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    using System;
    using System.Collections.Generic;
    using Apache.Arrow;
    using Apache.Arrow.Types;

    public static class VectorHelper
    {
        public static IArrowArray BuildArrayFromValues(
            IVariable variable,
            object?[] values)
        {
            switch (variable)
            {
                case NumericVariable num:
                    return BuildDecimalArray(num, values);

                case BoolVariable:
                    return BuildBoolArray(values);

                case IntegerVariable:
                case CategoryVariable:
                case OrdinalCategoryVariable:
                    return BuildInt32Array(values);

                case StringVariable:
                    return BuildStringArray(values);

                case DateVariable:
                    return BuildDate32Array(values);

                case DateTimeVariable dtVar:
                    return BuildTimestampArray(dtVar, values);

                default:
                    throw new NotSupportedException(
                        $"Unsupported variable type: {variable.GetType().Name}");
            }
        }

        // -------- Decimal (NumericVariable) --------

        private static IArrowArray BuildDecimalArray(
            NumericVariable variable,
            object?[] values)
        {
            // 1. Определяем тип
            var precision = variable.Precision;
            var scale = variable.Scale;
            var decimalType = new Decimal128Type(precision, scale);
            var builder = new Decimal128Array.Builder(decimalType);

            foreach (var v in values)
            {
                if (v is decimal d)
                {
                    var rounded = Math.Round(d, scale, MidpointRounding.AwayFromZero);
                    builder.Append(rounded);
                }
                else
                {
                    builder.AppendNull();
                }
            }

            return builder.Build();
        }

        // -------- Boolean --------

        private static IArrowArray BuildBoolArray(object?[] values)
        {
            var builder = new BooleanArray.Builder();

        foreach (var v in values)
            {
                if (v is bool b)
                    builder.Append(b);
                else
                    builder.AppendNull();
            }

            return builder.Build();
        }

        // -------- Int32 (Integer / Category / OrdinalCategory) --------

        private static IArrowArray BuildInt32Array(object?[] values)
        {
            var builder = new Int32Array.Builder(); 

        foreach (var v in values)
            {
                if (v is int i)
                    builder.Append(i);
                else
                    builder.AppendNull();
            }

            return builder.Build();
        }

        // -------- String --------

        private static IArrowArray BuildStringArray(object?[] values)
        {
            var builder = new StringArray.Builder();

        foreach (var v in values)
            {
                if (v is string s)
                    builder.Append(s);
                else
                    builder.AppendNull();
            }

            return builder.Build();
        }

        // -------- Date32 (DateVariable) --------

        private static IArrowArray BuildDate32Array(object?[] values)
        {
            var builder = new Date32Array.Builder();

        foreach (var v in values)
            {
                if (v is DateTime dt)
                {
                    int days = (int)(dt.Date - DateTime.UnixEpoch.Date).TotalDays;
                    builder.Append(dt);
                }
                else
                {
                    builder.AppendNull();
                }
            }

            return builder.Build();
        }

        // -------- Timestamp (DateTimeVariable) --------

        private static IArrowArray BuildTimestampArray(
            DateTimeVariable variable,
            object?[] values)
        {
            var type = new TimestampType(variable.TimeUnit, variable.TimeZone); 
        var builder = new TimestampArray.Builder(type);

            TimeSpan unitFactor = variable.TimeUnit switch
            {
                TimeUnit.Second => TimeSpan.FromSeconds(1),
                TimeUnit.Millisecond => TimeSpan.FromMilliseconds(1),
                TimeUnit.Microsecond => TimeSpan.FromTicks(10), // упрощённо
                TimeUnit.Nanosecond => TimeSpan.FromTicks(1),  // упрощённо
                _ => TimeSpan.FromMilliseconds(1)
            };

            foreach (var v in values)
            {
                if (v is DateTime dt)
                {
                    long ticksSinceEpoch =
                        (dt.ToUniversalTime() - DateTime.UnixEpoch).Ticks;
                    long raw = ticksSinceEpoch / unitFactor.Ticks;
                    var dto = new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero);
                    builder.Append(dto);
                }
                else
                {
                    builder.AppendNull();
                }
            }

            return builder.Build();
        }
    }

}
