using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Persistence
{
    // Jedan prenosivi XML sadrzi sve podatke i veze sistema.
    // Ovo je pomocni format za uvoz/izvoz; radni podaci i dalje
    // ostaju u zasebnim XML datotekama.
    /// <summary>
    /// Prenosni XML paket svih entiteta i njihovih veza za uvoz i izvoz.
    /// </summary>
    [XmlRoot("SajamKnjiga")]
    public class SajamPaket
    {
        public int Verzija { get; set; } = 1;
        public DateTime DatumIzvoza { get; set; } = DateTime.Now;

        public List<Adresa> Adrese { get; set; } = new();
        public List<Autor> Autori { get; set; } = new();
        public List<Knjiga> Knjige { get; set; } = new();
        public List<Posetilac> Posetioci { get; set; } = new();
        public List<Izdavac> Izdavaci { get; set; } = new();
        public List<Kupovina> Kupovine { get; set; } = new();
        public List<AutorKnjiga> AutorKnjigaVeze { get; set; } = new();
        public List<StavkaListeZelja> StavkeListeZelja { get; set; } = new();
        public List<IdBrojac> IdBrojaci { get; set; } = new();
    }
}
