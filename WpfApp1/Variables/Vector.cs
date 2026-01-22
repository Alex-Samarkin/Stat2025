using System;
using System.Collections.Generic;
using System.Text;
using System;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace WpfApp1.Variables
{
  

    public interface IVector
    {
        IVariable Variable { get; }
        int Length { get; }
        IArrowArray ArrowArray { get; }

        object? GetValue(int index);
        object GetValueToDB(int index);

        void SetValue(int index, object? value);
        void RebuildArrowArray();
    }

    public sealed class Vector : IVector
    {
        public IVariable Variable { get; }
        public IArrowArray ArrowArray { get; private set; }

        public int Length => ArrowArray.Length;

        private readonly object?[] _values;
        private readonly object[] _valuesDB;

        public Vector(IVariable variable, IArrowArray array)
        {
            Variable = variable;
            ArrowArray = array;
            _values = ExtractValues(variable, array);
            _valuesDB = ExtractValuesDB(variable, array);
        }

        public object? GetValue(int index) => _values[index];

        public object GetValueToDB(int index)
        {
            var value = _values[index];
            if (value == null) return DBNull.Value;
            return value;
        }

        public void SetValue(int index, object? value)
        {
            _values[index] = value;
            if (value == null)
            {
                _valuesDB[index] = DBNull.Value;
                return;
            }
            _valuesDB[index] = value;
        }

        public void RebuildArrowArray()
        {
            ArrowArray = VectorHelper.BuildArrayFromValues(Variable, _values);
        }

        // ------------------------
        // Извлечение CLR-значений из ArrowArray
        // ------------------------

        private static object?[] ExtractValues(
            IVariable variable,
            IArrowArray array)
        {
            var values = new object?[array.Length];

            switch (variable)
            {
                case NumericVariable:
                    {
                        var a = (Decimal128Array)array; 
                for (int i = 0; i < a.Length; i++)
                            values[i] = a.IsValid(i) ? a.GetValue(i) : null;
                        break;
                    }

                case BoolVariable:
                    {
                        var a = (BooleanArray)array; 
                for (int i = 0; i < a.Length; i++)
                            values[i] = a.IsValid(i) ? a.GetValue(i) : (bool?)null;
                        break;
                    }

                case IntegerVariable:
                case CategoryVariable:
                case OrdinalCategoryVariable:
                    {
                        var a = (Int32Array)array; 
                for (int i = 0; i < a.Length; i++)
                            values[i] = a.IsValid(i) ? a.GetValue(i) : (int?)null;
                        break;
                    }

                case StringVariable:
                    {
                        var a = (StringArray)array; 
                for (int i = 0; i < a.Length; i++)
                            values[i] = a.IsValid(i) ? a.GetString(i) : null;
                        break;
                    }

                case DateVariable:
                    {
                        var a = (Date32Array)array; 
                for (int i = 0; i < a.Length; i++)
                        {
                            if (!a.IsValid(i))
                            {
                                values[i] = null;
                                continue;
                            }

                            int days = Convert.ToInt32(a.GetValue(i)); 
                            values[i] = DateTime.UnixEpoch.Date.AddDays(days);
                        }
                        break;
                    }

                case DateTimeVariable dtVar:
                    {
                        var a = (TimestampArray)array; 

                TimeSpan unitFactor = dtVar.TimeUnit switch
                {
                    TimeUnit.Second => TimeSpan.FromSeconds(1),
                    TimeUnit.Millisecond => TimeSpan.FromMilliseconds(1),
                    TimeUnit.Microsecond => TimeSpan.FromTicks(10),
                    TimeUnit.Nanosecond => TimeSpan.FromTicks(1),
                    _ => TimeSpan.FromMilliseconds(1)
                };

                        for (int i = 0; i < a.Length; i++)
                        {
                            if (!a.IsValid(i))
                            {
                                values[i] = null;
                                continue;
                            }

                            long raw = Convert.ToInt64(a.GetValue(i));
                            long ticks = raw * unitFactor.Ticks;
                            values[i] = DateTime.UnixEpoch.AddTicks(ticks);
                        }
                        break;
                    }

                default:
                    throw new NotSupportedException(
                        $"Unsupported variable type: {variable.GetType().Name}");
            }

            return values;
        }

        private static object[] ExtractValuesDB(
            IVariable variable,
            IArrowArray array)
        {
            var values = new object[array.Length];

            switch (variable)
            {
                case NumericVariable:
                    {
                        var a = (Decimal128Array)array;
                        for (int i = 0; i < a.Length; i++)
                            if (a.IsValid(i))
                            {
                                var v = a.GetValue(i);
                                if (v != null) values[i] = v;
                                else values[i] = DBNull.Value;
                            }
                            else
                            {
                                values[i] = DBNull.Value;
                            }
                        break;
                    }

                case BoolVariable:
                    {
                        var a = (BooleanArray)array;
                        for (int i = 0; i < a.Length; i++)
                            if (a.IsValid(i))
                            {
                                var v = a.GetValue(i);
                                if (v != null) values[i] = v;
                                else values[i] = DBNull.Value;
                            }
                            else
                            {
                                values[i] = DBNull.Value; 
                            }
                        
                        break;
                    }

                case IntegerVariable:
                case CategoryVariable:
                case OrdinalCategoryVariable:
                    {
                        var a = (Int32Array)array;
                        for (int i = 0; i < a.Length; i++)
                            if (a.IsValid(i))
                            {
                                var v = a.GetValue(i);
                                if (v != null) values[i] = v;
                                else values[i] = DBNull.Value;
                            }
                            else
                            {
                                values[i] = DBNull.Value;
                            }
                        break;
                    }

                case StringVariable:
                    {
                        var a = (StringArray)array;
                        for (int i = 0; i < a.Length; i++)
                            values[i] = a.IsValid(i) ? a.GetString(i) : DBNull.Value;
                        break;
                    }

                case DateVariable:
                    {
                        var a = (Date32Array)array;
                        for (int i = 0; i < a.Length; i++)
                        {
                            if (!a.IsValid(i))
                            {
                                values[i] = DBNull.Value;
                                continue;
                            }

                            int days = Convert.ToInt32(a.GetValue(i));
                            values[i] = DateTime.UnixEpoch.Date.AddDays(days);
                        }
                        break;
                    }

                case DateTimeVariable dtVar:
                    {
                        var a = (TimestampArray)array;

                        TimeSpan unitFactor = dtVar.TimeUnit switch
                        {
                            TimeUnit.Second => TimeSpan.FromSeconds(1),
                            TimeUnit.Millisecond => TimeSpan.FromMilliseconds(1),
                            TimeUnit.Microsecond => TimeSpan.FromTicks(10),
                            TimeUnit.Nanosecond => TimeSpan.FromTicks(1),
                            _ => TimeSpan.FromMilliseconds(1)
                        };

                        for (int i = 0; i < a.Length; i++)
                        {
                            if (!a.IsValid(i))
                            {
                                values[i] = DBNull.Value;
                                continue;
                            }

                            long raw = Convert.ToInt64(a.GetValue(i));
                            long ticks = raw * unitFactor.Ticks;
                            values[i] = DateTime.UnixEpoch.AddTicks(ticks);
                        }
                        break;
                    }

                default:
                    throw new NotSupportedException(
                        $"Unsupported variable type: {variable.GetType().Name}");
            }

            return values;
        }


    }

}
