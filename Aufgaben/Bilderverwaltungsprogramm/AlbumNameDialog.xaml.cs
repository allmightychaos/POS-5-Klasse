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

namespace Bilderverwaltungsprogramm
{
    /// <summary>
    /// Interaktionslogik für AlbumNameDialog.xaml
    /// </summary>
    public partial class AlbumNameDialog : Window
    {
        public string AlbumName { get; private set; }

        public AlbumNameDialog()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            AlbumName = AlbumNameTextBox.Text;
            this.DialogResult = true;
        }
    }
}
