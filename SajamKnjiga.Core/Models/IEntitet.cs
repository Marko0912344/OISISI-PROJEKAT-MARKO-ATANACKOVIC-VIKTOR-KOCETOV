namespace SajamKnjiga.Core.Models;
/// <summary>
/// Ugovor za entitete kojima DAO pristupa preko jedinstvenog ID-ja.
/// </summary>
public interface IEntitet
{
    int Id { get; set; }
}
