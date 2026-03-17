
namespace SchiffeGUI
{
    public enum FeldZustand
    {
        Wasser,
        Schiff,     // ungetroffenes Schiff
        Treffer,    // getroffenes Schiff
        Versenkt,   // versenktes Schiff
        Fehlschuss  // ins Wasser getroffen
    }

    public enum Orientierung
    {
        Horizontal,
        Vertikal
    }

    public enum Schifftyp
    {
        Uboot = 2,
        Zerstoerer = 3,
        Kreuzer = 4,
        Schlachtschiff = 5
    }

    public enum Netzwerkbefehl
    {
        SCHUSS,
        WASSER,     // ins Wasser getroffen
        TREFFER,    // getroffenes Schiff
        VERSENKT,   // versenktes Schiff
        OK,         // nach WASSER/TREFFER/VERSENKT als Bestätigung
        UNGÜLTIG,   
        FERTIG,     // wenn Client alle Schiffe gesetzt hat
        STARTEN,    // wenn Host das Spiel startet
        VERLOREN    // schickt Verlierer -> Gewinner
    }

}