using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1.Variables
{
    public enum VectorFillMode
    {
        Random,
        Sequence,
        Constant
    }

    public interface IVectorFactory
    {
        Vector CreateEmpty(IVariable variable, int length);

        Vector CreateFilled(
            IVariable variable,
            int length,
            VectorFillMode mode,
            object? start = null,
            object? step = null,
            object? min = null,
            object? max = null);
    }

}
