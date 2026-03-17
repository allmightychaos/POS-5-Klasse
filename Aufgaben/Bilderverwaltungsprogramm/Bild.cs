using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Bilderverwaltungsprogramm
{
    public class Bild : INotifyPropertyChanged
    {
        private string _bildpfad;
        public string Bildpfad
        {
            get { return _bildpfad; }
            set
            {
                _bildpfad = value;
                OnPropertyChanged();
                if (_bildpfad != null)
                    Bildquelle = BildquelleErstellen();
            }
        }    // Bspw. "...\BVP-Album\...\image-001.png"

        [XmlIgnore]
        public string Dateiname => Path.GetFileName(Bildpfad);  // Bspw. "image-001.png"
        [XmlIgnore]
        public string Bildname => Path.GetFileNameWithoutExtension(Bildpfad);    // Bspw. "image-001" 


        [XmlIgnore]
        private BitmapImage _bildquelle { get; set; }
        [XmlIgnore]
        public BitmapImage Bildquelle
        {
            get {  return _bildquelle; }
            set
            {
                _bildquelle = value;
                OnPropertyChanged();
            }
        }

        public Bild(string bildpfad)
        {
            if (bildpfad == null) return;
            
            this.Bildpfad = bildpfad;
        }

        public Bild() { }

        public BitmapImage BildquelleErstellen()
        {
            // BitmapImage
            BitmapImage bmp = new BitmapImage();

            bmp.BeginInit();
            bmp.UriSource = new Uri(Bildpfad);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();

            return bmp;
        }


        // ------------------ INotifyPropertyChanged ------------------ \\

        // --- immer so kopieren --- \\
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
