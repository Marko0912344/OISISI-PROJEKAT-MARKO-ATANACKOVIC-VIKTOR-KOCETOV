namespace SajamKnjiga.ConsoleApp;

// Zajednicki ugovor: svaki student dodaje svoj meni u odvojenom fajlu.
public interface IMeniModul
{
    string Naziv { get; }
    void Pokreni();
}
