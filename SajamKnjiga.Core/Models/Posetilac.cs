
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SajamKnjiga.Core.Models;

public class Posetilac : IEntitet
{
    public int Id { get; set; }

    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public DateTime DatumRodjenja { get; set; }

    public string Telefon { get; set; } = "";
    public string Email { get; set; } = "";

    public string BrojClanskeKarte { get; set; } = "";

    public int GodinaClanstva { get; set; }

    public StatusPosetioca Status { get; set; }

    public double ProsecnaOcena { get; set; }

    public int AdresaId { get; set; }

    [XmlIgnore]
    public Adresa? Adresa { get; set; }

    [XmlIgnore]
   
    public List<Kupovina> Kupovine { get; set; } = new();

    [XmlIgnore]
    public List<StavkaListeZelja> ListaZelja { get; set; } = new();

    public Posetilac()
    {
    }

    public enum StatusPosetioca
    {
        Redovan,
        VIP
    }

}
