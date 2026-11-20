namespace SajamKnjiga.Core.Persistence;
/// <summary>
/// Trajno pamti sledeci ID za svaki tip entiteta; DAO vrsi dodelu ID-ja.
/// </summary>
public class IdBrojac
{
    public string TipEntiteta { get; set; } = "";
    public int SledeciId { get; set; } = 1;
}
