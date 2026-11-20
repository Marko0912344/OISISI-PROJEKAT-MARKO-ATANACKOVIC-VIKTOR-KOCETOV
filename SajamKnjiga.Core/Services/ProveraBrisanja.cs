using System.Collections.Generic;
namespace SajamKnjiga.Core.Services;
/// <summary>
/// Rezultat provere brisanja sa razlozima zabrane.
/// </summary>
public class ProveraBrisanja
{
    public List<string> Razlozi { get; } = new();
    public bool Dozvoljeno => Razlozi.Count == 0;
}
