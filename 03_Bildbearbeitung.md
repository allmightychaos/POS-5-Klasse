# WPF + XML + Bildbearbeitung – Referenz

---

## Projektstruktur

```
Bilderverwaltungsprogramm/
├── Album.cs                    ← Sammlung von Bildern (INotifyPropertyChanged)
├── Bild.cs                     ← einzelnes Bild mit Pfad + BitmapImage
├── ViewModel.cs                ← gesamte Logik + Commands
├── XmlService.cs               ← XML speichern/laden
├── AlbumNameDialog.xaml/.cs    ← Dialog: Albumname eingeben
├── AlbumWaehlenDialog.xaml/.cs ← Dialog: Album aus Liste wählen
├── MainWindow.xaml             ← UI
└── MainWindow.xaml.cs          ← DataContext + Closed-Event
```

---

## 1. Datenmodell

### Album.cs

```cs
public class Album : INotifyPropertyChanged
{
    public string AlbumName { get; set; }

    // ObservableCollection → UI aktualisiert sich automatisch wenn Bilder hinzukommen
    public ObservableCollection<Bild> Bilddatei { get; set; } = new ObservableCollection<Bild>();

    public Album(string albumname) { AlbumName = albumname; }
    public Album() { } // ← parameterlosen Konstruktor für XmlSerializer benötigt!

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

### Bild.cs

```cs
public class Bild : INotifyPropertyChanged
{
    // Wenn Bildpfad gesetzt wird → BitmapImage automatisch neu laden
    private string _bildpfad;
    public string Bildpfad
    {
        get => _bildpfad;
        set
        {
            _bildpfad = value;
            OnPropertyChanged();
            if (_bildpfad != null)
                Bildquelle = BildquelleErstellen(); // ← automatisch!
        }
    }

    // Berechnete Properties (aus Bildpfad abgeleitet, nicht in XML gespeichert)
    [XmlIgnore] public string Dateiname => Path.GetFileName(Bildpfad);
    [XmlIgnore] public string Bildname  => Path.GetFileNameWithoutExtension(Bildpfad);

    // BitmapImage für die UI (nicht in XML gespeichert)
    [XmlIgnore]
    private BitmapImage _bildquelle;
    [XmlIgnore]
    public BitmapImage Bildquelle
    {
        get => _bildquelle;
        set { _bildquelle = value; OnPropertyChanged(); }
    }

    public Bild(string bildpfad) { Bildpfad = bildpfad; }
    public Bild() { } // ← für XmlSerializer

    // BitmapImage laden (OnLoad = Datei sofort freigeben → File.Move/Delete möglich!)
    public BitmapImage BildquelleErstellen()
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(Bildpfad);
        bmp.CacheOption = BitmapCacheOption.OnLoad;           // Datei nach Laden freigeben
        bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Cache ignorieren (für Rotation)
        bmp.EndInit();
        return bmp;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

> **Warum `BitmapCacheOption.OnLoad`?**
> Standardmäßig hält WPF die Bilddatei geöffnet solange sie angezeigt wird.
> Das verhindert `File.Move` und `File.Delete`.
> Mit `OnLoad` wird das Bild sofort vollständig in den RAM geladen und die Datei freigegeben.

> **Warum `IgnoreImageCache`?**
> WPF cached Bilder intern. Nach einer Rotation würde sonst das alte Bild aus dem Cache angezeigt.
> `IgnoreImageCache` zwingt WPF die Datei neu zu lesen.

> **Warum `[XmlIgnore]`?**
> Der `XmlSerializer` versucht alle public Properties zu speichern.
> `BitmapImage` kann nicht serialisiert werden → `[XmlIgnore]` sagt "diese Property überspringen".
> `Bildname` und `Dateiname` werden aus `Bildpfad` berechnet → müssen nicht gespeichert werden.

---

## 2. XML speichern & laden (XmlService)

```cs
public class XmlService
{
    // Pfad zur XML-Datei (neben der .exe im BVP-Album Ordner)
    public static string XmlPfad = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "BVP-Album", "daten.xml");

    // ObservableCollection → XML-Datei
    public static void Speichern(ObservableCollection<Album> alben)
    {
        var serializer = new XmlSerializer(typeof(List<Album>));
        using var fs = new FileStream(XmlPfad, FileMode.Create);
        serializer.Serialize(fs, alben.ToList()); // ObservableCollection → List → XML
    }

    // XML-Datei → ObservableCollection
    public static ObservableCollection<Album> Laden()
    {
        var serializer = new XmlSerializer(typeof(List<Album>));
        using var fs = new FileStream(XmlPfad, FileMode.Open);
        var geladen = (List<Album>)serializer.Deserialize(fs);

        var result = new ObservableCollection<Album>();
        foreach (var a in geladen)
            result.Add(a);
        return result;
    }
}
```

### Wann speichern/laden?

```cs
// MainWindow.xaml → Closed="Window_Closed"
// Speichern beim Schließen des Fensters
private void Window_Closed(object sender, EventArgs e)
{
    try { XmlService.Speichern(_viewModel.AlbumAuswahl); }
    catch (Exception ex) { MessageBox.Show(ex.Message); }
}

// Laden im ViewModel-Konstruktor
if (File.Exists(XmlService.XmlPfad))
{
    AlbumAuswahl = XmlService.Laden();
    if (AlbumAuswahl.Any())
        AusgAlbum = AlbumAuswahl[0]; // erstes Album automatisch auswählen
}
```

> **WICHTIG: XmlSerializer braucht parameterlose Konstruktoren!**
> Sowohl `Album` als auch `Bild` brauchen `public Album() {}` und `public Bild() {}`.
> Sonst: `InvalidOperationException: cannot be serialized because it does not have a parameterless constructor`

---

## 3. ViewModel – Grundstruktur + Commands

```cs
public class ViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Album> AlbumAuswahl { get; set; }

    private Album _ausgAlbum;
    public Album AusgAlbum
    {
        get => _ausgAlbum;
        set { _ausgAlbum = value; OnPropertyChanged(); }
    }

    // Commands (im Konstruktor mit RelayCommand initialisieren)
    public ICommand AlbumErstellenCommand  { get; set; }
    public ICommand BildHinzufuegenCommand { get; set; }
    public ICommand BildLoeschenCommand    { get; set; }
    public ICommand BildVerschiebenCommand { get; set; }
    public ICommand RotateClock90Command   { get; set; }
    public ICommand RotateCounter90Command { get; set; }
    public ICommand Rotate180Command       { get; set; }

    public ViewModel()
    {
        AlbumAuswahl = new ObservableCollection<Album>();

        if (File.Exists(XmlService.XmlPfad))
        {
            AlbumAuswahl = XmlService.Laden();
            if (AlbumAuswahl.Any()) AusgAlbum = AlbumAuswahl[0];
        }

        AlbumErstellenCommand  = new RelayCommand(AlbumErstellen);
        BildHinzufuegenCommand = new RelayCommand(BildHinzufügen);
        BildLoeschenCommand    = new RelayCommand(BildLoeschen);
        BildVerschiebenCommand = new RelayCommand(BildVerschieben);
        RotateClock90Command   = new RelayCommand(RotateClock90);
        RotateCounter90Command = new RelayCommand(RotateCounter90);
        Rotate180Command       = new RelayCommand(Rotate180);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
```

---

## 4. Album erstellen (mit Ordner anlegen)

```cs
private void AlbumErstellen(object parameter)
{
    // Dialog für Albumname öffnen
    var dialog = new AlbumNameDialog();
    if (dialog.ShowDialog() != true) return;
    string albumname = dialog.AlbumName;

    // Prüfen ob Name bereits existiert (LINQ)
    bool exists = AlbumAuswahl.Any(a => a.AlbumName == albumname);
    if (exists) { MessageBox.Show("Album existiert bereits."); return; }

    // Ordner auf Dateisystem anlegen
    string pfad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BVP-Album", albumname);
    Directory.CreateDirectory(pfad); // erstellt den Ordner (auch Unterordner)

    // Album zur Collection hinzufügen
    var album = new Album(albumname);
    AlbumAuswahl.Add(album);

    // Wenn erstes Album → automatisch auswählen
    if (AlbumAuswahl.Count == 1) AusgAlbum = album;
}
```

---

## 5. Bilder hinzufügen (ZIP entpacken)

```cs
private void BildHinzufügen(object parameter)
{
    if (!AlbumAuswahl.Any()) { MessageBox.Show("Zuerst Album erstellen!"); return; }

    // Dateidialog für ZIP-Datei
    var oFD = new OpenFileDialog { Filter = "Zip files (*.zip)|*.zip" };
    if (oFD.ShowDialog() != true) return;

    string pfad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BVP-Album", AusgAlbum.AlbumName);

    // ZIP entpacken (true = überschreiben wenn vorhanden)
    ZipFile.ExtractToDirectory(oFD.FileName, pfad, true);

    // Alle Dateien im Ordner laden (nur neue hinzufügen)
    foreach (string file in Directory.GetFiles(pfad))
    {
        if (!AusgAlbum.Bilddatei.Any(b => b.Bildpfad == file))
            AusgAlbum.Bilddatei.Add(new Bild(file));
    }

    OnPropertyChanged(nameof(AusgAlbum)); // ListBox aktualisieren
}
```

---

## 6. SelectedItems als CommandParameter (Löschen/Verschieben/Rotieren)

Mehrere ausgewählte Bilder werden über `CommandParameter` übergeben:

### XAML

```xml
<MenuItem Command="{Binding BildLoeschenCommand}"
          CommandParameter="{Binding ElementName=BildListBox, Path=SelectedItems}"/>
```

### ViewModel empfängt IList → wirft auf List<Bild>

```cs
private void BildLoeschen(object parameter)
{
    // IList (SelectedItems) → List<Bild>
    List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();
    if (!selected.Any()) { MessageBox.Show("Wähle zuerst ein Bild aus."); return; }

    foreach (Bild bild in selected)
    {
        AusgAlbum.Bilddatei.Remove(bild); // aus Collection entfernen
        File.Delete(bild.Bildpfad);       // physisch löschen
    }
}
```

> **Warum `.Cast<Bild>().ToList()`?**
> `SelectedItems` ist `IList` (nicht typisiert). `.Cast<Bild>()` konvertiert jeden Eintrag zu `Bild`.

---

## 7. Bilder verschieben (mit Dialog)

```cs
private void BildVerschieben(object parameter)
{
    List<Bild>? selected = (parameter as IList)?.Cast<Bild>().ToList();
    if (!selected.Any()) { MessageBox.Show("Wähle zuerst ein Bild aus."); return; }

    // Dialog: Ziel-Album wählen
    var dialog = new AlbumWaehlenDialog(AlbumAuswahl);
    if (dialog.ShowDialog() != true) return;
    Album ziel = dialog.GewaehltesAlbum;

    foreach (Bild bild in selected)
    {
        AusgAlbum.Bilddatei.Remove(bild);  // aus altem Album entfernen
        ziel.Bilddatei.Add(bild);          // zu neuem Album hinzufügen

        // Datei physisch verschieben
        string destPfad = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "BVP-Album", ziel.AlbumName, bild.Dateiname);
        File.Move(bild.Bildpfad, destPfad, true); // true = überschreiben
    }
}
```

---

## 8. Bilder rotieren (System.Drawing)

> **NuGet benötigt:** `System.Drawing.Common`

```cs
private void RotateImage(List<Bild> selected, string direction)
{
    Bitmap rotated;

    foreach (var item in selected)
    {
        // Datei laden in Bitmap (außerhalb using damit Datei freigegeben wird vor Save)
        using (var temp = System.Drawing.Image.FromFile(item.Bildpfad))
        {
            rotated = new Bitmap(temp); // in Memory kopieren → Datei wird freigegeben

            RotateFlipType typ = direction switch
            {
                "clock90"   => RotateFlipType.Rotate90FlipNone,
                "counter90" => RotateFlipType.Rotate270FlipNone,
                "rotate180" => RotateFlipType.Rotate180FlipNone,
                _           => RotateFlipType.RotateNoneFlipNone
            };
            rotated.RotateFlip(typ);
        } // ← hier wird temp disposed → Datei freigegeben

        rotated.Save(item.Bildpfad);             // überschreiben
        item.Bildquelle = item.BildquelleErstellen(); // UI aktualisieren
    }
}
```

> **Warum `new Bitmap(temp)` vor dem Rotieren?**
> `Image.FromFile` hält die Datei offen bis das Objekt disposed wird.
> Mit `new Bitmap(temp)` wird das Bild in den RAM kopiert.
> Nach dem `using`-Block wird `temp` disposed → Datei freigegeben → `Save` funktioniert.

---

## 9. XAML – ListBox als Galerie (WrapPanel)

```xml
<ListBox x:Name="BildListBox"
         ItemsSource="{Binding AusgAlbum.Bilddatei}"
         SelectionMode="Multiple">

    <!-- WrapPanel: Bilder nebeneinander mit Zeilenumbruch -->
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>

    <!-- DataTemplate: wie sieht ein einzelnes Bild aus -->
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Vertical" Width="195" Height="110">
                <Image Source="{Binding Bildquelle}" Height="75"/>
                <TextBlock Text="{Binding Bildname}"
                           Height="35"
                           TextWrapping="Wrap"
                           Background="LightGray"
                           Padding="3,0"
                           VerticalAlignment="Center"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### ComboBox für Album-Auswahl

```xml
<ComboBox ItemsSource="{Binding AlbumAuswahl}"
          SelectedItem="{Binding AusgAlbum}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding AlbumName}" FontWeight="Bold"/>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

### Keyboard Shortcuts

```xml
<Window.InputBindings>
    <KeyBinding Key="N" Modifiers="Ctrl" Command="{Binding AlbumErstellenCommand}"/>
    <KeyBinding Key="Delete"
                Command="{Binding BildLoeschenCommand}"
                CommandParameter="{Binding ElementName=BildListBox, Path=SelectedItems}"/>
    <KeyBinding Key="R" Modifiers="Ctrl"
                Command="{Binding RotateClock90Command}"
                CommandParameter="{Binding ElementName=BildListBox, Path=SelectedItems}"/>
</Window.InputBindings>
```

---

## 10. Dialog-Pattern (Texteingabe + Auswahl)

### Texteingabe-Dialog

```cs
// Dialog.xaml.cs
public partial class AlbumNameDialog : Window
{
    public string AlbumName { get; private set; }

    public AlbumNameDialog() => InitializeComponent();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        AlbumName = AlbumNameTextBox.Text;
        DialogResult = true; // schließt Fenster + gibt true zurück
    }
}

// Aufrufen
var dialog = new AlbumNameDialog();
if (dialog.ShowDialog() == true)
    string name = dialog.AlbumName;
```

### Auswahl-Dialog (ComboBox)

```cs
// Dialog.xaml.cs
public partial class AlbumWaehlenDialog : Window
{
    public Album GewaehltesAlbum { get; private set; }

    public AlbumWaehlenDialog(ObservableCollection<Album> alben)
    {
        InitializeComponent();
        DataContext = alben; // Liste direkt als DataContext → Binding mit {Binding}
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        GewaehltesAlbum = AlbumComboBox.SelectedItem as Album;
        DialogResult = true;
    }
}
```

```xml
<!-- Dialog XAML: ItemsSource="{Binding}" weil DataContext direkt die Liste ist -->
<ComboBox x:Name="AlbumComboBox"
          ItemsSource="{Binding}"
          DisplayMemberPath="AlbumName"/>
```

---

## 11. Schnellreferenz

### Dateisystem

```cs
Directory.CreateDirectory(pfad)              // Ordner erstellen (auch verschachtelt)
File.Delete(pfad)                            // Datei löschen
File.Move(von, nach, true)                   // Datei verschieben (true = überschreiben)
Directory.GetFiles(pfad)                     // alle Dateien als string[] (voller Pfad!)
ZipFile.ExtractToDirectory(zip, ziel, true)  // ZIP entpacken
Path.Combine(a, b, c)                        // Pfade zusammensetzen
Path.GetFileName(pfad)                       // "image.png"
Path.GetFileNameWithoutExtension(pfad)       // "image"
AppDomain.CurrentDomain.BaseDirectory        // Ordner der .exe
File.Exists(pfad)                            // Datei vorhanden?
```

### LINQ

```cs
AlbumAuswahl.Any()                           // leer?
AlbumAuswahl.Any(a => a.AlbumName == name)  // existiert?
selected.Cast<Bild>().ToList()               // IList → List<Bild>
```

### INotifyPropertyChanged (immer gleich kopieren)

```cs
public event PropertyChangedEventHandler? PropertyChanged;
protected void OnPropertyChanged([CallerMemberName] string? n = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
```

### XmlSerializer Voraussetzungen

- Klasse muss `public` sein
- Alle Properties müssen `public get` und `public set` haben
- Parameterlosen Konstruktor `public MeineKlasse() {}` haben
- Nicht serialisierbare Properties mit `[XmlIgnore]` markieren
