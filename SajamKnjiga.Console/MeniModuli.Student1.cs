
using SajamKnjiga.ConsoleApp;
using System.Collections.Generic;

namespace SajamKnjiga.ConsoleApp;

public static partial class MeniModuli
{

    static partial void RegistrujStudent1(List<IMeniModul> moduli)
    {
        moduli.Add(new PosetiociMeni());
        moduli.Add(new IzdavaciMeni());
        moduli.Add(new KupovineMeni());
        moduli.Add(new KnjigeMeni());
    }

}
