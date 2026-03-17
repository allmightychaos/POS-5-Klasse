using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SchiffeGUI
{
    public class SpielViewModel : INotifyPropertyChanged
    {
        private bool _gameStarted = false;
        private bool _istClientReady = false;

        private NetzwerkManager _netz;
        
        // Status => "Setze deine Schiffe", "Spiel gestartet!", ...
        private string _status;
        public string SpielStatus
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }


        // ------------------ ObservCollection ------------------ \\
        public ObservableCollection<Feld> SpielfeldListe { get; set; }
        public ObservableCollection<Feld> GegnerListe { get; set; }
        public ObservableCollection<VerfuegbaresSchiff> Flottenliste { get; set; }



        // ------------------ Spielcontrols ------------------ \\

        // --- Aktuell ausg. Schiff --- \\\
        private VerfuegbaresSchiff _ausgSchiff {  get; set; }
        public VerfuegbaresSchiff AusgSchiff
        {
            get { return _ausgSchiff; }
            set
            {
                _ausgSchiff = value;
                OnPropertyChanged();
            }
        }


        // --- Aktuell ausg. Orientierung --- \\\
        private Orientierung _ausgOrientierung = Orientierung.Horizontal;
        public Orientierung AusgOrientierung
        {
            get { return _ausgOrientierung; }
            set
            {
                _ausgOrientierung = value;
                OnPropertyChanged();
            }
        }


        // --- OrientierungGeklickt --- \\
        public ICommand OrientierungGeklicktCommand { get; set; }

        private void OrientierungGeklickt(object parameter)
        {
            if (AusgOrientierung == Orientierung.Horizontal)
            {
                AusgOrientierung = Orientierung.Vertikal;
            }
            else
            {
                AusgOrientierung = Orientierung.Horizontal;
            }
        }



        // ------------------ Netzwerkcontrols ------------------ \\

        // --- buttonHosting --- \\
        private string _buttonHosting = "Spiel hosten";
        public string buttonHosting
        {
            get { return _buttonHosting; }
            set
            {
                _buttonHosting = value;
                OnPropertyChanged();
            }
        }

        // --- buttonBeitreten --- \\
        private string _buttonBeitreten = "Spiel beitreten";
        public string buttonBeitreten
        {
            get { return _buttonBeitreten; }
            set
            {
                _buttonBeitreten = value;
                OnPropertyChanged();
            }
        }


        // --- HostWurdeGeklickt --- \\
        public ICommand HostWurdeGeklickt { get; set; }
        private async void HostGeklickt(object parameter)
        {
            if (_istHost)
            {
                buttonHosting = "Spiel wird gehostet";
                SpielStatus = "Warten auf Verbindung...";
                istClient = false;

                await _netz.StarteHost();

    }
            else
            {
                buttonHosting = "Spiel hosten";
                SpielStatus = "";
                istClient = true;
            }
        }

        // --- BeitretenWurdeGeklickt --- \\
        public ICommand BeitretenWurdeGeklickt { get; set; }
        private async void BeitretenGeklickt(object parameter)
        {
            if (_istClient)
            {
                buttonBeitreten = "...wird beigetreten";
                SpielStatus = "Warten auf Verbindung...";
                istHost = false;

                await _netz.StarteClient("127.0.0.1", 12345);
            }
            else
            {
                buttonBeitreten = "Spiel beitreten";
                SpielStatus = "";
                istHost = true;
            }
        }



        // --- istHost aktiv --- \\
        private bool _istHost = true;
        public bool istHost
        {
            get { return _istHost; }
            set
            {
                _istHost = value;
                OnPropertyChanged();
            }
        }

        // --- istClient aktiv --- \\
        private bool _istClient = true;
        public bool istClient
        {
            get { return _istClient; }
            set
            {
                _istClient = value;
                OnPropertyChanged();
            }
        }

        // --- isConnected --- \\
        private bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value; 
                OnPropertyChanged();
            }
        }



        // --- NetConnected --- \\
        public void NetConnected()
        {
            SpielStatus = "Erfolgreich verbunden!";
            IsConnected = true;
        }

        // --- Client Ready --- \\

        public void ClientReady()
        {
            _istClientReady = true;
            CheckIsEnabled();
        }

        // ------------------ Spiel ------------------ \\

        // --- IsEnabled Property (able to start the game) --- \\
        private bool _isEnabled = false;
        public bool isEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }


        // -- CheckIsEnabled --- \\
        private void CheckIsEnabled()
        {
            int sum = Flottenliste.Sum(x => x.Anzahl);

            if (sum == 0)
            {
                SpielStatus = "Alle Schiffe gesetzt.";

                if (IsConnected && istHost && _istClientReady)
                {
                    isEnabled = true;
                }
                
                if (istClient)
                {
                    SpielStatus = "Sende FERTIG.";
                    _netz.sendMessage(Netzwerkbefehl.FERTIG);
                }

            }
        }

        // --- myTurn --- \\
        private bool _myTurn = false;
        public bool myTurn
        {
            get { return _myTurn; }
            set
            {
                _myTurn = value;
                OnPropertyChanged();
            }
        }


        // --- NeuesSpielGeklicktCommand --- \\
        public ICommand NeuesSpielGeklicktCommand { get; set; }
        private void NeuesSpielGeklickt(object parameter)
        {
            isEnabled = false;
            _netz.sendMessage(Netzwerkbefehl.STARTEN);
            SpielStarten();
        }


        // --- Spiel Starten --- \\
        public void SpielStarten()
        {
            SpielStatus = "Spiel gestartet!";
            _gameStarted = true;

            // MyTurn -> spielstatus = netzwerkbefehl
            if (_istClient) { myTurn = true; SpielStatus = "Dein Zug."; }
        }

        // --- Swap Turn --- \\
        public void SwapTurn()
        {
            SpielStatus = "Dein Zug.";
            myTurn = true;
        }


        // --- CheckHit --- \\
        public Netzwerkbefehl CheckHit(int index, out string payload) // "out string payload" -> Text nach außen
        {
            payload = index.ToString();

            if (index >= 0)
            {
                Feld Spielfeld = SpielfeldListe[index];

                // Feld überprüfen (Wasser/Schiff)
                switch (Spielfeld.Zustand)
                {
                    case FeldZustand.Wasser:
                        Spielfeld.Zustand = FeldZustand.Fehlschuss;
                        return Netzwerkbefehl.WASSER;

                    case FeldZustand.Schiff:
                        Spielfeld.SchiffReferenz.Leben--;
                         
                        if (Spielfeld.SchiffReferenz.Leben >= 1)
                        {
                            Spielfeld.Zustand = FeldZustand.Treffer;
                            return Netzwerkbefehl.TREFFER;
                        }
                        else
                        {
                            Versenkt(Spielfeld);

                            // Alle Indizen holen & mit Komma verbinden
                            var alleIndizes = Spielfeld.SchiffReferenz.Felder.Select(f => (f.Y * 10) + f.X);

                            payload = string.Join(",", alleIndizes);

                            // Prüfen ob verloren
                            int Leben = SpielfeldListe
                                .Where(f => f.SchiffReferenz != null)   // 1. Nur belegte Felder
                                .Select(f => f.SchiffReferenz)          // 2. Schiff-Objekte holen
                                .Distinct()                             // 3. Filtere Klone heraus
                                .Sum(schiff => schiff.Leben);           // 4. Summe der Leben

                            if (Leben == 0) 
                            {
                                SpielStatus = "Du hast leider das Spiel verloren.";
                                return Netzwerkbefehl.VERLOREN;
                            }
                            else 
                            {
                                return Netzwerkbefehl.VERSENKT;
                            }
                        }
                }
            }

            return Netzwerkbefehl.UNGÜLTIG;
        }

        public void Versenkt(Feld Spielfeld)
        {
            foreach(Feld feld in Spielfeld.SchiffReferenz.Felder)
            {
                feld.Zustand = FeldZustand.Versenkt;
            }
        }


        // ------------------ 10x10 Grid ------------------ \\

        // --- Eigenes Feld geklickt --- \\
        public ICommand FeldGeklicktCommand { get; set; }

        private void FeldWurdeGeklickt(object parameter)
        {
            int SchiffLaenge;
            bool istHorizontal;


            // Überprüfen ob ein AusgangsSchiff vorhanden ist
            if (AusgSchiff == null) { return; }

            // ...und ob die Anzahl mind. 1 ist 
            if (AusgSchiff.Anzahl > 0)
            {
                SchiffLaenge = (int)AusgSchiff.Typ;
            }
            else
            {
                SpielStatus = "Schifftyp ist nicht mehr verfügbar.";
                return;
            }


            // istHorizontal setzen
            if (AusgOrientierung == Orientierung.Horizontal)
            {
                istHorizontal = true;
            }
            else
            {
                istHorizontal = false;
            }



            if (parameter is Feld startFeld)
            {
                // 1. Spielregel - Prüfen, ob Schiff den Rand überschreitet.
                if (istHorizontal)
                {
                    if ((startFeld.X + SchiffLaenge - 1) > 9)
                    {
                        SpielStatus = "Ungültig: Schiff überschreitet den Rand.";
                        return;
                    }
                }
                else
                {
                    if ((startFeld.Y + SchiffLaenge - 1) > 9)
                    {
                        SpielStatus = "Ungültig: Schiff überschreitet den Rand.";
                        return;
                    }
                }

                // 2. Spielregel - Prüfen, ob die Schiffe aneinander stoßen würden
                for (int i = 0; i < SchiffLaenge; i++)
                {
                    int startX = istHorizontal ? startFeld.X + i : startFeld.X;
                    int startY = istHorizontal ? startFeld.Y : startFeld.Y + i;

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            int tempX = startX + offsetX;
                            int tempY = startY + offsetY;

                            if (tempX < 0 || tempX > 9) { continue; }
                            else if (tempY < 0 || tempY > 9) { continue; }
                            else
                            {
                                int index = tempY * 10 + tempX;
                                Feld checkFeld = SpielfeldListe[index];

                                if (checkFeld.Zustand != FeldZustand.Wasser)
                                {
                                    SpielStatus = "Ungültig: Schiffe kollidieren.";
                                    return;
                                }
                            }
                        }
                    }
                }

                // Schiff platzieren
                GesetztesSchiff Schiff = new(SchiffLaenge);
                Schiff.Felder = new List<Feld>();

                for (int i = 0; i < SchiffLaenge; i++)
                {
                    int setX = istHorizontal ? startFeld.X + i : startFeld.X;
                    int setY = istHorizontal ? startFeld.Y : startFeld.Y + i;

                    int index = setY * 10 + setX;
                    Feld setzFeld = SpielfeldListe[index];

                    setzFeld.Zustand = FeldZustand.Schiff;
                    setzFeld.SchiffReferenz = Schiff;
                    Schiff.Felder.Add(setzFeld);
                }

                AusgSchiff.Anzahl--;
                SpielStatus = "Schiff erfolgreich gesetzt.";
                CheckIsEnabled();
            }
        }


        // --- Gegner Feld geklickt--- \\
        public ICommand GegnerFeldGeklicktCommand { get; set; }

        private void GegnerFeldGeklickt(object paremeter)
        {
            if (_gameStarted && myTurn)
            {
                if (paremeter is Feld startFeld)
                {
                    string index = (startFeld.Y * 10 + startFeld.X).ToString();
                    
                    SpielStatus = "Gegner an der Reihe.";
                    _netz.sendMessage(Netzwerkbefehl.SCHUSS, index);

                    myTurn = false;
                }
            }
            else
            {
                SpielStatus = "Nicht dein Zug.";
                return;
            }
        }



        // ------------------ Konstruktor ------------------ \\
        public SpielViewModel()
        {
            // --- Beginne das Spiel --- \\
            SpielStatus = "Setzte deine Schiffe";
            SpielfeldListe = new ObservableCollection<Feld>();
            GegnerListe = new ObservableCollection<Feld>();


            // --- 10x10 Grid --- \\
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    SpielfeldListe.Add(new Feld(j, i));
                    GegnerListe.Add(new Feld(j, i));
                }
            }

            // --- Commands für WPF --- \\
            FeldGeklicktCommand = new RelayCommand(FeldWurdeGeklickt);
            OrientierungGeklicktCommand = new RelayCommand(OrientierungGeklickt);
            NeuesSpielGeklicktCommand = new RelayCommand(NeuesSpielGeklickt);
            GegnerFeldGeklicktCommand = new RelayCommand(GegnerFeldGeklickt);
            HostWurdeGeklickt = new RelayCommand(HostGeklickt);
            BeitretenWurdeGeklickt = new RelayCommand(BeitretenGeklickt);


            // --- Flottenliste --- \\
            Flottenliste = new ObservableCollection<VerfuegbaresSchiff>();

            Flottenliste.Add(new VerfuegbaresSchiff(Schifftyp.Schlachtschiff, "Schlachtschiff", 1));
            
            Flottenliste.Add(new VerfuegbaresSchiff(Schifftyp.Kreuzer, "Kreuzer", 2));
            Flottenliste.Add(new VerfuegbaresSchiff(Schifftyp.Zerstoerer, "Zerstörer", 3));
            Flottenliste.Add(new VerfuegbaresSchiff(Schifftyp.Uboot, "U-Boot", 4));
            

            AusgSchiff = Flottenliste[0];


            // --- Netzwerkmanager --- \\
            _netz = new NetzwerkManager(this);
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
