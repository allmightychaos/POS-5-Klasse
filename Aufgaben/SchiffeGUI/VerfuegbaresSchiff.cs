using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SchiffeGUI
{
    public class VerfuegbaresSchiff : INotifyPropertyChanged
    {
        public Schifftyp Typ { get; set; }
        
        // Text für den Button
        public string Name { get; set; }


        // Zähler (der "klingelt")
        private int _anzahl;
        public int Anzahl
        {
            get { return _anzahl; }
            set
            {
                _anzahl = value;
                OnPropertyChanged();
            }
        }

        public VerfuegbaresSchiff(Schifftyp schifftyp, string name, int startAnzahl)
        {
            Typ = schifftyp;
            Name = name;
            Anzahl = startAnzahl;
        }


        // -- INotifyPropertyChanged (immer so kopieren) -- \\
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
