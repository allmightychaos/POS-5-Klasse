using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Bilderverwaltungsprogramm
{
    public class Album : INotifyPropertyChanged
    {
        public string AlbumName { get; set; }
        public ObservableCollection<Bild> Bilddatei { get; set; } = new ObservableCollection<Bild>();

        // ------------------ Konstruktor ------------------ \\
        public Album(string Albumname)
        {
            this.AlbumName = Albumname;
        }

        public Album() { }

        // ------------------ INotifyPropertyChanged ------------------ \\

        // --- immer so kopieren --- \\
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
