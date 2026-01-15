using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public interface IVariable
    {
        // Идентификация
        string Name { get; }
        string? Description { get; }
        string? Unit { get; }

        // Формула (опционально)
        string? Formula { get; }
        bool IsCalculated { get; }

        // Тип в .NET (для WPF, валидации и т.п.)
        Type ClrType { get; }

        // Arrow-описание колонки
        Apache.Arrow.Field ArrowField { get; }
    }

}
