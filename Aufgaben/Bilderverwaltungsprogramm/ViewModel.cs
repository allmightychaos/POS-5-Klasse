using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Bilderverwaltungsprogramm
{
    public class ViewModel : INotifyPropertyChanged
    {
        public List<Bild> AusgBilder { get; set; }

        // ------------------ ObservableCollections ------------------ \\
        public ObservableCollection<Album> AlbumAuswahl {  get; set; }


        // ------------------ WPF ------------------ \\

        // ---------- DATEI ---------- \\

        // --- Ausgewähltes Album --- \\
        private Album _ausgAlbum {  get; set; }
        public Album AusgAlbum
        {
            get { return _ausgAlbum; }
            set
            {
                _ausgAlbum = value;
                OnPropertyChanged();
            }
        }

        // --- Album erstellen --- \\
        public ICommand AlbumErstellenCommand { get; set; }
        private void AlbumErstellen(object parameter)
        {
            AlbumNameDialog dialog = new AlbumNameDialog();
            bool? result = dialog.ShowDialog();

            if (result != true) return;
            string albumname = dialog.AlbumName;

            Album album = new(albumname);

            // If AlbumAuswahl is empty
            if (!AlbumAuswahl.Any())
            {
                // Check if directory exists & add directory with `albumname`
                EnsureAlbumDirectoryExists(albumname);

                // Add Album
                AlbumAuswahl.Add(album);
                AusgAlbum = AlbumAuswahl[0];
            }
            else
            {
                // Check if Album already exists with LINQ
                bool exists = AlbumAuswahl.Any(alb => alb.AlbumName == album.AlbumName);

                if (!exists)
                {
                    EnsureAlbumDirectoryExists(albumname);
                    AlbumAuswahl.Add(album);
                }
                else
                {
                    MessageBox.Show("Album existiert bereits.");
                }
            }
        }

        public void EnsureAlbumDirectoryExists(string albumName)
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory; // creates in same folder as .exe
            string baseDir = Path.Combine(exePath, "BVP-Album");    // adds default "BVP-Album" folder to path
            string albumDir = Path.Combine(baseDir, albumName);     // adds user's `Album` folder to path

            Directory.CreateDirectory(albumDir);                    // creates directory
        }


        // --- Bild (.zip) hinzufügen --- \\
        public ICommand BildHinzufuegenCommand { get; set; }
        private void BildHinzufügen(object parameter)
        {
            if (AlbumAuswahl.Any())
            {
                OpenFileDialog oFD = new OpenFileDialog();
                oFD.Filter = "Zip files (*.zip)|*.zip";

                bool? result = oFD.ShowDialog();

                if (result == true)
                {
                    // Select Albumname & Path
                    string album = AusgAlbum.AlbumName;
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    string path = Path.Combine(exePath, "BVP-Album", album);

                    // Extract Zip -> album directory
                    ZipFile.ExtractToDirectory(oFD.FileName, path, true); // true -> overwrite files

                    // Get all files in the directory
                    string[] files = Directory.GetFiles(path);

                    foreach (string file in files)
                    {
                        Bild bild = new Bild(file);

                        if (!AusgAlbum.Bilddatei.Any(f => f.Bildpfad == file))
                        {
                            AusgAlbum.Bilddatei.Add(bild);
                        }
                    }

                    // MessageBox.Show($"Bilder in Collection: {AusgAlbum.Bilddatei.Count}");
                }
            }
            else
            {
                MessageBox.Show("Zuerst Album erstellen!");
            }
        }


        // --- Bild(er) löschen --- \\
        public ICommand BildLoeschenCommand { get; set; }
        private void BildLoeschen(object parameter)
        {
            List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();

            if (selected.Any())
            {
                foreach (Bild bild in selected)
                {
                    AusgAlbum.Bilddatei.Remove(bild);
                    File.Delete(bild.Bildpfad);
                }
            }
            else
            {
                MessageBox.Show("Wähle zuerst ein Bild aus.");
            }

        }


        // --- Bild(er) verschieben --- \\
        public ICommand BildVerschiebenCommand { get; set; }
        private void BildVerschieben(object parameter)
        {
            List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();

            if (selected.Any())
            {
                AlbumWaehlenDialog dialog = new AlbumWaehlenDialog(AlbumAuswahl);
                bool? result = dialog.ShowDialog(); // <- returns "true" when "ok"-button was clicked

                if (result == true)
                {
                    Album auswahl = dialog.GewaehltesAlbum;

                    foreach (Bild bild in selected)
                    {
                        AusgAlbum.Bilddatei.Remove(bild);   // remove from old Album
                        auswahl.Bilddatei.Add(bild);        // add to selected Album

                        // get destination path
                        string exePath = AppDomain.CurrentDomain.BaseDirectory;
                        string destPath = Path.Combine(exePath, "BVP-Album", auswahl.AlbumName, bild.Dateiname);

                        // move file
                        File.Move(bild.Bildpfad, destPath, true); // <- wieder true für overwrite
                    }
                }
            }
            else
            {
                MessageBox.Show("Wähle zuerst ein Bild aus.");
            }
        }



        // ---------- BEARBEITEN ---------- \\

        // --- Rotate --- \\
        public ICommand RotateClock90Command { get; set; }
        public ICommand RotateCounter90Command { get; set; }
        public ICommand Rotate180Command { get; set; }

        private void RotateClock90(object parameter)
        {
            List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();
            RotateImage(selected, "clock90");
        }

        private void RotateCounter90(object parameter)
        {
            List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();
            RotateImage(selected, "counter90");
        }

        private void Rotate180(object parameter)
        {
            List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();
            RotateImage(selected, "rotate180");
        }


        private void RotateImage(List<Bild> selected, string direction)
        {
            // ------------ IMPORTANT ------------ \\
            // 1. Open NuGet                       \\
            // 2. Install System.Drawing.Common    \\
            // ----------------------------------- \\

            Bitmap rotated;

            switch (direction)
            {
                case "clock90":
                    foreach (var item in selected)
                    {
                        using (var temp = System.Drawing.Image.FromFile(item.Bildpfad))
                        {
                            rotated = new Bitmap(temp);
                            rotated.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        }

                        rotated.Save(item.Bildpfad);
                        item.Bildquelle = item.BildquelleErstellen();
                    }
                    break;
                case "counter90":
                    foreach (var item in selected)
                    {
                        using (var temp = System.Drawing.Image.FromFile(item.Bildpfad))
                        {
                            rotated = new Bitmap(temp);
                            rotated.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        }

                        rotated.Save(item.Bildpfad);
                        item.Bildquelle = item.BildquelleErstellen();
                    }
                    break;
                case "rotate180":
                    foreach (var item in selected)
                    {
                        using (var temp = System.Drawing.Image.FromFile(item.Bildpfad))
                        {
                            rotated = new Bitmap(temp);
                            rotated.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        }

                        rotated.Save(item.Bildpfad);
                        item.Bildquelle = item.BildquelleErstellen();
                    }
                    break;

                default:
                    break;
            }
        }


        // ------------------ Konstruktor ------------------ \\
        public ViewModel()
        {
            AlbumAuswahl = new ObservableCollection<Album>();
            // XML Laden (wenn vorhanden)
            if (File.Exists(XmlService.XmlPfad))
            {
                AlbumAuswahl = XmlService.Laden();

                if (AlbumAuswahl.Any())
                {
                    AusgAlbum = AlbumAuswahl[0];
                }
            }

            // --- Commands für WPF --- \\
            AlbumErstellenCommand = new RelayCommand(AlbumErstellen);
            BildHinzufuegenCommand = new RelayCommand(BildHinzufügen);
            BildLoeschenCommand = new RelayCommand(BildLoeschen);
            BildVerschiebenCommand = new RelayCommand(BildVerschieben);

            RotateClock90Command = new RelayCommand(RotateClock90);
            RotateCounter90Command = new RelayCommand(RotateCounter90);
            Rotate180Command = new RelayCommand(Rotate180);
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
