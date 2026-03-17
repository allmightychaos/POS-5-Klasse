# WPF + SQLite + Karte + Algorithmen – Referenz

---

## Projektstruktur

```
MeinProjekt/
├── Models/
│   └── Person.cs          ← Datenmodell (Name, Lon, Lat)
├── Data/
│   └── AppDbContext.cs    ← EF Core / SQLite
├── Helpers/
│   ├── KMeansHelper.cs    ← Clustering-Algorithmus
│   └── RouteHelper.cs     ← Routen-Algorithmus
├── MainWindow.xaml        ← UI
└── MainWindow.xaml.cs     ← Logik
```

---

## 1. Datenmodell + SQLite

### Person.cs

```cs
namespace MeinProjekt.Models
{
    public class Person
    {
        public int Id { get; set; }        // Primary Key (auto von EF Core)
        public string Name { get; set; } = "";
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
```

### AppDbContext.cs

```cs
using Microsoft.EntityFrameworkCore;
using MeinProjekt.Models;

namespace MeinProjekt.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // DB-Datei liegt neben der .exe
            string pfad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "daten.db");
            options.UseSqlite($"Data Source={pfad}");
        }
    }
}
```

### NuGet Packages (einmalig installieren)

```
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Design
```

### DB initialisieren + Personen laden (im Konstruktor)

```cs
public MainWindow()
{
    InitializeComponent();

    // DB erstellen wenn nicht vorhanden
    using var ctx = new AppDbContext();
    ctx.Database.EnsureCreated();

    // Alle Personen laden
    _persons = ctx.Persons.ToList();
}
```

### Person speichern

```cs
var person = new Person { Name = name, Longitude = lon, Latitude = lat };

using var ctx = new AppDbContext();
ctx.Persons.Add(person);
ctx.SaveChanges();

_persons.Add(person); // auch lokale Liste aktualisieren!
```

### Koordinaten validieren

```cs
// Grenzen des Kartenausschnitts
private const double LON_LEFT   = 16.209652;
private const double LON_RIGHT  = 16.281017;
private const double LAT_BOTTOM = 47.786898;
private const double LAT_TOP    = 47.846533;

// Prüfen ob Punkt innerhalb liegt
if (lon < LON_LEFT || lon > LON_RIGHT || lat < LAT_BOTTOM || lat > LAT_TOP)
{
    MessageBox.Show("Koordinaten liegen außerhalb des Bereichs!");
    return;
}
```

### double.TryParse für Koordinateneingabe

```cs
// InvariantCulture = Punkt als Dezimaltrennzeichen (16.245 statt 16,245)
if (!double.TryParse(eingabe, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
{
    MessageBox.Show("Ungültige Zahl!");
    return;
}
```

---

## 2. Karte – Canvas + Koordinatenumrechnung

### XAML: Karte aufbauen

```xml
<Grid x:Name="MapContainer" SizeChanged="MapContainer_SizeChanged">
    <!-- Stadtplan als Hintergrund -->
    <Image Source="stadtplan.jpg" Stretch="Fill"/>

    <!-- Canvas für Punkte und Linien darüber -->
    <Canvas x:Name="MapCanvas"
            Background="Transparent"
            MouseLeftButtonDown="MapCanvas_MouseLeftButtonDown"/>
</Grid>
```

### Koordinaten ↔ Pixel umrechnen

```cs
// Longitude → X-Pixel auf dem Canvas
// Wie weit ist der Punkt zwischen links und rechts? (0.0 bis 1.0) × Breite
private double LonToX(double lon)
    => (lon - LON_LEFT) / (LON_RIGHT - LON_LEFT) * MapCanvas.ActualWidth;

// Latitude → Y-Pixel
// ACHTUNG: Lat 0 = unten, Canvas 0 = oben → daher (1.0 - ...)
private double LatToY(double lat)
    => (1.0 - (lat - LAT_BOTTOM) / (LAT_TOP - LAT_BOTTOM)) * MapCanvas.ActualHeight;

// Rückrichtung (für Mausklick → Koordinaten)
private double XToLon(double x)
    => LON_LEFT + x / MapCanvas.ActualWidth * (LON_RIGHT - LON_LEFT);

private double YToLat(double y)
    => LAT_TOP - y / MapCanvas.ActualHeight * (LAT_TOP - LAT_BOTTOM);
```

> **Warum `1.0 - ...` bei Latitude?**
> Auf der Karte ist Norden oben (hohe Latitude), aber im Canvas ist Y=0 oben.
> D.h. hohe Latitude = kleines Y. Daher muss man "umdrehen".

### Punkt auf Canvas zeichnen

```cs
// Ellipse (Kreis) erstellen
var ellipse = new Ellipse
{
    Width = 14,
    Height = 14,
    Fill = Brushes.Green,
    Stroke = Brushes.White,
    StrokeThickness = 1.5,
    ToolTip = "Max Mustermann" // Tooltip beim Hovern
};

// Position setzen (Mittelpunkt = x-7, y-7 weil Width/Height = 14)
Canvas.SetLeft(ellipse, LonToX(person.Longitude) - 7);
Canvas.SetTop(ellipse,  LatToY(person.Latitude)  - 7);

MapCanvas.Children.Add(ellipse);
```

### Linie auf Canvas zeichnen

```cs
var line = new Line
{
    X1 = startX, Y1 = startY, // von
    X2 = endX,   Y2 = endY,   // nach
    Stroke = Brushes.Red,
    StrokeThickness = 2,
    Opacity = 0.75
};
MapCanvas.Children.Add(line);
```

### Canvas leeren und neu zeichnen

```cs
// Immer zuerst leeren, dann neu zeichnen
MapCanvas.Children.Clear();

// Canvas neu zeichnen wenn Fenstergröße sich ändert
private void MapContainer_SizeChanged(object sender, SizeChangedEventArgs e)
{
    DrawMap(); // eigene Methode die alles neu zeichnet
}
```

### Mausklick → Koordinaten

```cs
private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    var pos = e.GetPosition(MapCanvas); // Pixel-Position des Klicks
    double lon = XToLon(pos.X);        // → Longitude
    double lat = YToLat(pos.Y);        // → Latitude

    // ... weiterverarbeiten
}
```

---

## 3. Farben für mehrere Gruppen

```cs
// Array mit Farben – Index = Helfer-Nummer
private static readonly Brush[] Farben =
{
    Brushes.Red,      Brushes.Blue,    Brushes.LimeGreen,
    Brushes.Orange,   Brushes.Purple,  Brushes.Cyan,
    Brushes.Magenta,  Brushes.Yellow,  Brushes.SaddleBrown,
    Brushes.DeepPink
};

// Farbe für Helfer h holen (% verhindert Index-Fehler wenn h > 10)
Brush farbe = Farben[h % Farben.Length];
```

---

## 4. K-Means Clustering – Gruppen bilden

**Was macht das?** Teilt Punkte in k Gruppen auf, sodass jede Gruppe räumlich zusammenhängt.

**Wie funktioniert es (vereinfacht):**

1. Wähle k zufällige Startpunkte als "Zentren" (Centroids)
2. Weise jeden Punkt dem nächsten Zentrum zu
3. Berechne neue Zentren (Durchschnitt der Gruppe)
4. Wiederhole bis sich nichts mehr ändert

```cs
using MeinProjekt.Models;

namespace MeinProjekt.Helpers
{
    public static class KMeansHelper
    {
        // Euklidische Distanz zwischen zwei Koordinaten
        private static double Distance(double lon1, double lat1, double lon2, double lat2)
        {
            double dLon = lon1 - lon2;
            double dLat = lat1 - lat2;
            return Math.Sqrt(dLon * dLon + dLat * dLat);
        }

        // Gibt für jede Person den Cluster-Index zurück (0 bis k-1)
        // Beispiel: [0, 1, 0, 2, 1] → Person 0 & 2 = Gruppe 0, Person 1 & 4 = Gruppe 1, ...
        public static int[] Cluster(List<Person> persons, int k)
        {
            if (persons.Count == 0) return Array.Empty<int>();
            k = Math.Min(k, persons.Count); // k kann nicht größer als Personenanzahl sein

            var random = new Random(42); // 42 = fixer Seed → immer gleiche Zufallsfolge

            // Schritt 1: k zufällige Personen als Start-Centroids wählen
            var centroidIndices = Enumerable.Range(0, persons.Count)
                .OrderBy(_ => random.Next())
                .Take(k)
                .ToList();

            // Centroids als (Lon, Lat) Tupel speichern
            var centroids = centroidIndices
                .Select(i => (Lon: persons[i].Longitude, Lat: persons[i].Latitude))
                .ToList();

            int[] assignments = new int[persons.Count]; // Zuweisung: Person i → Cluster X
            bool changed = true;
            int maxIterations = 100; // Sicherheitsstop

            while (changed && maxIterations-- > 0)
            {
                changed = false;

                // Schritt 2: Jede Person dem nächsten Centroid zuweisen
                for (int i = 0; i < persons.Count; i++)
                {
                    int nearest = 0;
                    double minDist = double.MaxValue;

                    for (int j = 0; j < k; j++)
                    {
                        double dist = Distance(
                            persons[i].Longitude, persons[i].Latitude,
                            centroids[j].Lon, centroids[j].Lat);

                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = j; // dieser Centroid ist am nächsten
                        }
                    }

                    if (assignments[i] != nearest)
                    {
                        assignments[i] = nearest; // Zuweisung ändern
                        changed = true;           // → nochmal wiederholen
                    }
                }

                // Schritt 3: Neue Centroids berechnen (Mittelpunkt jeder Gruppe)
                for (int j = 0; j < k; j++)
                {
                    var gruppe = persons
                        .Where((_, i) => assignments[i] == j) // alle Personen in Gruppe j
                        .ToList();

                    if (gruppe.Count > 0)
                    {
                        // Durchschnitt der Koordinaten = neues Zentrum
                        centroids[j] = (
                            Lon: gruppe.Average(p => p.Longitude),
                            Lat: gruppe.Average(p => p.Latitude)
                        );
                    }
                }
            }

            return assignments; // z.B. [0, 2, 1, 0, 2, 1]
        }
    }
}
```

### K-Means aufrufen

```cs
// assignments[i] = Helfer-Index für Person i
int[] assignments = KMeansHelper.Cluster(_persons, anzahlHelfer);

// Farbe für Person i bestimmen
Brush farbe = Farben[assignments[i] % Farben.Length];
```

---

## 5. Nearest-Neighbor Routing – kürzesten Weg finden

**Was macht das?** Findet einen effizienten Weg durch alle Punkte einer Gruppe, ausgehend von einem Startpunkt.

**Wie funktioniert es:**

1. Starte beim Startpunkt
2. Finde die nächste noch nicht besuchte Person
3. Gehe dorthin, markiere als besucht
4. Wiederhole bis alle besucht

```cs
using MeinProjekt.Models;

namespace MeinProjekt.Helpers
{
    public static class RouteHelper
    {
        private static double Distance(double lon1, double lat1, double lon2, double lat2)
        {
            double dLon = lon1 - lon2;
            double dLat = lat1 - lat2;
            return Math.Sqrt(dLon * dLon + dLat * dLat);
        }

        // Gibt die Personen in der optimalen Reihenfolge zurück
        public static List<Person> NearestNeighbor(
            List<Person> persons,  // alle Personen dieser Gruppe
            double startLon,       // Startpunkt des Helfers
            double startLat)
        {
            var route = new List<Person>();        // fertige Route
            var remaining = persons.ToList();      // noch nicht besucht

            double curLon = startLon;
            double curLat = startLat;

            while (remaining.Count > 0)
            {
                // Nächste Person vom aktuellen Standort aus finden
                var nearest = remaining
                    .OrderBy(p => Distance(curLon, curLat, p.Longitude, p.Latitude))
                    .First();

                route.Add(nearest);          // zur Route hinzufügen
                remaining.Remove(nearest);   // als besucht markieren

                // neuer Standort = gerade besuchte Person
                curLon = nearest.Longitude;
                curLat = nearest.Latitude;
            }

            return route;
        }
    }
}
```

### Route berechnen und zeichnen

```cs
// Für jeden Helfer Route berechnen
for (int h = 0; h < anzahlHelfer; h++)
{
    // Personen die diesem Helfer zugewiesen sind (LINQ)
    var helperPersons = _persons
        .Where((_, i) => assignments[i] == h)
        .ToList();

    var startpunkt = _startPoints[h]; // (Lon, Lat) vom Mausklick

    // Route berechnen
    var route = RouteHelper.NearestNeighbor(helperPersons, startpunkt.Lon, startpunkt.Lat);

    // Route als Linie zeichnen
    double prevX = LonToX(startpunkt.Lon);
    double prevY = LatToY(startpunkt.Lat);

    foreach (var person in route)
    {
        double x = LonToX(person.Longitude);
        double y = LatToY(person.Latitude);

        var line = new Line
        {
            X1 = prevX, Y1 = prevY,
            X2 = x,     Y2 = y,
            Stroke = Farben[h % Farben.Length],
            StrokeThickness = 2,
            Opacity = 0.75
        };
        MapCanvas.Children.Add(line);

        prevX = x;
        prevY = y;
    }
}
```

---

## 6. Startpunkte per Mausklick setzen

```cs
private bool _routePlanningMode = false;
private int _nextHelperIndex = 0;
private List<(double Lon, double Lat)> _startPoints = new();

// Button "Routen planen" → Modus aktivieren
private void PlanRoutes_Click(object sender, RoutedEventArgs e)
{
    _routePlanningMode = true;
    _nextHelperIndex = 0;
    _startPoints.Clear();
    MapCanvas.Cursor = Cursors.Cross; // Fadenkreuz-Cursor
}

// Mausklick auf Karte
private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (!_routePlanningMode) return;

    var pos = e.GetPosition(MapCanvas);
    _startPoints.Add((XToLon(pos.X), YToLat(pos.Y)));

    _nextHelperIndex++;

    if (_nextHelperIndex >= anzahlHelfer)
    {
        // Alle Startpunkte gesetzt → Routen berechnen
        _routePlanningMode = false;
        MapCanvas.Cursor = Cursors.Arrow;
        CalculateRoutes(); // eigene Methode
    }
}
```

---

## 7. Schnellreferenz

### ListBox mit Daten befüllen

```cs
// Option A: direkt
MeineListBox.ItemsSource = _persons;

// Option B: neu setzen (zwingt UI-Update)
MeineListBox.ItemsSource = null;
MeineListBox.ItemsSource = _persons;
```

### LINQ Häufige Patterns

```cs
// Alle Personen in Gruppe h
_persons.Where((_, i) => assignments[i] == h).ToList()

// Durchschnitt berechnen
gruppe.Average(p => p.Longitude)

// Nächstes Element nach Distanz
remaining.OrderBy(p => Distance(..., p.Lon, p.Lat)).First()

// Existiert?
ctx.Persons.Any(p => p.Name == "Max")

// Alle laden
ctx.Persons.ToList()
```

### ORM CRUD

```cs
// Hinzufügen
ctx.Persons.Add(neuePerson); ctx.SaveChanges();

// Laden
var alle = ctx.Persons.ToList();

// Löschen
ctx.Persons.Remove(person); ctx.SaveChanges();
```

### Nützliche WPF Canvas-Methoden

```cs
MapCanvas.Children.Clear();          // alles löschen
MapCanvas.Children.Add(element);     // Element hinzufügen
Canvas.SetLeft(element, x);          // X-Position setzen
Canvas.SetTop(element, y);           // Y-Position setzen
MapCanvas.ActualWidth                // aktuelle Breite in Pixel
MapCanvas.ActualHeight               // aktuelle Höhe in Pixel
```
