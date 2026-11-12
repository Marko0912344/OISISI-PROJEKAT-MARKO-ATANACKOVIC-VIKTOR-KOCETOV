namespace SajamKnjiga.Core.Persistence;

// Zajednicki format za trajne identifikatore. Dogovoriti sa Studentom 1
// pre nego sto doda svoj DAO/perzistenciju (ne smemo imati dva brojacka sistema).
public class IdBrojac
{
    public string TipEntiteta { get; set; } = "";
    public int SledeciId { get; set; } = 1;
}
