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
