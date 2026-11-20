namespace SajamKnjiga.Core.Models;

/// <summary>
/// Veza posetioca i knjige koju posetilac zeli da kupi.
/// </summary>
public class StavkaListeZelja
{
    public int PosetilacId { get; set; }
    public int KnjigaId { get; set; }
}
