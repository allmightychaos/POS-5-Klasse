using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public class DivideExpression : IExpression
    {
        public IExpression _left;
        public IExpression _right;

        public DivideExpression(IExpression left, IExpression right)
        {
            _left = left;
            _right = right;
        }

        public double Evaluate(Dictionary<string, double> variables)
        {
            double divisor = _right.Evaluate(variables);
            if (divisor == 0)
            {
                throw new DivideByZeroException("Division durch Null nicht erlaubt");
            }

            return _left.Evaluate(variables) / divisor;
        }
    }
}
