using NotDefteri.Models;
using NotDefteri.Services;

namespace NotDefteri;

public class MainForm : Form
{
    private readonly NotDeposu _depo = new();
    private Not? _secili;          // Şu an düzenlenen not (null = yeni not)
    private bool _listeYenileniyor; // Liste doldurulurken SelectedIndexChanged'i yok saymak için

    private readonly TextBox _txtAra = new() { PlaceholderText = "Ara (başlık veya içerik)...", Dock = DockStyle.Fill };
    private readonly ListBox _lstNotlar = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly TextBox _txtBaslik = new() { Dock = DockStyle.Fill };
    private readonly TextBox _txtIcerik = new()
    {
        Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, AcceptsTab = true
    };
    private readonly Label _lblTarih = new() { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 8, 3, 3) };
    private readonly ToolStripStatusLabel _lblDurum = new() { Text = "Hazır" };

    public MainForm()
    {
        Text = "Not Defteri";
        MinimumSize = new Size(700, 450);
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        KurulumArayuz();
        BaglaOlaylar();

        string? uyari = _depo.Yukle();
        if (uyari != null)
            MessageBox.Show(uyari, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        ListeyiDoldur();
        Durum($"{_depo.Notlar.Count} not yüklendi.");
    }

    // ---------- Arayüz ----------
    private void KurulumArayuz()
    {
        var btnYeni = new Button { Text = "Yeni", AutoSize = true, Name = "btnYeni" };
        var btnKaydet = new Button { Text = "Kaydet", AutoSize = true, Name = "btnKaydet" };
        var btnSil = new Button { Text = "Sil", AutoSize = true, Name = "btnSil" };
        var btnDisaAktar = new Button { Text = "Dışa Aktar (.txt)", AutoSize = true, Name = "btnDisaAktar" };

        btnYeni.Click += (_, _) => YeniNot();
        btnKaydet.Click += (_, _) => NotuKaydet();
        btnSil.Click += (_, _) => NotuSil();
        btnDisaAktar.Click += (_, _) => TxtOlarakDisaAktar();

        // Sol taraf: arama + liste
        var sol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        sol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sol.Controls.Add(_txtAra, 0, 0);
        sol.Controls.Add(_lstNotlar, 0, 1);

        // Sağ taraf: başlık, içerik, tarih, butonlar
        var butonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        butonlar.Controls.AddRange(new Control[] { btnYeni, btnKaydet, btnSil, btnDisaAktar });

        var sag = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(6) };
        sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sag.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sag.Controls.Add(new Label { Text = "Başlık", AutoSize = true }, 0, 0);
        sag.Controls.Add(_txtBaslik, 0, 1);
        sag.Controls.Add(new Label { Text = "İçerik", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, 2);
        sag.Controls.Add(_txtIcerik, 0, 3);
        sag.Controls.Add(_lblTarih, 0, 4);
        sag.Controls.Add(butonlar, 0, 5);

        var bolme = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 280, FixedPanel = FixedPanel.Panel1 };
        // SplitterDistance form boyutlanmadan önce verilirse kırpılır; yüklenince uygula.
        Load += (_, _) => bolme.SplitterDistance = 280;
        bolme.Panel1.Controls.Add(sol);
        bolme.Panel2.Controls.Add(sag);

        var durumCubugu = new StatusStrip();
        durumCubugu.Items.Add(_lblDurum);

        Controls.Add(bolme);
        Controls.Add(durumCubugu);
    }

    private void BaglaOlaylar()
    {
        _lstNotlar.SelectedIndexChanged += (_, _) =>
        {
            if (_listeYenileniyor) return;
            if (_lstNotlar.SelectedItem is Not secilen) NotuAlanlaraYaz(secilen);
        };
        _txtAra.TextChanged += (_, _) => ListeyiDoldur(_secili?.Id);
    }

    // ---------- Liste ----------
    private void ListeyiDoldur(Guid? seciliId = null)
    {
        _listeYenileniyor = true;
        try
        {
            string ara = _txtAra.Text.Trim();
            var gosterilecek = _depo.Notlar
                .Where(n => ara.Length == 0
                    || n.Baslik.Contains(ara, StringComparison.CurrentCultureIgnoreCase)
                    || n.Icerik.Contains(ara, StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(n => n.DuzenlemeTarihi)
                .ToArray();

            _lstNotlar.BeginUpdate();
            _lstNotlar.Items.Clear();
            _lstNotlar.Items.AddRange(gosterilecek);
            _lstNotlar.EndUpdate();

            if (seciliId != null)
            {
                var hedef = gosterilecek.FirstOrDefault(n => n.Id == seciliId);
                if (hedef != null) _lstNotlar.SelectedItem = hedef;
            }
        }
        finally
        {
            _listeYenileniyor = false;
        }
    }

    private void NotuAlanlaraYaz(Not not)
    {
        _secili = not;
        _txtBaslik.Text = not.Baslik;
        _txtIcerik.Text = not.Icerik;
        _lblTarih.Text = $"Oluşturma: {not.OlusturmaTarihi:dd.MM.yyyy HH:mm}   |   Son düzenleme: {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}";
    }

    // ---------- İşlemler ----------
    private void YeniNot()
    {
        _secili = null;
        _lstNotlar.ClearSelected();
        _txtBaslik.Clear();
        _txtIcerik.Clear();
        _lblTarih.Text = "Yeni not";
        _txtBaslik.Focus();
        Durum("Yeni not hazır.");
    }

    private void NotuKaydet()
    {
        string baslik = _txtBaslik.Text.Trim();
        if (baslik.Length == 0)
        {
            MessageBox.Show("Lütfen not için bir başlık girin.", "Eksik bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtBaslik.Focus();
            return;
        }

        bool yeni = _secili == null;
        if (yeni)
        {
            _secili = new Not();
            _depo.Notlar.Add(_secili);
        }

        _secili!.Baslik = baslik;
        _secili.Icerik = _txtIcerik.Text;
        _secili.DuzenlemeTarihi = DateTime.Now;

        string? hata = _depo.Kaydet();
        if (hata != null)
        {
            MessageBox.Show(hata, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Durum("Kaydedilemedi!");
            return;
        }

        ListeyiDoldur(_secili.Id);
        NotuAlanlaraYaz(_secili);
        Durum(yeni ? "Not eklendi ve kaydedildi." : "Not güncellendi ve kaydedildi.");
    }

    private void NotuSil()
    {
        if (_secili == null)
        {
            MessageBox.Show("Silmek için listeden bir not seçin.", "Not seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sonuc = MessageBox.Show($"\"{_secili.Baslik}\" notu silinsin mi?", "Silme onayı",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (sonuc != DialogResult.Yes) return;

        _depo.Notlar.Remove(_secili);
        string? hata = _depo.Kaydet();
        if (hata != null) MessageBox.Show(hata, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

        YeniNot();
        ListeyiDoldur();
        Durum("Not silindi.");
    }

    private void TxtOlarakDisaAktar()
    {
        if (_secili == null)
        {
            MessageBox.Show("Dışa aktarmak için önce bir not seçin.", "Not seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Dosya adında geçersiz karakterleri temizle.
        string guvenliAd = string.Concat(_secili.Baslik.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = guvenliAd,
            Title = "Notu dışa aktar"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string? hata = NotDeposu.DisaAktar(_secili, dialog.FileName);
        if (hata != null)
        {
            MessageBox.Show(hata, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Durum("Dışa aktarılamadı!");
        }
        else
        {
            Durum("Not dışa aktarıldı: " + dialog.FileName);
        }
    }

    private void Durum(string mesaj) => _lblDurum.Text = mesaj;
}
