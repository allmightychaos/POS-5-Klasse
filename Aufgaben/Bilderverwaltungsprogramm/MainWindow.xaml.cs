using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bilderverwaltungsprogramm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            this._viewModel = new ViewModel();
            this.DataContext = this._viewModel;
        }

        private void Window_Closed(object sender, EventArgs e)
        { 
            try
            {
                XmlService.Speichern(_viewModel.AlbumAuswahl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}