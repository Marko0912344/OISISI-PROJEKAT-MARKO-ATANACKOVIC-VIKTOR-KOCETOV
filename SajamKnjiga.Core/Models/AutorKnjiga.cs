namespace SajamKnjiga.Core.Models;

// Zajednicka relacija M:N: isti XML koristi Student 1 i Student 2.
public class AutorKnjiga
{
    public int AutorId { get; set; }
    public int KnjigaId { get; set; }
}
