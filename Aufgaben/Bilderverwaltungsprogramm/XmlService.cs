using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Bilderverwaltungsprogramm
{
    public class XmlService
    {
        public static string XmlPfad = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "BVP-Album", "daten.xml");

        public static void Speichern(ObservableCollection<Album> alben)
        {
            // 1. Serializer erstellen
            XmlSerializer serializer = new XmlSerializer(typeof(List<Album>));

            // 2. Datei öffnen zum Schreiben
            using (FileStream fs = new FileStream(XmlPfad, FileMode.Create))
            {
                // 3. ObservableCollection -> Liste -> XML
                serializer.Serialize(fs, alben.ToList());
            }
        }

        public static ObservableCollection<Album> Laden()
        {
            // 1. Serializer erstellen
            XmlSerializer serializer = new XmlSerializer(typeof(List<Album>));

            // 2. Datei öffnen zum Schreiben
            using (FileStream fs = new FileStream(XmlPfad, FileMode.Open))
            {
                // 3. XML -> Liste
                List<Album> geladen = (List<Album>)serializer.Deserialize(fs);

                // 4. Liste -> ObservableCollection
                ObservableCollection<Album> XmlAlbumAuswahl = new ObservableCollection<Album>();

                foreach (Album a in geladen)
                {
                    XmlAlbumAuswahl.Add(a);
                }

                return XmlAlbumAuswahl;
            }
        }
    }
}
