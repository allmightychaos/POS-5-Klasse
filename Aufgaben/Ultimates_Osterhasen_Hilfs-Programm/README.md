# Ultimatives Osterhasen Hilfs-Programm

### Aufgabe 1

Um den Osterhasen bei seiner Arbeit zu unterstützen soll ein Programm erstellt werden mit dem die Personen registriert werden können die noch an den Osterhasen glauben und deswegen mit Ostereiern beschenkt werden sollen.

Das Programm beschänkt sich auf den Raum Wiener Neustadt und die Personen haben mindestens Namen, Longitude und Latitude als Daten.

Die Daten der Personen werden in einer SQLite Datenbank gespeichert. Auf die Datenbank wird mit ORM und LINQ zugegriffen.

Erstellen Sie ein WPF Programm mit passenden Eingabefeldern für die Personendaten. Personendaten müssen nicht wieder gelöscht werden können.

### Aufgabe 2

Es sollen alle eingegebenen Wünsche auf einer Wiener Neustadt Karte visualisiert werden. Die Visualisierung soll mit geometrischen Formen erfolgen.

![Stadtplan Wiener Neustadt](https://www.eduvidual.at/pluginfile.php/8567458/mod_page/content/2/Stadtplan-Wiener-Neustadt.jpg)

Der Kartenausschnitt zeigt folgende Longitude- und Latitude-Werte:

* Links 16.209652
* Unten 47.786898
* Rechts 16.281017
* Oben 47.846533

### Aufgabe 3

Die Daten sollen nicht nur visualisiert werden, sonder der Osterhase soll auch die Möglichkeit bekommen mit den Daten die Auslieferung der Ostereier zu planen.

Da der Osterhase schon etwas älter ist, wird er nicht alle selbst ausliefern sondern mehrere Helfer anwerben. Damit die Helfer nicht unnötige Wegstrecken zurücklegen sollen die auszuliefernden Ostereier so auf die Helfer aufgeteilt werden damit jeder Helfer eine ähnliche Anzahl von Ostereiern ausliefert und alle auszuliefernden Ostereier nahe beisammen liegen.

Bei der Visualisierung sollen die Ostereier die jedem Helfer zugewiesen wurde in einer extra Farbe dargestellt werden. Die Anzahl der Helfer soll frei wählbar sein.

Beispiel mit 5 Helfern:

![Aufteilung Ostereier](https://www.eduvidual.at/pluginfile.php/8567458/mod_page/content/2/AufteilungOstereier.png)

Wählen Sie einen passenden Algorithmus für diese Aufteilung.

### Aufgabe 4

Als letzte Hilfsfunktion soll nun auch noch der Weg der Helfer geplant werden können.

Dazu soll für jeden Helfer in die Karte geklickt werden. Das setzt den Ausgangspunkt für diesen Helfer. Es soll nun ausgehend von diesem Punkt der kürzeste Weg errechnet werden mit dem der Helfer alle seine Ostereier ausliefern kann. Der Weg soll durch farblich passende Linien auf der Karte visualisiert werden.