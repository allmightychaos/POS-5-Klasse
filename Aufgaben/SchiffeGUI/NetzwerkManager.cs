using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SchiffeGUI
{
    public class NetzwerkManager
    {
        private SpielViewModel _viewModel;
        private TcpClient _client;
        private NetworkStream _stream;

        public NetzwerkManager(SpielViewModel viewModel)
        {
            this._viewModel = viewModel;
        }



        // --- Host & Client --- \\
        async public Task StarteHost()
        {
            // 1. Der "Türsteher"
            // IPAddress.Any -> hört auf alle Netzwerk-Eingänge // 12345 -> Port
            TcpListener listener = new TcpListener(IPAddress.Any, 12345);
            listener.Start();

            // 3. AcceptTcpClient() -> wartet bis Client kommt.
            _client = await listener.AcceptTcpClientAsync();
            listener.Stop();

            _viewModel.NetConnected();

            // ---------------------------------------------------------------------

            _stream = _client.GetStream();

            while (_client.Connected)
            {
                // -) Nachricht erhalten
                if (!await getMessage()) { break; }
            }

            // -) Verbindung schließen
            _stream.Close();
            _client.Close();
        }

        async public Task StarteClient(string ipaddress, int port)
        {
            // -) Client starten
            _client = new TcpClient();
            await _client.ConnectAsync(ipaddress, port);

            _viewModel.NetConnected();

            // ---------------------------------------------------------------------

            _stream = _client.GetStream();

            while (_client.Connected)
            {
                // -) Nachricht erhalten
                if (!await getMessage()) { break; }
            }

            // -) Verbindung schließen
            _stream.Close();
            _client.Close();
        }


        // --- Hilfsfunktionen --- \\

        public void sendMessage(Netzwerkbefehl befehl, string? payload = null)
        {
            string message = befehl.ToString();
            if (payload != null)
            {
                message += $"|{payload}";
            }

            // -) Nachricht -> Bytes
            byte[] daten = Encoding.UTF8.GetBytes(message);

            // -) Paket schicken
            _stream.Write(daten, 0, daten.Length);
        }

        private async Task<bool> getMessage()
        {
            try
            {
                byte[] eimer = new byte[1024];

                // -) Warten auf Nachricht
                int anzahlBytes = await _stream.ReadAsync(eimer, 0, eimer.Length);
                if (anzahlBytes == 0) { return false; } // wenn Bytes = 0 -> Verbindung wurde geschlossen

                // -) Bytes -> Text (& entfernen aller Null-Bytes und Leerzeichen)
                string rawText = Encoding.UTF8.GetString(eimer, 0, anzahlBytes);
                string empfangeneNachricht = rawText.Trim('\0', '\r', '\n', ' ');

                // -) Nachricht verarbeiten
                HandleMessage(empfangeneNachricht);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void HandleMessage(string message)
        {
            // Split the message (e.g. "SCHUSS|20" -> {"SCHUSS", "20"}
            string[] parts = message.Split('|');
            string command = parts[0];
            string payload;

            if (parts.Length > 1)
            {
                payload = parts[1];
            }
            else
            {
                payload = "";
            }

            switch (command)
            {
                // SCHUSS|45
                case "SCHUSS":
                    int schussIndex = int.Parse(payload);

                    Netzwerkbefehl antwort = _viewModel.CheckHit(schussIndex, out string antwortPayload);
                    sendMessage(antwort, antwortPayload);
                    break;

                // WASSER|10
                case "WASSER":
                    int wasserIndex = int.Parse(payload);

                    _viewModel.GegnerListe[wasserIndex].Zustand = FeldZustand.Fehlschuss;
                    sendMessage(Netzwerkbefehl.OK);
                    break;

                // TREFFER|10
                case "TREFFER":
                    int trefferIndex = int.Parse(payload);

                    _viewModel.GegnerListe[trefferIndex].Zustand = FeldZustand.Treffer;
                    sendMessage(Netzwerkbefehl.OK);
                    break;

                // VERSENKT|0,1,2,3,4
                case "VERSENKT":
                    string[] versenktIndizes = payload.Split(',');

                    foreach (string idxStr in versenktIndizes)
                    {
                        int idx = int.Parse(idxStr);

                        _viewModel.GegnerListe[idx].Zustand = FeldZustand.Versenkt;
                    }
                    sendMessage(Netzwerkbefehl.OK);
                    break;

                // OK
                case "OK":
                    _viewModel.SwapTurn();
                    break;
                
                // UNGÜLTIG
                case "UNGÜLTIG":
                    _viewModel.SpielStatus = "Ungültig.";
                    break;

                // FERTIG
                case "FERTIG":
                    _viewModel.ClientReady();
                    break;

                // STARTEN
                case "STARTEN":
                    _viewModel.SpielStarten();
                    break;

                // VERLOREN|0,1,2,3,4 (=> Empfänger hat gewonnen!)
                case "VERLOREN":
                    _viewModel.SpielStatus = "Du hast das Spiel gewonnen!";
                    string[] verlorenIndizes = payload.Split(',');

                    foreach (string idxStr in verlorenIndizes)
                    {
                        int idx = int.Parse(idxStr);

                        _viewModel.GegnerListe[idx].Zustand = FeldZustand.Versenkt;
                    }
                    break;
            }
        }
    }
}
