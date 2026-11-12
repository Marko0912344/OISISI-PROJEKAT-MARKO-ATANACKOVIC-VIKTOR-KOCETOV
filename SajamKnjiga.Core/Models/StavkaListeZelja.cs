namespace SajamKnjiga.Core.Models;

// Zajednicka relacija posetilac-knjiga, bez implementacije klase Posetilac.
public class StavkaListeZelja
{
    public int PosetilacId { get; set; }
    public int KnjigaId { get; set; }
}
