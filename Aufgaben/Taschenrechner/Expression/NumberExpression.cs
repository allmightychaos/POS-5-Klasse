using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public class NumberExpression : IExpression
    {
        private double _value;
        public NumberExpression(double value)
        {
            _value = value;
        } 

        public double Evaluate(Dictionary<string, double> variables)
        {
            return _value;
        }
    }
}
