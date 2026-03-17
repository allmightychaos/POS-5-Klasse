using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public class VariableExpression : IExpression
    {
        private string _variable;

        public VariableExpression(string variable)
        {
            _variable = variable;
        }

        public double Evaluate(Dictionary<string, double> variables)
        {
            if (variables.ContainsKey(_variable))
            {
                return variables[_variable];
            }

            throw new System.Exception($"Variable {_variable} nicht definiert.");
        }
    }
}
