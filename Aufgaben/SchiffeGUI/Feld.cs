using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SchiffeGUI
{
    public class Feld : INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }

        public GesetztesSchiff SchiffReferenz {  get; set; }


        // --- LOGIK --- \\
        private FeldZustand _zustand;
        public FeldZustand Zustand
        {
            get { return _zustand; }
            set
            {
                _zustand = value;
                OnPropertyChanged();

                AktualisiereOptik();
            }
        }


        // --- Anzeige --- \\

        private string _anzeigeText;
        public string AnzeigeText
        {
            get { return _anzeigeText; }
            set
            {
                _anzeigeText = value;
                OnPropertyChanged();
            }
        }
        

        private string _textfarbe;
        public string TextFarbe
        {
            get { return _textfarbe; }
            set
            {
                _textfarbe = value;
                OnPropertyChanged();
            }
        }


        private string _backgFarbe;
        public string BackgFarbe
        {
            get { return _backgFarbe; }
            set
            {
                _backgFarbe = value;
                OnPropertyChanged();
            }
        }

        // --- Aktualisiere Optik --- \\

        private void AktualisiereOptik()
        {
            switch (Zustand)
            {
                case FeldZustand.Wasser:
                    AnzeigeText = "~";
                    TextFarbe = "Blue";
                    BackgFarbe = "LightBlue";
                    break;
                case FeldZustand.Schiff:
                    AnzeigeText = "S";
                    TextFarbe = "Black";
                    BackgFarbe = "Gray";
                    break;
                case FeldZustand.Treffer:
                    AnzeigeText = "X";
                    TextFarbe = "Red";
                    BackgFarbe = "DarkRed";
                    break;
                case FeldZustand.Fehlschuss:
                    AnzeigeText = "O";
                    TextFarbe = "Black";
                    BackgFarbe = "Lightblue";
                    break;
                case FeldZustand.Versenkt:
                    AnzeigeText = "X";
                    TextFarbe = "Red";
                    BackgFarbe = "Black";
                    break;
            }
        }

        
        // --- Feld initialisieren -- \\
        public ICommand KlickCommand { get; set; }

        public Feld(int x, int y)
        {
            this.X = x;
            this.Y = y;
            Zustand = FeldZustand.Wasser;
        }


        // -- INotifyPropertyChanged (immer so kopieren) -- \\
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
