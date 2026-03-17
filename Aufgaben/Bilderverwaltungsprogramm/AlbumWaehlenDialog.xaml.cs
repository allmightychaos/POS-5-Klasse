using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaktionslogik für AlbumWaehlenDialog.xaml
    /// </summary>
    public partial class AlbumWaehlenDialog : Window
    {
        public Album GewaehltesAlbum { get; private set; }

        public AlbumWaehlenDialog(ObservableCollection<Album> alben)
        {
            InitializeComponent();
            DataContext = alben;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            GewaehltesAlbum = AlbumComboBox.SelectedItem as Album;
            this.DialogResult = true;
        }
    }
}
