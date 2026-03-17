# WPF + Netzwerk + MVVM – Schiffe Versenken Referenz

---

## Projektstruktur

```
SchiffeGUI/
├── Enums.cs               ← FeldZustand, Orientierung, Schifftyp, Netzwerkbefehl
├── Feld.cs                ← einzelnes Spielfeld-Element (INotifyPropertyChanged)
├── GesetztesSchiff.cs     ← gesetztes Schiff mit Leben + Felder-Liste
├── VerfuegbaresSchiff.cs  ← Schiff zur Auswahl (Typ, Name, Anzahl)
├── SpielViewModel.cs      ← gesamte Spiellogik + MVVM
├── NetzwerkManager.cs     ← TCP-Verbindung + Nachrichten
├── RelayCommand.cs        ← ICommand-Implementierung
├── MainWindow.xaml        ← UI
└── MainWindow.xaml.cs     ← nur DataContext setzen
```

---

## 1. Enums

```cs
public enum FeldZustand
{
    Wasser,     // unberührt
    Schiff,     // eigenes Schiff (sichtbar für Besitzer)
    Treffer,    // getroffenes Schiff
    Versenkt,   // komplett versenktes Schiff
    Fehlschuss  // ins Wasser geschossen
}

public enum Orientierung { Horizontal, Vertikal }

public enum Schifftyp
{
    Uboot          = 2,  // Länge 2
    Zerstoerer     = 3,  // Länge 3
    Kreuzer        = 4,  // Länge 4
    Schlachtschiff = 5   // Länge 5
}

public enum Netzwerkbefehl
{
    SCHUSS,    // Spieler schießt auf Position
    WASSER,    // Schuss ins Wasser
    TREFFER,   // Schiff getroffen
    VERSENKT,  // Schiff versenkt
    OK,        // Bestätigung nach WASSER/TREFFER/VERSENKT
    UNGÜLTIG,
    FERTIG,    // Client hat alle Schiffe gesetzt
    STARTEN,   // Host startet das Spiel
    VERLOREN   // Verlierer schickt das an Gewinner
}
```

---

## 2. Feld-Klasse (einzelne Zelle)

Jede Zelle im Spielfeld ist ein `Feld`-Objekt mit `INotifyPropertyChanged`.
Wenn `Zustand` gesetzt wird, aktualisiert sich die Optik automatisch.

```cs
public class Feld : INotifyPropertyChanged
{
    public int X { get; set; }
    public int Y { get; set; }
    public GesetztesSchiff SchiffReferenz { get; set; } // null wenn kein Schiff

    private FeldZustand _zustand;
    public FeldZustand Zustand
    {
        get => _zustand;
        set
        {
            _zustand = value;
            OnPropertyChanged();
            AktualisiereOptik(); // ← Farbe + Text automatisch aktualisieren
        }
    }

    // Anzeige-Properties (gebunden im XAML)
    public string AnzeigeText { get; set; }
    public string TextFarbe   { get; set; }
    public string BackgFarbe  { get; set; }

    // Klick-Command (gebunden im DataTemplate)
    public ICommand KlickCommand { get; set; }

    public Feld(int x, int y)
    {
        X = x; Y = y;
        Zustand = FeldZustand.Wasser; // initialisiert auch Optik
    }

    private void AktualisiereOptik()
    {
        switch (Zustand)
        {
            case FeldZustand.Wasser:
                AnzeigeText = "~"; TextFarbe = "Blue";  BackgFarbe = "LightBlue"; break;
            case FeldZustand.Schiff:
                AnzeigeText = "S"; TextFarbe = "Black"; BackgFarbe = "Gray";      break;
            case FeldZustand.Treffer:
                AnzeigeText = "X"; TextFarbe = "Red";   BackgFarbe = "DarkRed";   break;
            case FeldZustand.Fehlschuss:
                AnzeigeText = "O"; TextFarbe = "Black"; BackgFarbe = "LightBlue"; break;
            case FeldZustand.Versenkt:
                AnzeigeText = "X"; TextFarbe = "Red";   BackgFarbe = "Black";     break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

---

## 3. GesetztesSchiff + VerfuegbaresSchiff

### GesetztesSchiff (ein platziertes Schiff)

```cs
public class GesetztesSchiff
{
    public int Leben { get; set; }       // = Schifflänge, wird bei Treffer verringert
    public List<Feld> Felder { get; set; } // alle Felder die das Schiff belegt

    public GesetztesSchiff(int leben)
    {
        Leben = leben;
        Felder = new List<Feld>();
    }
}
```

### VerfuegbaresSchiff (zur Auswahl in der UI)

```cs
public class VerfuegbaresSchiff : INotifyPropertyChanged
{
    public Schifftyp Typ { get; set; }
    public string Name { get; set; }

    private int _anzahl;
    public int Anzahl  // klingelt wenn sich ändert → UI aktualisiert Button
    {
        get => _anzahl;
        set { _anzahl = value; OnPropertyChanged(); }
    }

    public VerfuegbaresSchiff(Schifftyp typ, string name, int startAnzahl)
    {
        Typ = typ; Name = name; Anzahl = startAnzahl;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

---

## 4. RelayCommand (ICommand)

Ermöglicht das Binden von Buttons/Klicks an ViewModel-Methoden ohne Click-Events.

```cs
public class RelayCommand : ICommand
{
    private Action<object> _action;

    public RelayCommand(Action<object> aktion)
    {
        _action = aktion;
    }

    public bool CanExecute(object parameter) => true; // immer ausführbar
    public void Execute(object parameter) => _action(parameter);
    public event EventHandler CanExecuteChanged;
}
```

### Verwendung im ViewModel

```cs
// Command definieren
public ICommand SchussCommand { get; set; }

// Im Konstruktor initialisieren
SchussCommand = new RelayCommand(SchiesseAuf);

// Methode
private void SchiesseAuf(object parameter)
{
    Feld feld = parameter as Feld; // CommandParameter aus XAML
    // ...
}
```

### Im XAML binden

```xml
<Button Command="{Binding SchussCommand}"
        CommandParameter="{Binding}" <!-- das aktuelle Feld-Objekt -->
        Content="{Binding AnzeigeText}"/>
```

---

## 5. Spielfeld aufbauen (ObservableCollection)

```cs
// Im ViewModel
public ObservableCollection<Feld> EigenesSpielfeld { get; set; }
public ObservableCollection<Feld> GegnerListe { get; set; }

// Spielfeld initialisieren (10x10 = 100 Felder)
private void SpielfelderErstellen(int groesse)
{
    EigenesSpielfeld = new ObservableCollection<Feld>();
    GegnerListe      = new ObservableCollection<Feld>();

    for (int y = 0; y < groesse; y++)
    {
        for (int x = 0; x < groesse; x++)
        {
            EigenesSpielfeld.Add(new Feld(x, y));
            GegnerListe.Add(new Feld(x, y));
        }
    }

    // Commands für jede Zelle setzen
    foreach (var feld in EigenesSpielfeld)
        feld.KlickCommand = new RelayCommand(SchiffSetzen);

    foreach (var feld in GegnerListe)
        feld.KlickCommand = new RelayCommand(SchiesseAuf);
}

// Index aus X/Y berechnen (für Netzwerk-Kommunikation)
private int GetIndex(int x, int y, int groesse) => y * groesse + x;

// X/Y aus Index berechnen
private int GetX(int index, int groesse) => index % groesse;
private int GetY(int index, int groesse) => index / groesse;
```

---

## 6. XAML – Spielfeld als Grid darstellen (WrapPanel)

```xml
<ListBox ItemsSource="{Binding EigenesSpielfeld}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <!-- WrapPanel + feste Item-Breite = automatisches Grid -->
            <WrapPanel Width="300"/> <!-- 10 Felder × 30px = 300 -->
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Button Width="30" Height="30"
                    Content="{Binding AnzeigeText}"
                    Foreground="{Binding TextFarbe}"
                    Background="{Binding BackgFarbe}"
                    Command="{Binding KlickCommand}"
                    CommandParameter="{Binding}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

> **Trick für festes Grid:** WrapPanel-Width = Feldanzahl × Feldbreite
> Bei 10×10 mit 30px breiten Feldern: WrapPanel Width="300"

---

## 7. Schiff setzen (Platzierungslogik)

```cs
// Ausgewähltes Schiff (gebunden an Schiff-Auswahl Buttons)
private VerfuegbaresSchiff _ausgewaehltesSchiff;
public VerfuegbaresSchiff AusgewaehltesSchiff
{
    get => _ausgewaehltesSchiff;
    set { _ausgewaehltesSchiff = value; OnPropertyChanged(); }
}

private Orientierung _orientierung = Orientierung.Horizontal;

// Wird aufgerufen wenn Feld auf eigenem Spielfeld geklickt
private void SchiffSetzen(object parameter)
{
    if (AusgewaehltesSchiff == null || AusgewaehltesSchiff.Anzahl <= 0) return;

    Feld startFeld = (Feld)parameter;
    int laenge = (int)AusgewaehltesSchiff.Typ;

    // Alle Felder die das Schiff belegen würde sammeln
    var felder = new List<Feld>();
    for (int i = 0; i < laenge; i++)
    {
        int x = _orientierung == Orientierung.Horizontal ? startFeld.X + i : startFeld.X;
        int y = _orientierung == Orientierung.Vertikal   ? startFeld.Y + i : startFeld.Y;

        // Außerhalb des Feldes?
        if (x >= Groesse || y >= Groesse) return;

        var feld = EigenesSpielfeld[GetIndex(x, y, Groesse)];

        // Bereits belegt?
        if (feld.Zustand == FeldZustand.Schiff) return;

        felder.Add(feld);
    }

    // Schiff platzieren
    var schiff = new GesetztesSchiff(laenge) { Felder = felder };

    foreach (var feld in felder)
    {
        feld.Zustand = FeldZustand.Schiff;
        feld.SchiffReferenz = schiff;
    }

    AusgewaehltesSchiff.Anzahl--;
}
```

---

## 8. Treffer prüfen (CheckHit)

Wird aufgerufen wenn Gegner auf eigenes Spielfeld schießt.

```cs
// Gibt zurück: welcher Befehl an Gegner zurückgeschickt wird
public Netzwerkbefehl CheckHit(int index, out string payload)
{
    var feld = EigenesSpielfeld[index];

    if (feld.Zustand != FeldZustand.Schiff)
    {
        // Fehlschuss
        feld.Zustand = FeldZustand.Fehlschuss;
        payload = index.ToString();
        return Netzwerkbefehl.WASSER;
    }

    // Treffer
    var schiff = feld.SchiffReferenz;
    schiff.Leben--;
    feld.Zustand = FeldZustand.Treffer;

    if (schiff.Leben <= 0)
    {
        // Versenkt → alle Felder des Schiffs als Versenkt markieren
        foreach (var f in schiff.Felder)
            f.Zustand = FeldZustand.Versenkt;

        // Indizes aller Felder als Payload senden (z.B. "3,4,5")
        payload = string.Join(",", schiff.Felder.Select(f => GetIndex(f.X, f.Y, Groesse)));

        // Alle eigenen Schiffe versenkt? → verloren
        if (AlleSchiffeVersenkt())
        {
            _netzwerk.sendMessage(Netzwerkbefehl.VERSENKT, payload);
            _netzwerk.sendMessage(Netzwerkbefehl.VERLOREN, payload);
            SpielStatus = "Du hast verloren!";
            return Netzwerkbefehl.VERSENKT; // wird dann nochmal gesendet - ggf. anpassen
        }

        return Netzwerkbefehl.VERSENKT;
    }

    payload = index.ToString();
    return Netzwerkbefehl.TREFFER;
}

private bool AlleSchiffeVersenkt()
    => EigenesSpielfeld.All(f => f.Zustand != FeldZustand.Schiff);
```

---

## 9. Netzwerk – Text-Protokoll

Format: `"BEFEHL|payload"` → z.B. `"SCHUSS|42"` oder `"VERSENKT|3,4,5,6"`

### Nachricht senden

```cs
// Einfach
_netzwerk.sendMessage(Netzwerkbefehl.SCHUSS, "42");

// Ohne Payload
_netzwerk.sendMessage(Netzwerkbefehl.OK);
```

### Nachricht empfangen + verarbeiten (HandleMessage)

```cs
private void HandleMessage(string message)
{
    string[] parts = message.Split('|');
    string command = parts[0];
    string payload = parts.Length > 1 ? parts[1] : "";

    switch (command)
    {
        case "SCHUSS":
            int idx = int.Parse(payload);
            Netzwerkbefehl antwort = _viewModel.CheckHit(idx, out string antwortPayload);
            sendMessage(antwort, antwortPayload);
            break;

        case "WASSER":
            _viewModel.GegnerListe[int.Parse(payload)].Zustand = FeldZustand.Fehlschuss;
            sendMessage(Netzwerkbefehl.OK);
            break;

        case "TREFFER":
            _viewModel.GegnerListe[int.Parse(payload)].Zustand = FeldZustand.Treffer;
            sendMessage(Netzwerkbefehl.OK);
            break;

        case "VERSENKT":
            foreach (string idxStr in payload.Split(','))
                _viewModel.GegnerListe[int.Parse(idxStr)].Zustand = FeldZustand.Versenkt;
            sendMessage(Netzwerkbefehl.OK);
            break;

        case "OK":
            _viewModel.SwapTurn(); // Zug wechseln
            break;

        case "FERTIG":
            _viewModel.ClientReady(); // Client hat Schiffe gesetzt
            break;

        case "STARTEN":
            _viewModel.SpielStarten();
            break;

        case "VERLOREN":
            _viewModel.SpielStatus = "Du hast gewonnen!";
            foreach (string idxStr in payload.Split(','))
                _viewModel.GegnerListe[int.Parse(idxStr)].Zustand = FeldZustand.Versenkt;
            break;
    }
}
```

### NetzwerkManager aufbauen (Host oder Client)

```cs
// Im ViewModel
private NetzwerkManager _netzwerk;

// Host starten
public void AlsHostStarten()
{
    _netzwerk = new NetzwerkManager(this);
    _ = _netzwerk.StarteHost(); // fire & forget (async ohne await)
}

// Client starten
public void AlsClientStarten(string ip)
{
    _netzwerk = new NetzwerkManager(this);
    _ = _netzwerk.StarteClient(ip, 12345);
}
```

---

## 10. Zug-Wechsel (SwapTurn)

```cs
private bool _istMeinZug = false;
public string SpielStatus { get; set; }

public void SwapTurn()
{
    _istMeinZug = !_istMeinZug;
    SpielStatus = _istMeinZug ? "Du bist dran!" : "Gegner ist dran...";
}

// Beim Schuss: nur wenn eigener Zug
private void SchiesseAuf(object parameter)
{
    if (!_istMeinZug) return;

    Feld feld = (Feld)parameter;
    if (feld.Zustand != FeldZustand.Wasser) return; // bereits beschossen

    int index = GetIndex(feld.X, feld.Y, Groesse);
    _netzwerk.sendMessage(Netzwerkbefehl.SCHUSS, index.ToString());

    _istMeinZug = false; // warten auf Antwort (SwapTurn kommt bei OK)
}
```

---

## 11. Ablauf Spielstart

```
HOST                          CLIENT
  │                              │
  │←── AcceptTcpClientAsync ─────│  (Verbindung)
  │                              │
  │  beide setzen Schiffe        │
  │                              │
  │←────────── FERTIG ───────────│  (Client fertig)
  │                              │
  │──────────── STARTEN ────────→│  (Host startet)
  │                              │
  │  Spiel beginnt               │
  │  Host schießt zuerst         │
```

---

## 12. Schnellreferenz

### Index ↔ X/Y

```cs
int index = y * groesse + x;   // X/Y → Index
int x = index % groesse;       // Index → X
int y = index / groesse;       // Index → Y
```

### LINQ für Spielfeld

```cs
// Alle Felder mit Schiff
EigenesSpielfeld.Where(f => f.Zustand == FeldZustand.Schiff)

// Alle Schiffe versenkt?
EigenesSpielfeld.All(f => f.Zustand != FeldZustand.Schiff)

// Indizes aller Felder eines Schiffs
string.Join(",", schiff.Felder.Select(f => GetIndex(f.X, f.Y, groesse)))
```

### INotifyPropertyChanged (immer gleich kopieren)

```cs
public event PropertyChangedEventHandler? PropertyChanged;
protected void OnPropertyChanged([CallerMemberName] string? n = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
```

### Dispatcher (wenn Netzwerk → UI)

```cs
Application.Current.Dispatcher.Invoke(() => {
    // UI-Updates hier
    SpielStatus = "...";
    GegnerListe[idx].Zustand = FeldZustand.Treffer;
});
```
