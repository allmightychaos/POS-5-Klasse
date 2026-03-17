# Dialog-Pattern – Referenz

---

## Texteingabe-Dialog

### 1. Neues Fenster anlegen
Rechtsklick auf Projekt → Hinzufügen → Fenster (WPF) → Name: `TextEingabeDialog.xaml`

### TextEingabeDialog.xaml
```xml
<Window x:Class="MeinProjekt.TextEingabeDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Eingabe" Height="150" Width="300"
        WindowStartupLocation="CenterOwner">
    <StackPanel Margin="10">
        <TextBlock Text="Name eingeben:" Margin="0,0,0,5"/>
        <TextBox x:Name="EingabeTextBox" Margin="0,0,0,10"/>
        <Button Content="OK" Click="OK_Click"/>
    </StackPanel>
</Window>
```

### TextEingabeDialog.xaml.cs
```cs
public partial class TextEingabeDialog : Window
{
    // Hier holt der Aufrufer den eingegebenen Text ab
    public string EingabeText { get; private set; } = "";

    public TextEingabeDialog() => InitializeComponent();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        EingabeText = EingabeTextBox.Text.Trim();
        DialogResult = true; // schließt Fenster, ShowDialog() gibt true zurück
    }
}
```

### Aufrufen (z.B. in MainWindow.xaml.cs)
```cs
var dialog = new TextEingabeDialog();
bool? result = dialog.ShowDialog(); // blockiert bis Fenster geschlossen

if (result == true)
{
    string text = dialog.EingabeText;
    // weiterverarbeiten...
}
```

---

## Auswahl-Dialog (ComboBox aus Liste)

### AuswahlDialog.xaml
```xml
<Window x:Class="MeinProjekt.AuswahlDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Auswahl" Height="150" Width="300"
        WindowStartupLocation="CenterOwner">
    <StackPanel Margin="10">
        <TextBlock Text="Bitte wählen:" Margin="0,0,0,5"/>
        <ComboBox x:Name="AuswahlComboBox" Margin="0,0,0,10"/>
        <Button Content="OK" Click="OK_Click"/>
    </StackPanel>
</Window>
```

### AuswahlDialog.xaml.cs
```cs
public partial class AuswahlDialog : Window
{
    // Hier holt der Aufrufer das gewählte Objekt ab
    public MeinObjekt? GewaehlterEintrag { get; private set; }

    public AuswahlDialog(List<MeinObjekt> liste)
    {
        InitializeComponent();
        AuswahlComboBox.ItemsSource = liste;
        AuswahlComboBox.DisplayMemberPath = "Name"; // welche Property anzeigen
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        GewaehlterEintrag = AuswahlComboBox.SelectedItem as MeinObjekt;
        DialogResult = true;
    }
}
```

### Aufrufen
```cs
using var ctx = new AppDbContext();
var alleEintraege = ctx.Eintraege.ToList();

var dialog = new AuswahlDialog(alleEintraege);
bool? result = dialog.ShowDialog();

if (result == true)
{
    var gewahlt = dialog.GewaehlterEintrag;
    if (gewahlt == null) return;

    // WICHTIG: gespeicherte Werte verwenden, nicht GUI auslesen!
    _viewModel.Breite       = gewahlt.Breite;
    _viewModel.Spielnummer  = gewahlt.Spielnummer;
    // ...
}
```

---

## Wichtige Punkte

| Was | Warum |
|---|---|
| `DialogResult = true` | signalisiert dem Aufrufer dass OK gedrückt wurde |
| `ShowDialog()` gibt `bool?` zurück | kann `true`, `false` oder `null` sein (X gedrückt = null) |
| `result == true` statt `(bool)result` | sicher, kein Crash bei null |
| `WindowStartupLocation="CenterOwner"` | Dialog erscheint zentriert über dem Hauptfenster |
| `DisplayMemberPath="Name"` | ComboBox zeigt Property "Name" an statt Objekttyp |
| Public Property im Dialog | einziger Weg wie Aufrufer an den Wert kommt |

---

## ⚠️ Nach dem Laden: DB-Werte verwenden, NICHT GUI auslesen

Wenn ein gespeichertes Spiel geladen und dann an den Server gesendet wird,
immer die Werte aus dem DB-Objekt nehmen – nie aus TextBoxen oder anderen UI-Elementen!

```cs
// FALSCH – GUI auslesen (TextBoxen könnten andere Werte haben!)
int breite = int.Parse(BreiteTextBox.Text);
int hoehe  = int.Parse(HoeheTextBox.Text);
_netzwerk.SendePaket(new Paket("NEW", $"{breite},{hoehe}"));

// RICHTIG – direkt aus dem geladenen DB-Objekt
_netzwerk.SendePaket(new Paket("NEW",
    $"{gewahlt.Breite},{gewahlt.Hoehe},{gewahlt.Minen},{gewahlt.Spielnummer}"));
```

Warum? Die TextBoxen zeigen vielleicht noch Werte vom letzten Spiel an,
oder der Spieler hat sie zwischenzeitlich geändert. Das gespeicherte Objekt
enthält immer die korrekten Werte zum Zeitpunkt des Speicherns.

---

# Client-Server mit ORM, XML, WPF-Karte & Traveling Salesman – Referenz

---

## 1. ORM – SQLite mit Entity Framework Core

### NuGet-Pakete
```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Sqlite
```

### Model
```cs
public class Stadt
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

### DbContext
```cs
public class AppDbContext : DbContext
{
    public DbSet<Stadt> Staedte { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=staedte.db");
}
```

### Datenbank laden
```cs
using var ctx = new AppDbContext();
var alle = ctx.Staedte.ToList();
```

---

## 2. Netzwerk – TCP Server & Client (XML, mehrere Clients)

### Server (Port 12345, mehrere Clients gleichzeitig)
```cs
TcpListener listener = new TcpListener(IPAddress.Any, 12345);
listener.Start();

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClient(client)); // jeder Client bekommt eigenen Task
}

async Task HandleClient(TcpClient client)
{
    using var stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8);
    using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

    string? zeile = await reader.ReadLineAsync();
    if (zeile == null) return;

    // XML deserialisieren
    var anfrage = XmlHelper.Deserialize<Anfrage>(zeile);

    // Antwort erstellen und als XML senden
    var antwort = new Antwort { Ergebnis = "OK" };
    writer.WriteLine(XmlHelper.Serialize(antwort));
}
```

### Client (fix localhost:12345)
```cs
TcpClient client = new TcpClient("localhost", 12345);
var stream = client.GetStream();
var reader = new StreamReader(stream, Encoding.UTF8);
var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

// XML senden
writer.WriteLine(XmlHelper.Serialize(anfrage));

// Antwort empfangen
string antwortXml = reader.ReadLine()!;
var antwort = XmlHelper.Deserialize<Antwort>(antwortXml);
```

### XML-Hilfsmethoden
```cs
public static class XmlHelper
{
    public static string Serialize<T>(T obj)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true });
        new XmlSerializer(typeof(T)).Serialize(writer, obj);
        return sb.ToString();
    }

    public static T Deserialize<T>(string xml)
    {
        using var reader = new StringReader(xml);
        return (T)new XmlSerializer(typeof(T)).Deserialize(reader)!;
    }
}
```

### Transfer-Objekte (XML-serialisierbar)
```cs
[Serializable]
public class Anfrage
{
    public string Suchbegriff { get; set; } = "";
}

[Serializable]
public class Antwort
{
    public List<StadtDto> Staedte { get; set; } = new();
}

[Serializable]
public class StadtDto
{
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

---

## 3. GUI – WPF mit Weltkarte & Suchfeld

### MainWindow.xaml (Grundstruktur)
```xml
<Window x:Class="Client.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Stadtsuche" Height="600" Width="900">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="200"/>
        </Grid.ColumnDefinitions>

        <!-- Suchleiste -->
        <StackPanel Orientation="Horizontal" Grid.Row="0" Grid.ColumnSpan="2" Margin="5">
            <TextBox x:Name="SuchTextBox" Width="200" Margin="0,0,5,0"/>
            <Button Content="Suchen" Click="Suchen_Click"/>
        </StackPanel>

        <!-- Weltkarte -->
        <Canvas x:Name="WeltkarteCanvas" Grid.Row="1" Grid.Column="0" Background="LightBlue">
            <Image Source="weltkarte.png" Stretch="Fill"
                   Width="{Binding ActualWidth, RelativeSource={RelativeSource AncestorType=Canvas}}"
                   Height="{Binding ActualHeight, RelativeSource={RelativeSource AncestorType=Canvas}}"/>
        </Canvas>

        <!-- Rechte Seite: ListBoxen -->
        <StackPanel Grid.Row="1" Grid.Column="1" Margin="5">
            <TextBlock Text="Suchergebnisse:"/>
            <ListBox x:Name="ErgebnisListBox" Height="200" SelectionMode="Extended"
                     DisplayMemberPath="Name"/>
            <Button Content=">> Auswählen" Click="Auswaehlen_Click" Margin="0,5"/>
            <TextBlock Text="Ausgewählte Städte:"/>
            <ListBox x:Name="AuswahlListBox" Height="150" DisplayMemberPath="Name"/>
            <Button Content="Route berechnen" Click="Route_Click" Margin="0,5"/>
        </StackPanel>
    </Grid>
</Window>
```

### Suchen-Button: Suchbegriff an Server senden
```cs
private async void Suchen_Click(object sender, RoutedEventArgs e)
{
    string suchbegriff = SuchTextBox.Text.Trim();
    if (string.IsNullOrEmpty(suchbegriff)) return;

    var anfrage = new Anfrage { Suchbegriff = suchbegriff };
    // XML senden (Netzwerk-Verbindung vorher herstellen, siehe oben)
    _writer.WriteLine(XmlHelper.Serialize(anfrage));

    string antwortXml = await Task.Run(() => _reader.ReadLine()!);
    var antwort = XmlHelper.Deserialize<Antwort>(antwortXml);

    // Karte aktualisieren + ListBox befüllen
    ZeigeStaedteAufKarte(antwort.Staedte);
    ErgebnisListBox.ItemsSource = antwort.Staedte;
}
```

---

## 4. LINQ – Städte in der Datenbank suchen (Server)

```cs
// Suche: Name enthält den Suchbegriff (Groß-/Kleinschreibung egal)
using var ctx = new AppDbContext();
string suche = anfrage.Suchbegriff.ToLower();

List<StadtDto> treffer = ctx.Staedte
    .Where(s => s.Name.ToLower().Contains(suche))
    .Select(s => new StadtDto
    {
        Name      = s.Name,
        Latitude  = s.Latitude,
        Longitude = s.Longitude
    })
    .ToList();
```

---

## 5. Städte auf der Weltkarte anzeigen

```cs
private void ZeigeStaedteAufKarte(List<StadtDto> staedte)
{
    // Alte Marker entfernen (nur Ellipsen, nicht das Bild)
    var zuEntfernen = WeltkarteCanvas.Children.OfType<Ellipse>().ToList();
    foreach (var el in zuEntfernen)
        WeltkarteCanvas.Children.Remove(el);

    double breite = WeltkarteCanvas.ActualWidth;
    double hoehe  = WeltkarteCanvas.ActualHeight;

    foreach (var stadt in staedte)
    {
        // Koordinaten auf Canvas umrechnen
        double x = (stadt.Longitude + 180) / 360 * breite;
        double y = (90 - stadt.Latitude)  / 180 * hoehe;

        var punkt = new Ellipse
        {
            Width  = 8,
            Height = 8,
            Fill   = Brushes.Red,
            ToolTip = stadt.Name
        };
        Canvas.SetLeft(punkt, x - 4);
        Canvas.SetTop(punkt,  y - 4);
        WeltkarteCanvas.Children.Add(punkt);
    }
}
```

---

## 6. Städte auswählen (ListBox → zweite ListBox)

```cs
private void Auswaehlen_Click(object sender, RoutedEventArgs e)
{
    foreach (StadtDto stadt in ErgebnisListBox.SelectedItems)
    {
        // Nur hinzufügen wenn noch nicht enthalten
        var liste = (AuswahlListBox.ItemsSource as List<StadtDto>) ?? new List<StadtDto>();
        if (!liste.Any(s => s.Name == stadt.Name))
            liste.Add(stadt);
        AuswahlListBox.ItemsSource = null;
        AuswahlListBox.ItemsSource = liste;
    }
}
```

> **Wichtig:** `AuswahlListBox` wird bei einer neuen Suche **nicht** zurückgesetzt –
> nur `ErgebnisListBox` bekommt neue Daten.

---

## 7. Traveling Salesman – kürzeste Route (Nearest Neighbor)

```cs
private void Route_Click(object sender, RoutedEventArgs e)
{
    var staedte = (AuswahlListBox.ItemsSource as List<StadtDto>)?.ToList();
    if (staedte == null || staedte.Count < 2) return;

    var route = NearestNeighbor(staedte);
    ZeigeRouteAufKarte(route);
}

// Nearest-Neighbor-Heuristik
List<StadtDto> NearestNeighbor(List<StadtDto> staedte)
{
    var verbleibend = staedte.ToList();
    var route = new List<StadtDto> { verbleibend[0] };
    verbleibend.RemoveAt(0);

    while (verbleibend.Count > 0)
    {
        var letzter = route.Last();
        var naechster = verbleibend
            .OrderBy(s => Distanz(letzter, s))
            .First();
        route.Add(naechster);
        verbleibend.Remove(naechster);
    }
    return route;
}

// Euklidische Distanz auf Basis von Lat/Lon
double Distanz(StadtDto a, StadtDto b)
{
    double dx = a.Longitude - b.Longitude;
    double dy = a.Latitude  - b.Latitude;
    return Math.Sqrt(dx * dx + dy * dy);
}

// Route als Linien auf Canvas zeichnen
void ZeigeRouteAufKarte(List<StadtDto> route)
{
    // Alte Linien entfernen
    var linien = WeltkarteCanvas.Children.OfType<Line>().ToList();
    foreach (var l in linien) WeltkarteCanvas.Children.Remove(l);

    double breite = WeltkarteCanvas.ActualWidth;
    double hoehe  = WeltkarteCanvas.ActualHeight;

    for (int i = 0; i < route.Count - 1; i++)
    {
        var von = route[i];
        var bis = route[i + 1];

        var linie = new Line
        {
            X1 = (von.Longitude + 180) / 360 * breite,
            Y1 = (90 - von.Latitude)   / 180 * hoehe,
            X2 = (bis.Longitude + 180) / 360 * breite,
            Y2 = (90 - bis.Latitude)   / 180 * hoehe,
            Stroke          = Brushes.DarkBlue,
            StrokeThickness = 2
        };
        WeltkarteCanvas.Children.Add(linie);
    }
}
```

---

## Schnellübersicht

| Thema | Wichtigste Klassen / Methoden |
|---|---|
| ORM | `DbContext`, `DbSet<T>`, `UseSqlite(...)`, `.ToList()` |
| Server | `TcpListener`, `AcceptTcpClientAsync`, `Task.Run(HandleClient)` |
| Client | `TcpClient("localhost", 12345)`, `StreamReader/Writer` |
| XML | `XmlSerializer`, `Serialize<T>`, `Deserialize<T>` |
| LINQ | `.Where(...)`, `.Select(...)`, `.Contains(...)`, `.ToList()` |
| Karte | `Canvas`, `Ellipse`, `Canvas.SetLeft/Top`, Koordinaten umrechnen |
| ListBox | `ItemsSource`, `SelectedItems`, `SelectionMode="Extended"` |
| TSP | Nearest Neighbor: immer zur nächsten unbesuchten Stadt |
