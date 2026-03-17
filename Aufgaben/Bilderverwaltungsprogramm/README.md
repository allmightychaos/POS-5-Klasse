# Bilderverwaltungsprogramm

## Aufgabe

Das Programm soll folgende Menüpunkte haben (jeweils inkl. Mnemonics, [Shortcuts](https://wpf-tutorial.com/commands/implementing-custom-commands/), Icons)

* Datei
    * Neues Album
    * Bilder hinzufügen
    * Bilder in Album verschieben
    * Bilder löschen
* Bearbeiten
    * 90° im Uhrzeigersinn rotieren
    * 90° gegen den Uhrzeigersinn rotieren
    * 180° rotieren

Die Bilder sollen im Programm in einem Images-Ordner gespeichert werden. Die Alben sollen [Unterordner](https://learn.microsoft.com/de-de/dotnet/api/system.io.directory?view=net-8.0) in diesem Ordner sein.

Die Bildformate [JPG](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-encode-and-decode-a-jpeg-image?view=netframeworkdesktop-4.8&viewFallbackFrom=netdesktop-7.0) und [PNG](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-encode-and-decode-a-png-image?view=netframeworkdesktop-4.8) sollen unterstützt werden.

Der Menüpunkt "Neues Album" fragt über einen Dialog den Namen des neuen Albums ab und prüft ob nicht bereits ein Album mit diesem Namen vorhanden ist. Wird der Dialog erfolgreich beendet, wird das neue Album angelegt (inkl Unterordner)

Der Menüpunkt "Bilder hinzufügen" soll über einen Dateiauswahldialog eine Zip-Datei wählen können. Die Bilder in dieser Zip-Datei sollen dann in den Unterordner des aktuellen Albums [entpackt](https://learn.microsoft.com/de-de/dotnet/standard/io/how-to-compress-and-extract-files) werden (vorhandene Bilder mit gleichen Namen können überschrieben werden).

Neben dem Menü soll das Programm noch über eine ComboBox, in der das aktuelle Album gewählt werden kann, und einer ListBox, in der die Bilder des aktuellen Albums angezeigt werden, verfügen.

In der ListBox sollen die Bilder mit einem DataTemplate wie in einer Bildergallerie angezeigt werden. Als Name der Bilder soll der Dateiname ohne Endung angezeigt werden. Es sollen mehrere Bilder ausgewählt werden können.

Wurden Bilder gewählt sollen diese bearbeitet werden bzw. [verschoben](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-8.0) oder [gelöscht](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.delete?view=net-8.0) werden können. Die Bilder werden beim Bearbeiten überschrieben.

Alle Daten des Programms sollen beim Beenden des Programms in einer XML-Datei gespeichert werden und beim nächsten Start des Programms wieder geladen werden.
