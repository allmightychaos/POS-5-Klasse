using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public class PowerExpression : IExpression
    {
        private IExpression _left;
        private IExpression _right;

        public PowerExpression(IExpression left, IExpression right)
        {
            _left = left;
            _right = right;
        }

        public double Evaluate(Dictionary<string, double> variables)
        {
            return Math.Pow(_left.Evaluate(variables), _right.Evaluate(variables));
        }
    }
}
