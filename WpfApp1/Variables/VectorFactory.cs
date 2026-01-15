using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace WpfApp1.Variables
{
    
    public sealed class VectorFactory : IVectorFactory
    {
        private readonly Random _rnd;

        public VectorFactory(int? seed = null)
        {
            _rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public Vector CreateEmpty(IVariable variable, int length)
        {
            var emptyValues = new object?[length];
            var array = VectorHelper.BuildArrayFromValues(variable, emptyValues);
            return new Vector(variable, array);
        }

        public Vector CreateFilled(
            IVariable variable,
            int length,
            VectorFillMode mode,
            object? start = null,
            object? step = null,
            object? min = null,
            object? max = null)
        {
            object?[] values = variable switch
            {
                NumericVariable => FillNumeric(length, mode, start, step, min, max),
                IntegerVariable => FillInteger(length, mode, start, step, min, max),
                BoolVariable => FillBool(length, mode, start),
                StringVariable => FillString(length, mode, start),
                CategoryVariable cat => FillCategory(cat, length, mode, start),
                OrdinalCategoryVariable o => FillOrdinal(o, length, mode, start),
                DateVariable => FillDate(length, mode, start, step, min, max),
                DateTimeVariable dtVar => FillDateTime(dtVar, length, mode, start, step, min, max),
                _ => throw new NotSupportedException(
                    $"Unsupported variable type: {variable.GetType().Name}")
            };

            var array = VectorHelper.BuildArrayFromValues(variable, values);
            return new Vector(variable, array);
        }

        // ------------------------
        // Числовые (decimal)
        // ------------------------

        private object?[] FillNumeric(
            int length,
            VectorFillMode mode,
            object? start,
            object? step,
            object? min,
            object? max)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        decimal current = (start as decimal?) ?? 0m;
                        decimal delta = (step as decimal?) ?? 1m;

                        for (int i = 0; i < length; i++)
                        {
                            result[i] = current;
                            current += delta;
                        }
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        decimal lo = (min as decimal?) ?? 0m;
                        decimal hi = (max as decimal?) ?? 1m;
                        for (int i = 0; i < length; i++)
                        {
                            result[i] = RandomDecimal(lo, hi); // через Random.NextDouble[web:150]
                        }
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        decimal value = (start as decimal?) ?? 0m;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }

        private decimal RandomDecimal(decimal min, decimal max)
        {
            var d = (decimal)_rnd.NextDouble();
        return min + (max - min) * d;
        }

        // ------------------------
        // Integer
        // ------------------------

        private object?[] FillInteger(
            int length,
            VectorFillMode mode,
            object? start,
            object? step,
            object? min,
            object? max)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        int current = (start as int?) ?? 0;
                        int delta = (step as int?) ?? 1;

                        for (int i = 0; i < length; i++)
                        {
                            result[i] = current;
                            current += delta;
                        }
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        int lo = (min as int?) ?? 0;
                        int hi = (max as int?) ?? 100;
                        for (int i = 0; i < length; i++)
                        {
                            result[i] = _rnd.Next(lo, hi); 
                }

                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        int value = (start as int?) ?? 0;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // Boolean
        // ------------------------

        private object?[] FillBool(
            int length,
            VectorFillMode mode,
            object? start)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        // простая чередующаяся последовательность
                        for (int i = 0; i < length; i++)
                            result[i] = (i % 2 == 0);
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = _rnd.Next(2) == 0; 
                break;
                    }

                case VectorFillMode.Constant:
                    {
                        bool value = (start as bool?) ?? true;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // String
        // ------------------------

        private object?[] FillString(
            int length,
            VectorFillMode mode,
            object? start)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = $"item_{i}";
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = $"val_{_rnd.Next(0, 1_000_000)}";
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        string value = (start as string) ?? string.Empty;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // Category
        // ------------------------

        private object?[] FillCategory(
            CategoryVariable variable,
            int length,
            VectorFillMode mode,
            object? start)
        {
            var result = new object?[length];
            var codes = variable.Categories.Keys.ToArray();
            if (codes.Length == 0)
                return result;

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = codes[i % codes.Length];
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = codes[_rnd.Next(0, codes.Length)];
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        int defaultCode = codes[0];
                        int code = (start as int?) ?? defaultCode;
                        for (int i = 0; i < length; i++)
                            result[i] = code;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // OrdinalCategory
        // ------------------------

        private object?[] FillOrdinal(
            OrdinalCategoryVariable variable,
            int length,
            VectorFillMode mode,
            object? start)
        {
            var result = new object?[length];
            int n = variable.OrderedCategories.Count;
            if (n == 0)
                return result;

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = i % n;
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        for (int i = 0; i < length; i++)
                            result[i] = _rnd.Next(0, n);
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        int defaultCode = 0; // первая категория (минимальная)
                        int code = (start as int?) ?? defaultCode;
                        for (int i = 0; i < length; i++)
                            result[i] = code;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // Date
        // ------------------------

        private object?[] FillDate(
            int length,
            VectorFillMode mode,
            object? start,
            object? step,
            object? min,
            object? max)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        DateTime current = (start as DateTime?)?.Date
                                           ?? DateTime.Today.Date;
                        TimeSpan delta = (step as TimeSpan?)
                                           ?? TimeSpan.FromDays(1);

                        for (int i = 0; i < length; i++)
                        {
                            result[i] = current.Date;
                            current = current.Add(delta);
                        }
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        DateTime minDate = (min as DateTime?)?.Date
                                           ?? DateTime.Today.AddYears(-1).Date;
                        DateTime maxDate = (max as DateTime?)?.Date
                                           ?? DateTime.Today.Date;

                        if (maxDate < minDate)
                            (minDate, maxDate) = (maxDate, minDate);

                        int rangeDays = Math.Max((maxDate - minDate).Days, 1);
                        for (int i = 0; i < length; i++)
                        {
                            int offset = _rnd.Next(0, rangeDays + 1); 
                    result[i] = minDate.AddDays(offset).Date;
                        }
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        DateTime value = (start as DateTime?)?.Date
                                         ?? DateTime.Today.Date;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }

        // ------------------------
        // DateTime
        // ------------------------

        private object?[] FillDateTime(
            DateTimeVariable variable,
            int length,
            VectorFillMode mode,
            object? start,
            object? step,
            object? min,
            object? max)
        {
            var result = new object?[length];

            switch (mode)
            {
                case VectorFillMode.Sequence:
                    {
                        DateTime current = (start as DateTime?) ?? DateTime.UtcNow;
                        TimeSpan delta = (step as TimeSpan?) ?? TimeSpan.FromMinutes(1);

                        for (int i = 0; i < length; i++)
                        {
                            result[i] = current;
                            current = current.Add(delta);
                        }
                        break;
                    }

                case VectorFillMode.Random:
                    {
                        DateTime minDt = (min as DateTime?) ?? DateTime.UtcNow.AddDays(-1);
                        DateTime maxDt = (max as DateTime?) ?? DateTime.UtcNow;

                        if (maxDt < minDt)
                            (minDt, maxDt) = (maxDt, minDt);

                        var totalSeconds = Math.Max((maxDt - minDt).TotalSeconds, 1);

                        for (int i = 0; i < length; i++)
                        {
                            var offsetSeconds = _rnd.NextDouble() * totalSeconds; 
                    result[i] = minDt.AddSeconds(offsetSeconds);
                        }
                        break;
                    }

                case VectorFillMode.Constant:
                    {
                        DateTime value = (start as DateTime?) ?? DateTime.UtcNow;
                        for (int i = 0; i < length; i++)
                            result[i] = value;
                        break;
                    }
            }

            return result;
        }
    }

}
