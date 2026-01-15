using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public static class VectorTransformation
    {
        // ---------- служебное ----------

        private static void EnsureSameLength(Vector a, Vector b)
        {
            if (a.Length != b.Length)
                throw new InvalidOperationException("Vectors must have the same length.");
        }

        private static bool IsNumericLike(IVariable variable)
            => variable is NumericVariable || variable is IntegerVariable;

        // =====================================================
        // 1. Арифметика (вектор–вектор, вектор–константа)
        // =====================================================

        public static Vector Add(Vector left, Vector right)
            => ElementwiseNumericBinary(left, right, (a, b) => a + b, "add");

        public static Vector Subtract(Vector left, Vector right)
            => ElementwiseNumericBinary(left, right, (a, b) => a - b, "sub");

        public static Vector Multiply(Vector left, Vector right)
            => ElementwiseNumericBinary(left, right, (a, b) => a * b, "mul");

        public static Vector Divide(Vector left, Vector right)
            => ElementwiseNumericBinary(left, right, (a, b) => a / b, "div");

        public static Vector Add(Vector vector, decimal constant)
            => ElementwiseNumericUnary(vector, v => v + constant, $"plus_{constant}");

        public static Vector Subtract(Vector vector, decimal constant)
            => ElementwiseNumericUnary(vector, v => v - constant, $"minus_{constant}");

        public static Vector Multiply(Vector vector, decimal constant)
            => ElementwiseNumericUnary(vector, v => v * constant, $"mul_{constant}");

        public static Vector Divide(Vector vector, decimal constant)
            => ElementwiseNumericUnary(vector, v => v / constant, $"div_{constant}");

        // =====================================================
        // 2. Логические операции (включая инверсию)
        // =====================================================

        public static Vector And(Vector left, Vector right)
        {
            EnsureSameLength(left, right);
            if (left.Variable is not BoolVariable || right.Variable is not BoolVariable)
                throw new InvalidOperationException("And is supported only for BoolVariable.");

            var resultValues = new object?[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                var a = left.GetValue(i) as bool?;
                var b = right.GetValue(i) as bool?;
                resultValues[i] = (a.HasValue && b.HasValue)
                    ? (a.Value && b.Value)
                    : (bool?)null;
            }

            var resultVar = new BoolVariable(
                name: $"{left.Variable.Name}_and_{right.Variable.Name}");

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        public static Vector Or(Vector left, Vector right)
        {
            EnsureSameLength(left, right);
            if (left.Variable is not BoolVariable || right.Variable is not BoolVariable)
                throw new InvalidOperationException("Or is supported only for BoolVariable.");

            var resultValues = new object?[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                var a = left.GetValue(i) as bool?;
                var b = right.GetValue(i) as bool?;
                resultValues[i] = (a.HasValue && b.HasValue)
                    ? (a.Value || b.Value)
                    : (bool?)null;
            }

            var resultVar = new BoolVariable(
                name: $"{left.Variable.Name}_or_{right.Variable.Name}");

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        public static Vector Not(Vector vector)
        {
            if (vector.Variable is not BoolVariable)
                throw new InvalidOperationException("Not is supported only for BoolVariable.");

            var resultValues = new object?[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i) as bool?;
                resultValues[i] = v.HasValue ? !v.Value : (bool?)null;
            }

            var resultVar = new BoolVariable(
                name: $"not_{vector.Variable.Name}");

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // =====================================================
        // 3. Нормализация и стандартизация (Z-score)
        // =====================================================

        // Простейшая min-max нормализация: (x - min) / (max - min)
        public static Vector Normalize(Vector vector)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException("Normalize is supported only for numeric/integer variables.");

            // считаем min/max по ненулевым значениям
            decimal? min = null, max = null;
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null) continue;
                decimal dv = Convert.ToDecimal(v);

                if (!min.HasValue || dv < min.Value) min = dv;
                if (!max.HasValue || dv > max.Value) max = dv;
            }

            if (!min.HasValue || !max.HasValue || min.Value == max.Value)
                throw new InvalidOperationException("Cannot normalize constant or empty vector.");

            decimal range = max.Value - min.Value;

            var resultValues = new object?[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    resultValues[i] = null;
                    continue;
                }

                decimal dv = Convert.ToDecimal(v);
                resultValues[i] = (dv - min.Value) / range;
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_norm",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // Z-score стандартизация: (x - mean) / std[web:213][web:216][web:219]
        public static Vector Standardize(Vector vector)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException("Standardize is supported only for numeric/integer variables.");

            // среднее и стандартное отклонение по ненулевым значениям
            decimal sum = 0m;
            int count = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null) continue;
                sum += Convert.ToDecimal(v);
                count++;
            }

            if (count == 0)
                throw new InvalidOperationException("Cannot standardize empty vector.");

            decimal mean = sum / count;

            decimal sumSq = 0m;
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null) continue;
                decimal dv = Convert.ToDecimal(v) - mean;
                sumSq += dv * dv;
            }

            decimal variance = sumSq / count;
            if (variance == 0m)
                throw new InvalidOperationException("Cannot standardize constant vector.");

            decimal std = (decimal)Math.Sqrt((double)variance);

            var resultValues = new object?[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    resultValues[i] = null;
                    continue;
                }

                decimal dv = Convert.ToDecimal(v);
                resultValues[i] = (dv - mean) / std;
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_z",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // =====================================================
        // 4. Box–Cox transform (power transform)
        // =====================================================

        // Box–Cox определён только для строго положительных значений[web:207][web:210][web:217]
        // y(λ) = (y^λ - 1) / λ, λ != 0;  y(0) = ln(y)
        public static Vector BoxCox(Vector vector, double lambda)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException("BoxCox is supported only for numeric/integer variables.");

            var resultValues = new object?[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    resultValues[i] = null;
                    continue;
                }

                decimal dv = Convert.ToDecimal(v);
                if (dv <= 0)
                    throw new InvalidOperationException("BoxCox requires strictly positive values.");

                double x = (double)dv;
                double yTrans;

                if (Math.Abs(lambda) < 1e-9)
                    yTrans = Math.Log(x);
                else
                    yTrans = (Math.Pow(x, lambda) - 1.0) / lambda;

                resultValues[i] = (decimal)yTrans;
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_boxcox_{lambda}",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // =====================================================
        // 5. Rolling-операции (простое скользящее окно)
        // =====================================================

        // Rolling mean по окну windowSize, выравнивание "right": значение в i — среднее по [i-windowSize+1 .. i]
        public static Vector RollingMean(Vector vector, int windowSize)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException("RollingMean is supported only for numeric/integer variables.");

            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));

            var resultValues = new object?[vector.Length];

            decimal windowSum = 0m;
            int windowCount = 0;

            var buffer = new decimal?[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    buffer[i] = null;
                }
                else
                {
                    buffer[i] = Convert.ToDecimal(v);
                }
            }

            for (int i = 0; i < vector.Length; i++)
            {
                if (buffer[i].HasValue)
                {
                    windowSum += buffer[i]!.Value;
                    windowCount++;
                }

                int j = i - windowSize;
                if (j >= 0 && buffer[j].HasValue)
                {
                    windowSum -= buffer[j]!.Value;
                    windowCount--;
                }

                if (i >= windowSize - 1 && windowCount > 0)
                    resultValues[i] = windowSum / windowCount;
                else
                    resultValues[i] = null;
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_rollmean_{windowSize}",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // Rolling sum (по той же схеме)
        public static Vector RollingSum(Vector vector, int windowSize)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException("RollingSum is supported only for numeric/integer variables.");

            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));

            var resultValues = new object?[vector.Length];

            decimal windowSum = 0m;
            int windowCount = 0;

            var buffer = new decimal?[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    buffer[i] = null;
                }
                else
                {
                    buffer[i] = Convert.ToDecimal(v);
                }
            }

            for (int i = 0; i < vector.Length; i++)
            {
                if (buffer[i].HasValue)
                {
                    windowSum += buffer[i]!.Value;
                    windowCount++;
                }

                int j = i - windowSize;
                if (j >= 0 && buffer[j].HasValue)
                {
                    windowSum -= buffer[j]!.Value;
                    windowCount--;
                }

                if (i >= windowSize - 1 && windowCount > 0)
                    resultValues[i] = windowSum;
                else
                    resultValues[i] = null;
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_rollsum_{windowSize}",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // =====================================================
        // 6. Даты (пример: разность дат в днях и сдвиг времени)
        // =====================================================

        public static Vector DaysBetween(Vector left, Vector right)
        {
            EnsureSameLength(left, right);

            if (left.Variable is not DateVariable || right.Variable is not DateVariable)
                throw new InvalidOperationException("DaysBetween is supported only for DateVariable.");

            var resultValues = new object?[left.Length];

            for (int i = 0; i < left.Length; i++)
            {
                var a = left.GetValue(i) as DateTime?;
                var b = right.GetValue(i) as DateTime?;

                if (!a.HasValue || !b.HasValue)
                {
                    resultValues[i] = null;
                    continue;
                }

                resultValues[i] = (int)(a.Value.Date - b.Value.Date).TotalDays;
            }

            var resultVar = new IntegerVariable(
                name: $"{left.Variable.Name}_minus_{right.Variable.Name}_days");

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        public static Vector ShiftDateTime(Vector vector, TimeSpan offset)
        {
            if (vector.Variable is not DateTimeVariable dtVar)
                throw new InvalidOperationException("ShiftDateTime is supported only for DateTimeVariable.");

            var resultValues = new object?[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i) as DateTime?;
                resultValues[i] = v.HasValue ? v.Value.Add(offset) : null;
            }

            var resultVar = new DateTimeVariable(
                name: $"{vector.Variable.Name}_shift_{offset}",
                timeUnit: dtVar.TimeUnit,
                timeZone: dtVar.TimeZone);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        // =====================================================
        // 7. Общие числовые помощники (v–v, v–const)
        // =====================================================

        private static Vector ElementwiseNumericBinary(
            Vector left,
            Vector right,
            Func<decimal, decimal, decimal> op,
            string opName)
        {
            EnsureSameLength(left, right);

            if (!IsNumericLike(left.Variable) || !IsNumericLike(right.Variable))
                throw new InvalidOperationException(
                    $"{opName} is supported only for numeric/integer variables.");

            var resultValues = new object?[left.Length];

            for (int i = 0; i < left.Length; i++)
            {
                var a = left.GetValue(i);
                var b = right.GetValue(i);

                if (a is null || b is null)
                {
                    resultValues[i] = null;
                    continue;
                }

                decimal da = Convert.ToDecimal(a);
                decimal db = Convert.ToDecimal(b);
                resultValues[i] = op(da, db);
            }

            var resultVar = new NumericVariable(
                name: $"{left.Variable.Name}_{opName}_{right.Variable.Name}",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }

        private static Vector ElementwiseNumericUnary(
            Vector vector,
            Func<decimal, decimal> op,
            string suffix)
        {
            if (!IsNumericLike(vector.Variable))
                throw new InvalidOperationException(
                    $"Operation is supported only for numeric/integer variables.");

            var resultValues = new object?[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                var v = vector.GetValue(i);
                if (v is null)
                {
                    resultValues[i] = null;
                    continue;
                }

                decimal dv = Convert.ToDecimal(v);
                resultValues[i] = op(dv);
            }

            var resultVar = new NumericVariable(
                name: $"{vector.Variable.Name}_{suffix}",
                precision: 18,
                scale: 6);

            var array = VectorHelper.BuildArrayFromValues(resultVar, resultValues);
            return new Vector(resultVar, array);
        }
    }

}
