using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Taschenrechner.Expression;

namespace Taschenrechner.Parser
{
    public class Parser
    {
        List<string> tokens = [];
        int _position; 

        public Parser(List<string> tokens)
        {
            this.tokens = tokens;
        }

        // Startpunkt

        public IExpression Parse()
        {
            return ParseExpression();
        }


        /* 
        ==================================
                * Hilfsmethoden *
        ==================================
        */
        private string Peek()
        {
            // Sieht sich das aktuelle Token an
            return _position < tokens.Count ? tokens[_position] : "";
        }

        private string Eat()
        {
            // Entfernt das aktuelle token, gibt es zurück, geht zum nächsten
            return tokens[_position++];
        }


        /* ================================== */

        // Ebene 1: Strichrechnung (+, -)
        private IExpression ParseExpression()
        {
            IExpression left = ParseTerm();

            while (Peek() == "+" ||Peek() == "-")
            {
                string op = Eat(); // Das + oder - konsumieren
                IExpression right = ParseTerm();

                if (op == "+")
                {
                    left = new AddExpression(left, right);
                }
                else
                {
                    left = new SubtractExpression(left, right); 
                }
            }

            return left;
        }

        // Ebene 2: Punktrechnung (*, /)
        private IExpression ParseTerm()
        {
            IExpression left = ParsePower();

            while (Peek() == "*" || Peek() == "/")
            {
                string op = Eat(); // Das + oder - konsumieren
                IExpression right = ParsePower();

                if (op == "*")
                {
                    left = new MultiplyExpression(left, right);
                }
                else
                {
                    left = new DivideExpression(left, right);
                }
            }

            return left;
        }

        // Ebene 3: Potenzrechnung (^)
        private IExpression ParsePower()
        {
            IExpression left = ParseFactor();

            if (Peek() == "^")
            {
                Eat();

                IExpression right = ParsePower();

                return new PowerExpression(left, right);
            }

            return left;
        }

        // Ebene 4: Zahlen, Variablen, Klammern
        private IExpression ParseFactor()
        {
            string op = Peek();

            if (op == "(")
            {
                Eat();
                IExpression result = ParseExpression();

                if (Peek() == ")")
                {
                    Eat();
                }
                else
                {
                    throw new Exception("Fehlende schließende Klammer");
                }

                return result;
            }
            else if (char.IsDigit(op[0]))
            {
                return new NumberExpression(double.Parse(Eat()));
            }
            else if (char.IsLetter(op[0]))
            {
                return new VariableExpression(Eat());
            }
            else
            {
                throw new Exception("Ungültiges Token: " + op);
            }
        }   
    }
}
