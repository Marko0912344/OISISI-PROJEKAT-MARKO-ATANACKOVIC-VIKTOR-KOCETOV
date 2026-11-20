namespace SajamKnjiga.ConsoleApp;

public static partial class MeniModuli
{
    static partial void RegistrujStudent2(List<IMeniModul> moduli) =>
        moduli.Add(new Student2Meni());
}
