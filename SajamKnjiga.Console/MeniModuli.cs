namespace SajamKnjiga.ConsoleApp;

public static partial class MeniModuli
{
    public static List<IMeniModul> Napravi()
    {
        var moduli = new List<IMeniModul>();
        RegistrujStudent1(moduli);
        RegistrujStudent2(moduli);
        return moduli;
    }

    // Neimplementirani partial void se automatski izostavlja iz kompajliranja.
    // Svaki student dodaje svoju implementaciju u SOPSTVENOM .cs fajlu.
    static partial void RegistrujStudent1(List<IMeniModul> moduli);
    static partial void RegistrujStudent2(List<IMeniModul> moduli);
}
