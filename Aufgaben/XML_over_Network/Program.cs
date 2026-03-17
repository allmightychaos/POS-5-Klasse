using System.Xml.Serialization;
using Xml_over_Network;

// ===== XML übers Netzwerk senden =====
// Zum Testen: Programm zweimal starten
//   → 1x "h" eingeben (Host)
//   → 1x "c" eingeben (Client)

Console.Write("Host oder Client? (h/c): ");
string? wahl = Console.ReadLine()?.Trim().ToLower();



// ===== TODO: eigene Logik nachdem Paket angekommen ist =====
XmlNetzwerkManager? manager = null;

void OnPaketEmpfangen(Paket paket)
{
    Console.WriteLine($"\nPaket empfangen: Befehl={paket.Befehl}, Inhalt={paket.Inhalt}");
    
    // Empfangenes Paket zur empfangen.xml hinzufügen (überschreibt hier, für Liste → List<Paket>)
    SpeichereXml(paket, "empfangen.xml");

    // Bspw. "Logik" => auf "PING" mit "PONG" antworten
    if (paket.Befehl == "PING")
        manager?.SendePaket(LadeXml("daten-host.xml") ?? new Paket("PONG"));
}


// → Manager ruft OnPaketEmpfangen automatisch auf wenn ein Paket ankommt
manager = new XmlNetzwerkManager(OnPaketEmpfangen);

if (wahl == "h")
{
    SpeichereXml(new Paket("PONG", "> Host"), "daten-host.xml");

    // Host starten (wartet auf Client)
    var hostTask = manager.StarteHost(12345);

    // Warten bis Verbindung steht (dann Paket senden)
    while (!manager._isConnected)
        await Task.Delay(500);
    
    // manager.SendePaket(new Paket("TEST", "-> Host!"));

    await hostTask;
}
else
{
    SpeichereXml(new Paket("PING", "> Client"), "daten-client.xml");

    // Client starten
    var clientTask = manager.StarteClient("localhost", 12345);

    while (!manager._isConnected)
        await Task.Delay(500);

    // manager.SendePaket(new Paket("PING", "-> Client!")); <- direkt im Code

    manager.SendePaket(LadeXml("daten-client.xml") ?? new Paket("PING")); // <- von XML-Datei

    await clientTask;
}



// ===== Hilfsmethoden: XML-Datei speichern/laden =====

// Paket → XML-Datei
void SpeichereXml(Paket paket, string dateiPfad)
{
    // Serializer für den Typ "Paket" erstellen
    var serializer = new XmlSerializer(typeof(Paket));

    // FileStream öffnen zum Schreiben (FileMode.Create = neu erstellen / überschreiben)
    using var fs = new FileStream(dateiPfad, FileMode.Create);

    // Paket-Objekt → XML → in die Datei schreiben
    serializer.Serialize(fs, paket);
}

// XML-Datei → Paket
Paket? LadeXml(string dateiPfad)
{
    if (!File.Exists(dateiPfad)) return null; // falls Datei nicht existiert 

    var serializer = new XmlSerializer(typeof(Paket));
    using var fs = new FileStream(dateiPfad, FileMode.Open);

    // XML aus Datei lesen → Paket-Objekt (Cast nötig da Deserialize object zurückgibt)
    return (Paket?)serializer.Deserialize(fs);
}


// ===== Wenn hinzufügen zur XML statt überchreiben =====
void FügeHinzuXml(Paket paket, string dateiPfad)
{
    // Bestehende Liste laden (oder neue erstellen wenn Datei nicht existiert)
    List<Paket> liste = LadeXmlListe(dateiPfad) ?? new List<Paket>();

    // Neues Paket hinzufügen
    liste.Add(paket);

    // Gesamte Liste wieder speichern
    var serializer = new XmlSerializer(typeof(List<Paket>));
    using var fs = new FileStream(dateiPfad, FileMode.Create);
    serializer.Serialize(fs, liste);
}
List<Paket>? LadeXmlListe(string dateiPfad)
{
    if (!File.Exists(dateiPfad)) return null;
    var serializer = new XmlSerializer(typeof(List<Paket>));
    using var fs = new FileStream(dateiPfad, FileMode.Open);
    return (List<Paket>?)serializer.Deserialize(fs);
}