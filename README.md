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

# Netzwerk-Anwendung mit Datenbank – Referenz

---

## Entity Framework Core – SQLite

### NuGet-Pakete
```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Sqlite
```

### Model & DbContext
```cs
public class Produkt
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double X { get; set; }  // z.B. Position, Koordinate, Preis, ...
    public double Y { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Produkt> Produkte { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}
```

### Daten laden
```cs
using var ctx = new AppDbContext();
var alle = ctx.Produkte.ToList();
```

---

## TCP-Server (mehrere Clients gleichzeitig)

```cs
TcpListener listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();

while (true)
{
    TcpClient tcpClient = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => ClientVerarbeiten(tcpClient));
}

async Task ClientVerarbeiten(TcpClient tcpClient)
{
    using var stream = tcpClient.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8);
    using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

    string? zeile = await reader.ReadLineAsync();
    if (zeile == null) return;

    var anfrage = XmlHelper.Deserialize<Anfrage>(zeile);
    // ... verarbeiten ...
    var antwort = new Antwort();
    writer.WriteLine(XmlHelper.Serialize(antwort));
}
```

## TCP-Client

```cs
TcpClient client = new TcpClient("localhost", 5000);
var stream = client.GetStream();
var reader = new StreamReader(stream, Encoding.UTF8);
var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

writer.WriteLine(XmlHelper.Serialize(anfrage));
string xml = reader.ReadLine()!;
var antwort = XmlHelper.Deserialize<Antwort>(xml);
```

---

## XML serialisieren / deserialisieren

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

### Beispiel Transfer-Objekte
```cs
[Serializable] public class Anfrage  { public string Filter { get; set; } = ""; }
[Serializable] public class Antwort  { public List<EintragDto> Eintraege { get; set; } = new(); }
[Serializable] public class EintragDto { public string Name { get; set; } = ""; public double X { get; set; } public double Y { get; set; } }
```

---

## LINQ – Filtern & Projizieren

```cs
using var ctx = new AppDbContext();
string filter = anfrage.Filter.ToLower();

var treffer = ctx.Produkte
    .Where(p => p.Name.ToLower().Contains(filter))
    .Select(p => new EintragDto { Name = p.Name, X = p.X, Y = p.Y })
    .ToList();
```

Weitere nützliche LINQ-Operationen:

```cs
.OrderBy(p => p.Name)          // sortieren
.OrderByDescending(p => p.X)
.First()                       // erstes Element
.FirstOrDefault()              // erstes oder null
.Count()                       // Anzahl
.Sum(p => p.X)                 // Summe
.Any(p => p.Name == "Test")    // existiert?
```

---

## WPF – Canvas mit Bild & Punkte zeichnen

### XAML
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="220"/>
    </Grid.ColumnDefinitions>

    <!-- Canvas mit Hintergrundbild -->
    <Canvas x:Name="HauptCanvas" Grid.Column="0" Background="LightBlue">
        <Image Source="hintergrund.png" Stretch="Fill"
               Width="{Binding ActualWidth, RelativeSource={RelativeSource AncestorType=Canvas}}"
               Height="{Binding ActualHeight, RelativeSource={RelativeSource AncestorType=Canvas}}"/>
    </Canvas>

    <!-- Seitenleiste -->
    <StackPanel Grid.Column="1" Margin="5">
        <TextBox x:Name="FilterTextBox" Margin="0,0,0,5"/>
        <Button Content="Suchen" Click="Suchen_Click" Margin="0,0,0,10"/>
        <ListBox x:Name="ErgebnisListBox" Height="180" SelectionMode="Extended"
                 DisplayMemberPath="Name"/>
        <Button Content="Hinzufügen" Click="Hinzufuegen_Click" Margin="0,5"/>
        <ListBox x:Name="AuswahlListBox" Height="130" DisplayMemberPath="Name"/>
        <Button Content="Auswertung" Click="Auswertung_Click" Margin="0,5"/>
    </StackPanel>
</Grid>
```

### Punkte auf Canvas setzen (X/Y als Koordinaten)
```cs
void ZeigePunkteAufCanvas(List<EintragDto> eintraege)
{
    // Nur Ellipsen entfernen, nicht das Hintergrundbild
    foreach (var el in HauptCanvas.Children.OfType<Ellipse>().ToList())
        HauptCanvas.Children.Remove(el);

    double w = HauptCanvas.ActualWidth;
    double h = HauptCanvas.ActualHeight;

    foreach (var e in eintraege)
    {
        // Beispiel: X/Y sind Längen-/Breitengrad → auf Canvas umrechnen
        double cx = (e.X + 180) / 360 * w;
        double cy = (90 - e.Y)  / 180 * h;

        var kreis = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Red, ToolTip = e.Name };
        Canvas.SetLeft(kreis, cx - 4);
        Canvas.SetTop(kreis,  cy - 4);
        HauptCanvas.Children.Add(kreis);
    }
}
```

---

## ListBox – Mehrfachauswahl in zweite ListBox übertragen

```cs
private void Hinzufuegen_Click(object sender, RoutedEventArgs e)
{
    var liste = (AuswahlListBox.ItemsSource as List<EintragDto>) ?? new List<EintragDto>();

    foreach (EintragDto item in ErgebnisListBox.SelectedItems)
        if (!liste.Any(x => x.Name == item.Name))
            liste.Add(item);

    AuswahlListBox.ItemsSource = null;
    AuswahlListBox.ItemsSource = liste;
    // ErgebnisListBox bleibt unverändert
}
```

> Die zweite ListBox (`AuswahlListBox`) wird beim nächsten Suchen **nicht** geleert –
> sie akkumuliert die Auswahl über mehrere Suchen hinweg.

---

## Graphen / Routenoptimierung – Nearest Neighbor (Greedy TSP)

Findet eine kurze (nicht unbedingt optimale) Rundroute durch alle Punkte.

```cs
List<EintragDto> NearestNeighbor(List<EintragDto> punkte)
{
    var offen = punkte.ToList();
    var route = new List<EintragDto> { offen[0] };
    offen.RemoveAt(0);

    while (offen.Count > 0)
    {
        var letzter   = route.Last();
        var naechster = offen.MinBy(p => Distanz(letzter, p))!;
        route.Add(naechster);
        offen.Remove(naechster);
    }
    return route;
}

double Distanz(EintragDto a, EintragDto b)
{
    double dx = a.X - b.X, dy = a.Y - b.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}
```

### Route als Linien zeichnen
```cs
void ZeigeRoute(List<EintragDto> route)
{
    foreach (var l in HauptCanvas.Children.OfType<Line>().ToList())
        HauptCanvas.Children.Remove(l);

    double w = HauptCanvas.ActualWidth, h = HauptCanvas.ActualHeight;

    for (int i = 0; i < route.Count - 1; i++)
    {
        var a = route[i]; var b = route[i + 1];
        HauptCanvas.Children.Add(new Line
        {
            X1 = (a.X + 180) / 360 * w,  Y1 = (90 - a.Y) / 180 * h,
            X2 = (b.X + 180) / 360 * w,  Y2 = (90 - b.Y) / 180 * h,
            Stroke = Brushes.DarkBlue, StrokeThickness = 2
        });
    }
}
```

---

## Kurzübersicht

| Was | Wie |
|---|---|
| SQLite laden | `UseSqlite(...)`, `ctx.Tabelle.ToList()` |
| Mehrere Clients | `Task.Run(() => ClientVerarbeiten(client))` |
| XML senden | `writer.WriteLine(XmlHelper.Serialize(obj))` |
| XML empfangen | `XmlHelper.Deserialize<T>(reader.ReadLine()!)` |
| LINQ filtern | `.Where(x => x.Name.ToLower().Contains(filter))` |
| Canvas Punkt | `new Ellipse()`, `Canvas.SetLeft/Top` |
| Mehrfachauswahl | `SelectionMode="Extended"`, `SelectedItems` |
| Nearest Neighbor | MinBy(Distanz) in Schleife bis alle besucht |
