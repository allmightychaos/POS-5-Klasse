# WPF + Netzwerk + ORM – Referenz

---

## WPF GUI

### Grundstruktur

- `Window` → `Grid` mit `RowDefinitions` / `ColumnDefinitions`
- Menü ganz oben → `<Menu>` in `RowDefinition Height="Auto"`
- Eingabebereich → `StackPanel Orientation="Horizontal"` mit `TextBox`, `ComboBox`, `Button`
- Hauptbereich → `ListBox` in `RowDefinition Height="*"` (füllt Rest)

### Wichtige Controls & Parameter

| Control     | Wichtige Properties                                                          |
| ----------- | ---------------------------------------------------------------------------- |
| `TextBox`   | `x:Name`, `Width`, `Text`                                                    |
| `ComboBox`  | `x:Name`, `ItemsSource="{Binding ...}"`, `SelectedItem`, `DisplayMemberPath` |
| `Button`    | `Content`, `Click="Handler_Name"`                                            |
| `ListBox`   | `x:Name`, `ItemsSource="{Binding ...}"`, `SelectionMode`                     |
| `TextBlock` | `Text="{Binding ...}"`                                                       |

### ListBox mit WrapPanel (Elemente nebeneinander)

```xml
<ListBox ItemsSource="{Binding MeineCollection}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <!-- WrapPanel = Elemente nebeneinander, umbrechen automatisch -->
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <!-- wie sieht ein einzelnes Element aus -->
            <Border Width="40" Height="40" BorderBrush="Gray" BorderThickness="1"
                    MouseLeftButtonDown="Element_Geklickt">
                <TextBlock Text="{Binding Wert}"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"/>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Menü

```xml
<Menu>
    <MenuItem Header="Datei">
        <MenuItem Header="Speichern" Click="Speichern_Click"/>
        <MenuItem Header="Laden"     Click="Laden_Click"/>
    </MenuItem>
</Menu>
```

---

## Datenmodell + INotifyPropertyChanged

```cs
public class MeinElement : INotifyPropertyChanged
{
    private int _wert = -1;
    public int Wert
    {
        get => _wert;
        set { _wert = value; OnPropertyChanged(); }
    }

    public int Index { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

### Collection initialisieren (z.B. Grid befüllen)

```cs
public void CollectionBefüllen(int breite, int höhe)
{
    MeineCollection = new ObservableCollection<MeinElement>();
    for (int i = 0; i < breite * höhe; i++)
        MeineCollection.Add(new MeinElement { Index = i, Wert = -1 });
    OnPropertyChanged(nameof(MeineCollection));
}
```

### Collection als String speichern / laden (für DB)

```cs
// Collection → kommaseparierter String
string CollectionAlsString()
    => string.Join(",", MeineCollection.Select(e => e.Wert));

// String → Collection wiederherstellen
void CollectionAusString(string daten)
{
    var werte = daten.Split(',').Select(int.Parse).ToList();
    MeineCollection = new ObservableCollection<MeinElement>();
    for (int i = 0; i < werte.Count; i++)
        MeineCollection.Add(new MeinElement { Index = i, Wert = werte[i] });
    OnPropertyChanged(nameof(MeineCollection));
}
```

---

## Klick-Handling (Element in ListBox klicken)

```cs
// Code-Behind
private void Element_Geklickt(object sender, MouseButtonEventArgs e)
{
    if ((sender as FrameworkElement)?.DataContext is MeinElement el)
    {
        if (el.Wert != -1) return; // nur anklickbar wenn noch unbekannt

        _viewModel.ElementGewählt(el.Index);
    }
}

// ViewModel
public void ElementGewählt(int index)
{
    _netzwerk.SendePaket(new Paket("AKTION", index.ToString()));
}
```

### Antwort vom Server verarbeiten

```cs
public void PaketVerarbeiten(Paket paket)
{
    switch (paket.Befehl)
    {
        case "ERGEBNIS":
            var teile = paket.Inhalt.Split(',');
            int idx  = int.Parse(teile[0]);
            int wert = int.Parse(teile[1]);
            MeineCollection[idx].Wert = wert;
            break;

        case "FEHLER":
            // MessageBox muss im UI-Thread laufen!
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show("Etwas ist schiefgelaufen.", "Info"));
            break;
    }
}
```

---

## ORM – SQLite mit Entity Framework Core

### NuGet Packages

```
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Design
```

### Modell-Klasse

```cs
public class Eintrag
{
    public int Id { get; set; }            // Primary Key (auto)
    public string Name { get; set; } = ""; // eindeutiger Name
    public int Breite { get; set; }
    public int Höhe { get; set; }
    public int AnzahlX { get; set; }       // z.B. Minen, Schiffe, ...
    public int ServerNummer { get; set; }  // ID/Nummer vom Server
    public string Felddaten { get; set; } = ""; // kommaseparierte Werte
}
```

### DbContext

```cs
public class AppDbContext : DbContext
{
    public DbSet<Eintrag> Eintraege { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string pfad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "daten.db");
        options.UseSqlite($"Data Source={pfad}");
    }
}
```

### DB initialisieren (einmalig beim Start)

```cs
using var ctx = new AppDbContext();
ctx.Database.EnsureCreated(); // erstellt DB + Tabellen wenn nicht vorhanden
```

---

## Dialog-Pattern

Immer gleich – egal ob Namenseingabe oder Auswahl aus Liste:

```cs
var dialog = new MeinDialog(optionaleParameter);
bool? result = dialog.ShowDialog(); // blockiert bis Fenster geschlossen
if (result == true)
{
    var wert = dialog.MeinePublicProperty;
}
```

### Dialog: Texteingabe

```cs
public partial class TextEingabeDialog : Window
{
    public string EingabeText { get; private set; } = "";

    public TextEingabeDialog() => InitializeComponent();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        EingabeText = MeineTextBox.Text.Trim();
        DialogResult = true; // schließt Fenster & gibt true zurück
    }
}
```

### Dialog: Auswahl aus Liste

```cs
public partial class AuswahlDialog : Window
{
    public Eintrag? GewähltesElement { get; private set; }

    public AuswahlDialog(List<Eintrag> liste)
    {
        InitializeComponent();
        MeineComboBox.ItemsSource = liste;
        MeineComboBox.DisplayMemberPath = "Name"; // zeigt Name-Property an
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        GewähltesElement = MeineComboBox.SelectedItem as Eintrag;
        DialogResult = true;
    }
}
```

---

## Daten speichern & laden (DB)

### Speichern (mit Eindeutigkeitsprüfung)

```cs
using var ctx = new AppDbContext();

// Prüfen ob Name bereits vorhanden
bool existiert = ctx.Eintraege.Any(e => e.Name == name);
if (existiert)
{
    MessageBox.Show("Name bereits vorhanden!", "Fehler");
    return;
}

ctx.Eintraege.Add(new Eintrag
{
    Name         = name,
    Breite       = _viewModel.Breite,
    Höhe         = _viewModel.Höhe,
    AnzahlX      = _viewModel.AnzahlX,
    ServerNummer = _viewModel.ServerNummer,
    Felddaten    = _viewModel.CollectionAlsString()
});
ctx.SaveChanges();
```

### Laden

```cs
using var ctx = new AppDbContext();
var alleEintraege = ctx.Eintraege.ToList();

var dialog = new AuswahlDialog(alleEintraege);
if (dialog.ShowDialog() != true) return;

var gewählt = dialog.GewähltesElement;
if (gewählt == null) return;

// WICHTIG: gespeicherte Werte verwenden, NICHT GUI-Elemente auslesen!
_viewModel.Breite       = gewählt.Breite;
_viewModel.Höhe         = gewählt.Höhe;
_viewModel.AnzahlX      = gewählt.AnzahlX;
_viewModel.ServerNummer = gewählt.ServerNummer;
_viewModel.CollectionAusString(gewählt.Felddaten);
```

---

## Schnellreferenz

### LINQ

```cs
ctx.Eintraege.Any(e => e.Name == "x")      // existiert?
ctx.Eintraege.ToList()                      // alle laden
ctx.Eintraege.FirstOrDefault(e => e.Id==1) // einzelnes laden
ctx.Eintraege.Where(e => e.AnzahlX > 5)    // filtern
```

### ORM CRUD

```cs
ctx.Eintraege.Add(neu); ctx.SaveChanges();  // Create
ctx.Eintraege.ToList();                     // Read
s.Felddaten = neu; ctx.SaveChanges();       // Update
ctx.Eintraege.Remove(s); ctx.SaveChanges(); // Delete
```

### Dispatcher (Netzwerk-Thread → UI)

```cs
Application.Current.Dispatcher.Invoke(() => {
    // UI-Updates hier sicher ausführen
});
```

### INotifyPropertyChanged (immer gleich kopieren)

```cs
public event PropertyChangedEventHandler? PropertyChanged;
protected void OnPropertyChanged([CallerMemberName] string? n = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
```
