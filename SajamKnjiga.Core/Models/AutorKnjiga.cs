namespace SajamKnjiga.Core.Models;

/// <summary>
/// Pomocna veza vise-prema-vise izmedju autora i knjige.
/// </summary>
public class AutorKnjiga
{
    public int AutorId { get; set; }
    public int KnjigaId { get; set; }
}
