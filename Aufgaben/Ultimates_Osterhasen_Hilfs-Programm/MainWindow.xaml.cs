using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OsterhasenProgramm.Data;
using OsterhasenProgramm.Helpers;
using OsterhasenProgramm.Models;


namespace Ultimates_Osterhasen_Hilfs_Programm
{
    public partial class MainWindow : Window
    {
        // ===== Kartengrenzen Wiener Neustadt =====
        private const double LON_LEFT = 16.209652;
        private const double LON_RIGHT = 16.281017;
        private const double LAT_BOTTOM = 47.786898;
        private const double LAT_TOP = 47.846533;

        // ===== Farben für Helfer =====
        private static readonly Brush[] HelperColors =
        {
            Brushes.Red,        Brushes.Blue,       Brushes.LimeGreen,
            Brushes.Orange,     Brushes.Purple,     Brushes.Cyan,
            Brushes.Magenta,    Brushes.Yellow,     Brushes.SaddleBrown,
            Brushes.DeepPink
        };

        // ===== Zustand =====
        private List<Person> _persons = new();
        private int[]? _assignments = null;           // Cluster-Zuweisung pro Person
        private int _helperCount = 3;

        // Routen-Planung
        private bool _routePlanningMode = false;
        private int _nextHelperIndex = 0;
        private List<(double Lon, double Lat)> _startPoints = new();
        private List<List<Person>>? _routes = null;

        public MainWindow()
        {
            InitializeComponent();
            InitDatabase();
            LoadPersons();
        }

        // ===== Datenbank =====

        private void InitDatabase()
        {
            using var ctx = new AppDbContext();
            ctx.Database.EnsureCreated();
        }

        private void LoadPersons()
        {
            using var ctx = new AppDbContext();
            _persons = ctx.Persons.ToList();
            RefreshPersonList();
            DrawMap();
        }

        private void RefreshPersonList()
        {
            PersonListBox.ItemsSource = null;
            PersonListBox.ItemsSource = _persons;
        }

        // ===== Aufgabe 1: Person hinzufügen =====

        private void AddPerson_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string lonStr = LonBox.Text.Trim();
            string latStr = LatBox.Text.Trim();

            // Validierung
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Bitte einen Namen eingeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(lonStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                MessageBox.Show("Ungültige Longitude. Bitte einen Dezimalwert eingeben (z.B. 16.245000).",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(latStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lat))
            {
                MessageBox.Show("Ungültige Latitude. Bitte einen Dezimalwert eingeben (z.B. 47.815000).",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prüfen ob innerhalb Wiener Neustadt
            if (lon < LON_LEFT || lon > LON_RIGHT || lat < LAT_BOTTOM || lat > LAT_TOP)
            {
                MessageBox.Show(
                    $"Koordinaten liegen außerhalb von Wiener Neustadt!\n" +
                    $"Longitude: {LON_LEFT} – {LON_RIGHT}\n" +
                    $"Latitude:  {LAT_BOTTOM} – {LAT_TOP}",
                    "Außerhalb des Bereichs", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // In Datenbank speichern
            var person = new Person { Name = name, Longitude = lon, Latitude = lat };

            using (var ctx = new AppDbContext())
            {
                ctx.Persons.Add(person);
                ctx.SaveChanges();
            }

            _persons.Add(person);

            // Clustering zurücksetzen (neue Person noch nicht zugewiesen)
            _assignments = null;
            _routes = null;

            RefreshPersonList();
            DrawMap();

            // Felder leeren
            NameBox.Clear();
            LonBox.Clear();
            LatBox.Clear();
            NameBox.Focus();

            SetStatus($"✅ Person '{name}' wurde registriert. Gesamt: {_persons.Count} Personen.");
        }

        // ===== Aufgabe 3: Clustering =====

        private void Cluster_Click(object sender, RoutedEventArgs e)
        {
            if (_persons.Count == 0)
            {
                MessageBox.Show("Keine Personen registriert.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(HelperCountBox.Text.Trim(), out int k) || k < 1 || k > 10)
            {
                MessageBox.Show("Bitte eine gültige Helferanzahl eingeben (1–10).",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _helperCount = Math.Min(k, _persons.Count);
            _assignments = KMeansHelper.Cluster(_persons, _helperCount);
            _routes = null;
            _startPoints.Clear();

            DrawMap();
            UpdateLegend();
            SetStatus($"🎨 K-Means abgeschlossen: {_persons.Count} Personen auf {_helperCount} Helfer aufgeteilt.");
        }

        // ===== Aufgabe 4: Routenplanung =====

        private void PlanRoutes_Click(object sender, RoutedEventArgs e)
        {
            if (_assignments == null)
            {
                MessageBox.Show("Bitte zuerst die Personen aufteilen (Clustering).",
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_persons.Count == 0) return;

            // Routenplanung starten
            _routePlanningMode = true;
            _nextHelperIndex = 0;
            _startPoints.Clear();
            _routes = null;

            PlanRoutesButton.Background = Brushes.DarkRed;
            PlanRoutesButton.Content = "⏳ Startpunkte setzen...";
            MapCanvas.Cursor = Cursors.Cross;

            SetStatus($"🗺️ Klicken Sie auf die Karte für den Startpunkt von Helfer 1 von {_helperCount}.");
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_routePlanningMode) return;

            var pos = e.GetPosition(MapCanvas);
            double lon = XToLon(pos.X);
            double lat = YToLat(pos.Y);

            _startPoints.Add((lon, lat));

            // Startpunkt-Markierung zeichnen
            DrawStartMarker(pos.X, pos.Y, _nextHelperIndex);

            _nextHelperIndex++;

            if (_nextHelperIndex < _helperCount)
            {
                SetStatus($"🗺️ Startpunkt für Helfer {_nextHelperIndex} gesetzt. Jetzt Helfer {_nextHelperIndex + 1} von {_helperCount}.");
            }
            else
            {
                // Alle Startpunkte gesetzt → Routen berechnen
                _routePlanningMode = false;
                PlanRoutesButton.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                PlanRoutesButton.Content = "🗺️ Routen planen";
                MapCanvas.Cursor = Cursors.Cross;

                CalculateRoutes();
            }
        }

        private void CalculateRoutes()
        {
            _routes = new List<List<Person>>();

            for (int h = 0; h < _helperCount; h++)
            {
                // Alle Personen die diesem Helfer zugewiesen sind
                var helperPersons = _persons
                    .Where((_, i) => _assignments![i] == h)
                    .ToList();

                var sp = _startPoints[h];
                var route = RouteHelper.NearestNeighbor(helperPersons, sp.Lon, sp.Lat);
                _routes.Add(route);
            }

            DrawMap();
            SetStatus($"✅ Routen für {_helperCount} Helfer berechnet und visualisiert.");
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _assignments = null;
            _routes = null;
            _startPoints.Clear();
            _routePlanningMode = false;
            PlanRoutesButton.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            PlanRoutesButton.Content = "🗺️ Routen planen";
            LegendPanel.Text = "";
            DrawMap();
            SetStatus("🔄 Zurückgesetzt. Clustering und Routen wurden entfernt.");
        }

        // ===== Kartenzeichnung =====

        private void DrawMap()
        {
            MapCanvas.Children.Clear();

            if (_routes != null)
            {
                // Linien zeichnen (unter den Punkten)
                DrawRouteLines();
            }

            // Personen als Kreise zeichnen
            for (int i = 0; i < _persons.Count; i++)
            {
                var p = _persons[i];
                double x = LonToX(p.Longitude);
                double y = LatToY(p.Latitude);

                Brush fill = _assignments != null
                    ? HelperColors[_assignments[i] % HelperColors.Length]
                    : new SolidColorBrush(Color.FromRgb(34, 139, 34)); // Dunkelgrün

                var ellipse = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = fill,
                    Stroke = Brushes.White,
                    StrokeThickness = 1.5,
                    ToolTip = $"{p.Name}\nLon: {p.Longitude:F6}\nLat: {p.Latitude:F6}" +
                              (_assignments != null ? $"\nHelfer {_assignments[i] + 1}" : "")
                };

                Canvas.SetLeft(ellipse, x - 7);
                Canvas.SetTop(ellipse, y - 7);
                MapCanvas.Children.Add(ellipse);
            }

            // Startpunkte zeichnen
            for (int h = 0; h < _startPoints.Count; h++)
            {
                DrawStartMarker(
                    LonToX(_startPoints[h].Lon),
                    LatToY(_startPoints[h].Lat),
                    h);
            }
        }

        private void DrawRouteLines()
        {
            if (_routes == null) return;

            for (int h = 0; h < _routes.Count; h++)
            {
                var route = _routes[h];
                if (route.Count == 0) continue;

                var color = HelperColors[h % HelperColors.Length];
                var sp = _startPoints[h];

                double prevX = LonToX(sp.Lon);
                double prevY = LatToY(sp.Lat);

                foreach (var person in route)
                {
                    double x = LonToX(person.Longitude);
                    double y = LatToY(person.Latitude);

                    var line = new Line
                    {
                        X1 = prevX,
                        Y1 = prevY,
                        X2 = x,
                        Y2 = y,
                        Stroke = color,
                        StrokeThickness = 2,
                        Opacity = 0.75
                    };
                    MapCanvas.Children.Add(line);

                    prevX = x;
                    prevY = y;
                }
            }
        }

        private void DrawStartMarker(double x, double y, int helperIndex)
        {
            var color = HelperColors[helperIndex % HelperColors.Length];

            // Stern-ähnliche Markierung: Kreuz + Kreis
            var outer = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = color,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                ToolTip = $"Helfer {helperIndex + 1} – Startpunkt"
            };
            Canvas.SetLeft(outer, x - 10);
            Canvas.SetTop(outer, y - 10);
            MapCanvas.Children.Add(outer);

            // Nummer
            var label = new TextBlock
            {
                Text = (helperIndex + 1).ToString(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Width = 20
            };
            Canvas.SetLeft(label, x - 10);
            Canvas.SetTop(label, y - 8);
            MapCanvas.Children.Add(label);
        }

        private void MapContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawMap();
        }

        // ===== Koordinatenumrechnung =====

        private double LonToX(double lon)
            => (lon - LON_LEFT) / (LON_RIGHT - LON_LEFT) * MapCanvas.ActualWidth;

        private double LatToY(double lat)
            => (1.0 - (lat - LAT_BOTTOM) / (LAT_TOP - LAT_BOTTOM)) * MapCanvas.ActualHeight;

        private double XToLon(double x)
            => LON_LEFT + x / MapCanvas.ActualWidth * (LON_RIGHT - LON_LEFT);

        private double YToLat(double y)
            => LAT_TOP - y / MapCanvas.ActualHeight * (LAT_TOP - LAT_BOTTOM);

        // ===== Hilfsmethoden =====

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }

        private void UpdateLegend()
        {
            if (_assignments == null)
            {
                LegendPanel.Text = "";
                return;
            }

            var parts = new List<string>();
            for (int h = 0; h < _helperCount; h++)
            {
                int count = _assignments.Count(a => a == h);
                parts.Add($"Helfer {h + 1}: {count}");
            }
            LegendPanel.Text = string.Join("  |  ", parts);
        }
    }
}
