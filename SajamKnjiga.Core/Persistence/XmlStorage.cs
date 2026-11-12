using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SajamKnjiga.Core.Persistence
{
    /// <summary>
    /// Cita i cuva listu entiteta u zasebnoj XML datoteci.
    /// Upis koristi privremenu datoteku i zadrzava prethodnu verziju (*.bak).
    /// Ovo stiti POJEDINACNU datoteku; nije transakcija preko vise XML fajlova.
    /// </summary>
    public class XmlStorage<T>
    {
        private static readonly XmlSerializer Serializer =
            new XmlSerializer(typeof(List<T>));

        private readonly string _filePath;

        public XmlStorage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "Naziv XML datoteke je obavezan.", nameof(fileName));
            }

            string dataFolder = Path.Combine(
                AppContext.BaseDirectory, "Data");

            Directory.CreateDirectory(dataFolder);

            _filePath = Path.Combine(dataFolder, fileName);
        }

        public string Putanja => _filePath;

        public List<T> Ucitaj()
        {
            if (!File.Exists(_filePath))
            {
                return new List<T>();
            }

            using FileStream stream = File.OpenRead(_filePath);

            try
            {
                return (List<T>?)Serializer.Deserialize(stream)
                    ?? throw new InvalidDataException(
                        $"XML datoteka '{_filePath}' ne sadrzi listu podataka.");
            }
            catch (InvalidOperationException ex)
            {
                // Ne smemo pokvareni XML tumaciti kao praznu listu.
                // Time bismo mogli da pregazimo postojecu datoteku.
                throw new InvalidDataException(
                    $"XML datoteka '{_filePath}' ne moze da se procita. " +
                    "Proveri datoteku ili rezervnu kopiju (*.bak).", ex);
            }
        }

        public void Sacuvaj(List<T> podaci)
        {
            ArgumentNullException.ThrowIfNull(podaci);

            string? folder = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new InvalidOperationException(
                    "Nije moguce utvrditi folder za XML datoteku.");
            }

            Directory.CreateDirectory(folder);

            // Privremena datoteka je u ISTOM folderu kao glavna,
            // kako bi njena zamena mogla da se izvrsi na istom disku.
            string privremena = _filePath + "." +
                Guid.NewGuid().ToString("N") + ".tmp";

            string rezervnaKopija = _filePath + ".bak";

            try
            {
                // 1. Prvo serijalizujemo sve u privremenu datoteku.
                using (FileStream stream = new FileStream(
                    privremena,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    Serializer.Serialize(stream, podaci);
                    stream.Flush(flushToDisk: true);
                }

                // 2. Tek posle uspesne serijalizacije menjamo glavni XML.
                if (File.Exists(_filePath))
                {
                    // Stari XML postaje *.bak, novi preuzima glavno ime.
                    File.Replace(
                        privremena,
                        _filePath,
                        rezervnaKopija,
                        ignoreMetadataErrors: true);
                }
                else
                {
                    // Pri prvom upisu nema prethodnog XML-a za backup.
                    File.Move(privremena, _filePath);
                }
            }
            finally
            {
                // Posle normalnog upisa privremena datoteka vise ne postoji.
                // Ako upis nije uspeo, pokusavamo da je uklonimo.
                if (File.Exists(privremena))
                {
                    try
                    {
                        File.Delete(privremena);
                    }
                    catch (IOException)
                    {
                        // Ne prikrivamo prvobitnu gresku pri upisu.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Ne prikrivamo prvobitnu gresku pri upisu.
                    }
                }
            }
        }
    }
}
