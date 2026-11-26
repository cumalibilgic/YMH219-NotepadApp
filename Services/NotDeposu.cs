using System.Text;
using System.Text.Json;
using NotDefteri.Models;

namespace NotDefteri.Services;

/// <summary>Notları List&lt;Not&gt; olarak tutar; JSON dosyasına kaydeder/yükler ve .txt dışa aktarır.</summary>
public class NotDeposu
{
    private static readonly JsonSerializerOptions JsonSecenekleri = new() { WriteIndented = true };

    public List<Not> Notlar { get; private set; } = new();
    public string DosyaYolu { get; }

    public NotDeposu(string? dosyaYolu = null)
    {
        DosyaYolu = dosyaYolu ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NotDefteri", "notlar.json");
    }

    /// <summary>Dosyadan notları yükler. Kullanıcıya gösterilecek bir uyarı varsa döndürür, yoksa null.</summary>
    public string? Yukle()
    {
        if (!File.Exists(DosyaYolu))
        {
            Notlar = new List<Not>();
            return null; // İlk açılış: dosya henüz yok, hata değil.
        }

        try
        {
            string json = File.ReadAllText(DosyaYolu, Encoding.UTF8);
            Notlar = JsonSerializer.Deserialize<List<Not>>(json) ?? new List<Not>();
            return null;
        }
        catch (JsonException)
        {
            // Bozuk dosya: üzerine yazıp kaybetmemek için yedekle, boş listeyle devam et.
            Notlar = new List<Not>();
            string yedek = DosyaYolu + ".bozuk";
            try { File.Move(DosyaYolu, yedek, overwrite: true); } catch (IOException) { }
            return $"Not dosyası bozuk olduğu için okunamadı. Dosya şuraya yedeklendi:\n{yedek}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Notlar = new List<Not>();
            return "Not dosyası okunamadı: " + ex.Message;
        }
    }

    /// <summary>Notları JSON olarak kaydeder. Başarısızsa hata mesajı döndürür, başarılıysa null.</summary>
    public string? Kaydet()
    {
        try
        {
            string? klasor = Path.GetDirectoryName(DosyaYolu);
            if (!string.IsNullOrEmpty(klasor)) Directory.CreateDirectory(klasor);

            // Önce geçici dosyaya yaz, sonra taşı: yazma yarıda kesilirse eski veri bozulmaz.
            string gecici = DosyaYolu + ".tmp";
            File.WriteAllText(gecici, JsonSerializer.Serialize(Notlar, JsonSecenekleri), Encoding.UTF8);
            File.Move(gecici, DosyaYolu, overwrite: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Notlar kaydedilemedi: " + ex.Message;
        }
    }

    /// <summary>Bir notu düz metin (.txt) olarak dışa aktarır. Hata mesajı veya null döndürür.</summary>
    public static string? DisaAktar(Not not, string yol)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(not.Baslik);
            sb.AppendLine(new string('-', Math.Max(not.Baslik.Length, 3)));
            sb.AppendLine($"Oluşturma: {not.OlusturmaTarihi:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Düzenleme: {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
            sb.Append(not.Icerik);
            File.WriteAllText(yol, sb.ToString(), Encoding.UTF8);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Dışa aktarma başarısız: " + ex.Message;
        }
    }
}
