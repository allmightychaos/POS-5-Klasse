using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Taschenrechner.UI
{
    /// <summary>
    /// Interaktionslogik für ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public string FullText { get; set; }
        public string FindString { get; set; }
        public string ErrorString { get; set; }
        public ErrorDialog(string text, string findstring, string error )
        {
            InitializeComponent();
            FullText = text;
            FindString = findstring;
            ErrorString = error;

            this.DataContext = this;
        }
    }
}
