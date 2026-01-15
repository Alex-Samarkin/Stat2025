using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public abstract class VariableBase : IVariable
    {
        public string Name { get; }
        public string? Description { get; }
        public string? Unit { get; }
        public string? Formula { get; }
        public bool IsCalculated => !string.IsNullOrWhiteSpace(Formula);

        public abstract Type ClrType { get; }
        public abstract Apache.Arrow.Field ArrowField { get; }

        protected VariableBase(
            string name,
            string? description = null,
            string? unit = null,
            string? formula = null)
        {
            Name = name;
            Description = description;
            Unit = unit;
            Formula = formula;
        }
    }

}
