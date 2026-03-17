using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public class AddExpression : IExpression
    {
        private IExpression _left;
        private IExpression _right;

        public AddExpression(IExpression left, IExpression right)
        {
            _left = left;
            _right = right;
        }

        public double Evaluate(Dictionary<string, double> variables)
        {
            return _left.Evaluate(variables) + _right.Evaluate(variables);
        }
    }
}
