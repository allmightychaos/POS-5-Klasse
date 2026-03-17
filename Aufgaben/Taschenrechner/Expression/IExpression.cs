using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taschenrechner.Expression
{
    public interface IExpression
    {
        double Evaluate(Dictionary<string, double> variables);
    }
}
