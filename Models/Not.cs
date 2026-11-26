namespace NotDefteri.Models;

/// <summary>Tek bir notu temsil eder. JSON'a serileştirilir.</summary>
public class Not
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime DuzenlemeTarihi { get; set; } = DateTime.Now;

    // ListBox'ta başlığın görünmesini sağlar.
    public override string ToString() => string.IsNullOrWhiteSpace(Baslik) ? "(başlıksız)" : Baslik;
}
