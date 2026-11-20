// Brza provera osnovnih poslovnih operacija na odvojenim testnim podacima.
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;
using SajamKnjiga.Core.Services;

// Runs in its own bin/.../Data folder: does not affect Console or WPF state.
var store=new RazmenaPodatakaService();
int count=0;
void Check(bool condition,string message)
{
    if(!condition)throw new InvalidOperationException("FAIL: "+message);
    Console.WriteLine("PASS: "+message);count++;
}
try
{
    XmlTransakcija.OporaviPrekinute();
    var demo=store.ProcitajPaket(Path.Combine(AppContext.BaseDirectory,"DEMO_SajamKnjiga.xml"));
    Check(demo.Autori.Count==4,"Demo includes four authors");
    Check(demo.Knjige.Count==6,"Demo includes six books");
    Check(demo.Posetioci.Count==3,"Demo includes three visitors");
    store.Uvezi(demo);
    Check(new XmlDao<Autor>("Autori.xml").UcitajSve().Count==4,"Imported author records");
    Check(new XmlDao<Posetilac>("Posetioci.xml").UcitajSve().Count==3,"Imported visitor records");
    var links=new AutorKnjigaService();
    var (authors,books)=links.UcitajPovezanePodatke();
    Check(authors.Any(a=>a.Knjige.Count>0)&&books.Any(b=>b.Autori.Count>0),"Bidirectional author-book links");
    var visitor=new PosetilacService();
    var (visitors,_,purchases)=visitor.UcitajPovezanePodatke();
    Check(visitors[0].ListaZelja.Count>0,"Wishlists reconstructed from XML");
    Check(purchases.Count==3,"Purchases loaded");
    var previous=new XmlDao<Kupovina>("Kupovine.xml").UcitajSve().Count;
    var newPurchase=visitor.EvidentirajKupovinu(1,4,DateTime.Today,5,"Smoke test");
    Check(newPurchase.Id>0&&new XmlDao<Kupovina>("Kupovine.xml").UcitajSve().Count==previous+1,"Purchase persisted");
    Check(!new XmlStorage<StavkaListeZelja>("StavkeListeZelja.xml").Ucitaj().Any(z=>z.PosetilacId==1&&z.KnjigaId==4),"Bought book removed from wishlist");
    Check(visitor.PonistiKupovinu(1,4),"Purchase cancellation");
    Check(new XmlStorage<StavkaListeZelja>("StavkeListeZelja.xml").Ucitaj().Any(z=>z.PosetilacId==1&&z.KnjigaId==4),"Cancelled purchase returns to wishlist");
    var deletion=new KontrolisanoBrisanjeService();
    Check(!deletion.ProveriBrisanjeKnjige(1).Dozvoljeno,"Referenced book cannot be deleted");
    var i=store.Izvezi(Path.Combine(AppContext.BaseDirectory,"test-export.xml"));
    Check(File.Exists(i),"Complete XML export");
    Check(store.ProcitajPaket(i).Knjige.Count==6,"Export can be re-imported");
    Console.WriteLine($"ALL {count} CORE SMOKE CHECKS PASSED");
}
catch(Exception ex)
{
    Console.Error.WriteLine(ex);Environment.ExitCode=1;
}
