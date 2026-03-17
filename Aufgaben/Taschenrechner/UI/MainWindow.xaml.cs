using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;

using Taschenrechner.Expression;
using Taschenrechner.Parser;

namespace Taschenrechner.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isNewEntry = false;
        private int _isOpenBracket = 0;

        Dictionary<string, double> keyValues;

        public MainWindow()
        {
            InitializeComponent();
            
        }

        // 1, 2, 3, 4, 5, 6, 7, 8, 9, 0
        private void buttonNumbersClicked(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            string eingabe = b.Content.ToString();

            // Anfangszustand
            if (Output.Text == "0")
            {
                Output.Text = eingabe;
            }
            else if (!_isNewEntry)
            {
                Output.Text = eingabe;
            }
            else if (Zwischenrechnung.Text.EndsWith("(") && !_isNewEntry)
            {
                Output.Text = eingabe;
            }
            else 
            {
                Output.Text += eingabe;
            }

            _isNewEntry = true;
        }

        // +, -, *, /, ^
        private void buttonOperaterClicked(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            string eingabe = b.Content.ToString();

            // '×' -> '*' und '÷' -> '/' umwandeln
            if (eingabe == "×")
            {
                eingabe = "*";
            }
            else if (eingabe == "÷")
            {
                eingabe = "/";
            }

            // Anfangszustand
            if (Zwischenrechnung.Text == "")
            {
                Zwischenrechnung.Text = Output.Text + " " + eingabe;
                _isNewEntry = false;
            }
            else if (!_isNewEntry)
            {
                // Letztes Zeichen ersetzen (außer Klammer zu)
                if (Zwischenrechnung.Text.EndsWith(")"))
                {
                    Zwischenrechnung.Text += " " + eingabe;
                }
                else
                {
                    Zwischenrechnung.Text = Zwischenrechnung.Text[..^1] + eingabe;
                }
            }
            else
            {
                Zwischenrechnung.Text += " " + Output.Text + " " + eingabe;
                _isNewEntry = false;
            }
        }

        // ⌫
        private void buttonDeleteClicked(object sender, EventArgs e)
        {

            if (Output.Text != "0" && _isNewEntry)
            {
                if (Output.Text.Length > 1)
                {
                    Output.Text = Output.Text.Remove(Output.Text.Length - 1, 1);
                }
                else
                {
                    Output.Text = "0";
                }
            }
        }

        // Clear
        private void buttonClearClicked(object sender, EventArgs e)
        {
            Output.Text = "0";
            Zwischenrechnung.Text = "";
            _isNewEntry = false;
            
            if (keyValues == null) { keyValues.Clear(); }
        }

        //,
        private void buttonCommaClicked(object sender, EventArgs e)
        {
            if (!_isNewEntry)
            {
                Output.Text = "0,";
            }
            else
            {
                Output.Text += ",";
            }

            _isNewEntry = true;
        }

        // (
        private void buttonOpenBracketClicked(object sender, EventArgs e)
        {
            // Ausgangslage
            if (Output.Text == "0" && Zwischenrechnung.Text == "")
            {
                Zwischenrechnung.Text = "(";
            }
            else if (!_isNewEntry)
            {
                if (Zwischenrechnung.Text.EndsWith(")"))
                {
                    Zwischenrechnung.Text += " × (";
                }
                else
                {
                    Zwischenrechnung.Text += " (";
                    Output.Text = "0";
                }
            }
            else
            {
                Zwischenrechnung.Text += " " + Output.Text + " × (";
            }

            _isOpenBracket += 1;
            _isNewEntry = false;
        }

        // )
        private void buttonClosedBracketClicked(object sender, EventArgs e)
        {
            if (_isOpenBracket > 0)
            {
                if (_isNewEntry == true)
                {
                    Zwischenrechnung.Text += " " + Output.Text + " )";
                }
                else if (Output.Text == "0") {
                    Zwischenrechnung.Text += " 0 )";
                }
                else
                {
                    Zwischenrechnung.Text += " )";
                }

            }

            _isOpenBracket -= 1;
            _isNewEntry = false;
        }

        // x, y, z, ...
        private void buttonVariableClicked(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            string eingabe = b.Content.ToString();

            if (Output.Text == "0" && Zwischenrechnung.Text == "")
            {
                Output.Text = eingabe;
            }
            else if (!_isNewEntry)
            {
                Output.Text = eingabe;
            }

            _isNewEntry = true;
        }

        // =
        private void buttonEqualClicked(object sender, EventArgs e)
        {
            // 1. Eingabe holen (buttonEqualClicked()
            if (Output.Text == "0" && Zwischenrechnung.Text == "")
            {
                Zwischenrechnung.Text = "0 =";
            }
            else if (Zwischenrechnung.Text == "")
            {
                Zwischenrechnung.Text = Output.Text + " =";
            }
            else if (_isOpenBracket > 0)
            {
                Zwischenrechnung.Text += " " + Output.Text + " ) =";
                _isOpenBracket = 0;
            }
            else if (!_isNewEntry)
            {
                Zwischenrechnung.Text += " =";
            }
            else
            {
                Zwischenrechnung.Text += " " + Output.Text + " =";
            }
            _isNewEntry = false;

            mainFunction();
        }

        // Keyboard Handler
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            Button b = new Button();
            /* ======================= Zahlen ======================= */
            // Zahlen 0-9 (oberste Reihe)
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                b.Content = (e.Key - Key.D0).ToString();
                buttonNumbersClicked(b, new EventArgs());
            }
            // Zahlen 0-9 (NumPad)
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                b.Content = (e.Key - Key.NumPad0).ToString();
                buttonNumbersClicked(b, new EventArgs());
            }

            /* ==================== Variablen A-Z ==================== */
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                b.Content = e.Key.ToString().ToLower();
                buttonNumbersClicked(b, new EventArgs());
            }

            /* ===================== Operatoren ====================== */
            // +
            else if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                b.Content = "+";
                buttonOperaterClicked(b, new EventArgs());
            }
            // -
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                b.Content = "-";
                buttonOperaterClicked(b, new EventArgs());
            }
            // *
            else if (e.Key == Key.Multiply)
            {
                b.Content = "*";
                buttonOperaterClicked(b, new EventArgs());
            }
            // /
            else if (e.Key == Key.Divide)
            {
                b.Content = "/";
                buttonOperaterClicked(b, new EventArgs());
            }
            // Gleich (=)
            else if (e.Key == Key.Enter)
            {
                b.Content = e.Key.ToString();
                buttonEqualClicked(b, new EventArgs());
            }
            // Komma (,)
            else if (e.Key == Key.OemComma || e.Key == Key.Decimal)
            {
                b.Content = e.Key.ToString();
                buttonCommaClicked(b, new EventArgs());
            }

            /* ====================== Sonstiges ====================== */
            // Löschen (Backspace)
            else if (e.Key == Key.Back)
            {
                b.Content = e.Key.ToString();
                buttonDeleteClicked(b, new EventArgs());
            }
            // Clear (Escape)
            else if (e.Key == Key.Escape)
            {
                b.Content = e.Key.ToString();
                buttonClearClicked(b, new EventArgs());
            }
        }

        // Regex-Match + Tokenize
        private List<string> regexMatch()
        {
            string eingabe = Zwischenrechnung.Text;

            // create Regex
            Regex rx = new Regex("[0-9]+([,][0-9]*)?|[a-z]|[\\+\\-\\*\\/\\^\\(\\)\\=]");

            // convert eingabe to tokens
            MatchCollection matches = rx.Matches(eingabe);
            List<string> tokens = matches.Cast<Match>().Select(m => m.Value).ToList();

            // MessageBox.Show(string.Join(", ", tokens)); // <-- debug Regex/Match Collection

            if (!regexErrorHandler(eingabe, tokens, rx))
            {
                return new List<string>();
            }

            return tokens;
        }

        // Regex Error Handler
        private bool regexErrorHandler(string eingabe, List<string> tokens, Regex rx)
        {
            // Remove whitespaces
            Array result = eingabe.Where(c => !char.IsWhiteSpace(c)).ToArray();
            // MessageBox.Show(result.Length.ToString()); // <-- debug: eingabe length w/o whitespaces

            // Count total tokens length (from regex/matchcollection)
            int tokenCounter = 0;

            foreach (var item in tokens)
            {
                tokenCounter += item.Length;
            }

            // MessageBox.Show(tokenCounter.ToString()); <-- debug: regex matches array length w/o whitespaces 

            // compare "eingabe" (w/o whitespaces) with (regex) tokens
            if (result.Length != tokenCounter)
            {
                List<string> falscheEingabeListe = [];

                foreach (var item in result)
                {
                    string itemStr = item.ToString();

                    if (!rx.IsMatch(itemStr))
                    {
                        falscheEingabeListe.Add(itemStr);
                    }
                }

                // Liste -> String
                string falscheEingabe = string.Join(", ", falscheEingabeListe);

                // HighlightTextBlock WPF-Fenster
                ErrorDialog dialog = new ErrorDialog(eingabe, falscheEingabeListe[0].ToString(), falscheEingabe);
                dialog.ShowDialog();

                /* Debug
                MessageBox.Show("Fehlerhafte Eingabe!");
                MessageBox.Show(falscheEingabe);
                */

            return false;
            }

            tokenCounter = 0;
            return true;
        }

        // Variablen finden
        private Dictionary<string, double> findVariables()
        {
            string eingabe = Zwischenrechnung.Text;
            List<string> variables = [];

            // Finde Variablen im Eingabe-String
            foreach (var item in eingabe)
            {
                if (char.IsLetter(item))
                {
                    variables.Add(item.ToString());
                }
            }

            // Distinct() um Duplikate zu entfernen
            variables.Distinct();

            // Prüfe ob Eingabe-String nicht leer ist 
            if (variables.Any())
            {
                return matchVariablesWithNumber(variables);
            }

            // Leeres Dictionary zurückgeben
            return new Dictionary<string, double>();
        }

        // Variablen mit Werten vom User matchen
        private Dictionary<string, double> matchVariablesWithNumber(List<string> variables)
        {

            if (keyValues == null)
            {
                keyValues = new Dictionary<string, double>();

                // Normaler Ablauf (Match Values mit Keys)
                foreach (var item in variables)
                {
                    string userValue = Interaction.InputBox($"Wert für Variable: {item} eingeben.", "Variable zuweisen");
                    double num;

                    if (double.TryParse(userValue, out num))
                    {
                        keyValues.Add(item, num);
                    }
                    else
                    {
                        throw new Exception("Ungültiger Wert für Variable.");
                    }
                }
            }
            else
            {
                List<string> tempVariables = [];

                // Check welche Variablen NICHT bereits im Dictionary sind
                foreach (var key in variables)
                {
                    if (!keyValues.ContainsKey(key))
                    {
                        tempVariables.Add(key);
                    }
                }

                // Normaler Ablauf (Match Values mit Keys)
                foreach (var item in tempVariables)
                {
                    string userValue = Interaction.InputBox($"Wert für Variable: {item} eingeben.", "Variable zuweisen");
                    double num;

                    if (double.TryParse(userValue, out num))
                    {
                        keyValues.Add(item, num);
                    }
                    else
                    {
                        throw new Exception("Ungültiger Wert für Variable.");
                    }
                }
            }

            return keyValues;
        }


        /* ============================================ */
        /*         HAUPTFUNKTION NACH "="-KLICK         */  
        /* ============================================ */
        private void mainFunction()
        {
            // 2. Regex-Matching / Lexer starten
            List<string> tokens = regexMatch();

            // 3. Variablen abfragen (findVariables) & in Dictionary
            keyValues = findVariables();

            // 4. Kontrollieren ob 'tokens' befüllt sind
            if (tokens.Count == 0)
            {
                return;
            }

            // 5. Parser füttern
            Parser p = new Parser(tokens);
            IExpression root = p.Parse();

            // 5. Berechnen
            double result = root.Evaluate(keyValues);

            // 6. Ergebnis anzeigen
            Output.Text = result.ToString();
        }
    }
}