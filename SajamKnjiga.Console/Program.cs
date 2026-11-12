using System.Text;
using SajamKnjiga.ConsoleApp;

Console.OutputEncoding = Encoding.UTF8;

while (true)
{
    var moduli = MeniModuli.Napravi();
    Console.WriteLine("\n=== SAJAM KNJIGA - ZAJEDNICKA KONZOLA ===");
    for (int i = 0; i < moduli.Count; i++)
        Console.WriteLine($"{i + 1}. {moduli[i].Naziv}");
    Console.WriteLine("0. Izlaz");
    Console.Write("Izbor: ");
    string? unos = Console.ReadLine();
    if (unos == "0") break;
    if (int.TryParse(unos, out int n) && n >= 1 && n <= moduli.Count)
    {
        try { moduli[n - 1].Pokreni(); }
        catch (Exception ex) { Console.WriteLine("Greska: " + ex.Message); }
    }
    else Console.WriteLine("Nepostojeca opcija.");
}
