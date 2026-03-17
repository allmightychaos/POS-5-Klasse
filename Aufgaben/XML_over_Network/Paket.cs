using System.Xml.Serialization;

namespace Xml_over_Network
{
    // ===== Paket-Klasse =====
    // Kann jederzeit um weitere Properties erweitert werden (zB. "Data", "ErrorCode")
    // WICHTIG: Muss einen parameterlosen Konstruktor haben (für XmlSerializer)
    
    public class Paket
    {
        // Der Befehl / Typ der Nachricht (zB. "SHOOT", "TREFFER")
        public string Befehl { get; set; } = "";

        // Optionaler Inhalt (zB. für Position, Spielnummer,..: "5,3", "42")
        public string Inhalt { get; set; } = "";


        // Parameterloser Konstruktor
        public Paket() { }


        // Hilfs-Konstruktor (für schnelleres Erstellen)
        public Paket(string befehl, string inhalt = "")
        {
            Befehl = befehl;
            Inhalt = inhalt;
        }

        public override string ToString() => $"[{Befehl}] {Inhalt}";
    }
}
