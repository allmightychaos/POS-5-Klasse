using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace Xml_over_Network
{
    // ===== XmlNetzwerkManager =====
    // Verwaltet die TCP-Verbindung und das Senden/Empfangen von XML-Paketen
    // Verwendung:
    //   1. new XmlNetzwerkManager(onPaketEmpfangen)
    //   2. await StarteHost(port) ODER await StarteClient(ip, port)
    //   3. SendePaket(new Paket("BEFEHL", "inhalt"))

    public class XmlNetzwerkManager
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        public bool _isConnected = false; // <- verwende um zu überprüfen ob Host&Client verbunden sind

        // wird aufgerufen wenn ein Paket empfangen wird
        private readonly Action<Paket> _onPaketEmpfangen;
        public XmlNetzwerkManager(Action<Paket> onPaketEmpfangen)
        {
            _onPaketEmpfangen = onPaketEmpfangen;
        }

        // Puffergröße für empfangene Daten
        // ACHTUNG: bei großen XML-Paketen ggf. erhöhen (8192)
        private const int PUFFER_GROESSE = 4096;



        // ===== Host starten =====
        public async Task StarteHost(int port = 12345)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[HOST] Warte auf Verbindung auf Port {port}...");

            _client = await listener.AcceptTcpClientAsync();
            listener.Stop();
            Console.WriteLine("[HOST] Client verbunden!");

            _isConnected = true;

            await StarteEmpfangsschleife();
        }


        // ===== Client starten =====
        public async Task StarteClient(string ip = "localhost", int port = 12345)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            Console.WriteLine($"[CLIENT] Verbunden mit {ip}:{port}");

            _isConnected = true;

            await StarteEmpfangsschleife();
        }



        // ===== Empfangsschleife =====
        // Empfängt XML → Deserialisiert → Ruft Callback auf
        private async Task StarteEmpfangsschleife()
        {
            _stream = _client!.GetStream();

            while (_client.Connected)
            {
                if (!await EmpfangePaket()) break;
            }

            _stream.Close();
            _client.Close();
            Console.WriteLine("\n[NETZWERK] Verbindung getrennt.");
        }


        // ===== Paket senden =====
        // Objekt → XML-String → Bytes → Netzwerk
        public void SendePaket(Paket paket)
        {
            if (_stream == null) throw new InvalidOperationException("\n\nKeine Verbindung.");

            // Paket → XML-String
            string xml = SerialisiereZuXml(paket);

            Console.WriteLine($"\n[SENDEN]\n{xml}");

            // XML-String → Bytes → senden
            byte[] daten = Encoding.UTF8.GetBytes(xml);
            _stream.Write(daten, 0, daten.Length);
        }


        // ===== Paket empfangen (intern) =====
        // Bytes → XML-String → Objekt → Callback
        private async Task<bool> EmpfangePaket()
        {
            try
            {
                byte[] puffer = new byte[PUFFER_GROESSE];

                int anzahlBytes = await _stream!.ReadAsync(puffer, 0, puffer.Length);
                if (anzahlBytes == 0) return false; // Verbindung geschlossen

                // Bytes → XML-String
                string xml = Encoding.UTF8.GetString(puffer, 0, anzahlBytes).Trim('\0', '\r', '\n', ' ');

                Console.WriteLine($"\n[EMPFANGEN]\n{xml}");

                // XML-String → Paket-Objekt
                Paket? paket = DeserialisieVonXml(xml);

                if (paket != null)
                {
                    // Callback → Progam.cs -> OnPaketEmpfangen(paket)
                    _onPaketEmpfangen(paket);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[FEHLER]\n{ex.Message}");
                return false;
            }
        }



        // ===== Hilfsmethoden: Serialisierung =====

        // Objekt → XML-String 
        private static string SerialisiereZuXml(Paket paket)
        {
            // konkreter Typ für XmlSerializer (Paket)
            var serializer = new XmlSerializer(typeof(Paket));

            var writer = new StringWriter(); // (StringWriter statt FileStream)

            // Paket-Objekt → XML → in den writer
            serializer.Serialize(writer, paket);

            // Den fertigen XML-String zurückgeben
            // (sieht dann zB. so aus: `<Paket><Befehl>PING</Befehl><Inhalt>123</Inhalt></Paket>`)
            return writer.ToString();
        }

        // XML-String → Objekt (StringReader statt FileStream)
        private static Paket? DeserialisieVonXml(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Paket));

                var reader = new StringReader(xml); // (StringWriter statt FileStream)

                // XML-String → Paket-Objekt (Cast nötig, da Deserialize object zurückgibt)
                return (Paket?)serializer.Deserialize(reader);
            }
            catch
            {
                Console.WriteLine("[FEHLER] XML konnte nicht deserialisiert werden.");
                return null;
            }
        }
    }
}