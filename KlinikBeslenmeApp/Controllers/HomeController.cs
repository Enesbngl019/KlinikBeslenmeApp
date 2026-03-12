using Microsoft.AspNetCore.Mvc;
using KlinikBeslenmeApp.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;

namespace KlinikBeslenmeApp.Controllers
{
    public class HomeController : Controller
    {
        KlinikBeslenmeDbContext _context = new KlinikBeslenmeDbContext();

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(email))
            {
                var hasta = _context.TblHastalars.FirstOrDefault(x => x.Email == email);
                if (hasta != null) return RedirectToAction("AnaPanel", new { id = hasta.HastaId });
            }

            ViewBag.KullaniciAd = HttpContext.Session.GetString("KullaniciAd");
            ViewBag.Colyak = HttpContext.Session.GetString("Colyak") == "1";
            ViewBag.Tansiyon = HttpContext.Session.GetString("Tansiyon") == "1";
            ViewBag.Diyabet = HttpContext.Session.GetString("Diyabet") == "1";

            var malzemeler = _context.TblMalzemelers.ToList();
            return View(malzemeler);
        }

        [HttpGet]
        public IActionResult GirisYap() { return View(); }

        [HttpPost]
        public IActionResult GirisYap(string Email, string Sifre)
        {
            var girisYapanHasta = _context.TblHastalars.FirstOrDefault(x => x.Email == Email && x.Sifre == Sifre);
            if (girisYapanHasta != null)
            {
                HttpContext.Session.SetString("Email", girisYapanHasta.Email);
                HttpContext.Session.SetString("KullaniciAd", girisYapanHasta.Ad);
                HttpContext.Session.SetString("Colyak", girisYapanHasta.ColyakMi == true ? "1" : "0");
                HttpContext.Session.SetString("Diyabet", girisYapanHasta.DiyabetMi == true ? "1" : "0");
                HttpContext.Session.SetString("Tansiyon", girisYapanHasta.TansiyonHastasiMi == true ? "1" : "0");

                return RedirectToAction("AnaPanel", new { id = girisYapanHasta.HastaId });
            }
            ViewBag.HataMesaji = "E-posta adresiniz veya þifreniz hatalý. Lütfen tekrar deneyin!";
            return View();
        }

        public IActionResult KayitOl() { return View(); }

        [HttpPost]
        public IActionResult KayitOl(TblHastalar yeniHasta)
        {
            var mailKontrol = _context.TblHastalars.FirstOrDefault(x => x.Email == yeniHasta.Email);
            if (mailKontrol != null)
            {
                ViewBag.HataMesaji = "Bu e-posta adresi zaten kayýtlýdýr!";
                return View(yeniHasta);
            }

            if (!string.IsNullOrEmpty(yeniHasta.Telefon)) yeniHasta.Telefon = "+90" + yeniHasta.Telefon;

            var bugun = DateTime.Today;
            var yas = bugun.Year - yeniHasta.DogumTarihi.Year;
            if (yeniHasta.DogumTarihi.Date > bugun.AddYears(-yas)) yas--;
            yeniHasta.Yas = yas;

            try
            {
                _context.TblHastalars.Add(yeniHasta);
                _context.SaveChanges();
                TempData["BasariMesaji"] = "Kayýt iþleminiz baþarýyla tamamlandý! Lütfen giriþ yapýnýz.";
                return RedirectToAction("GirisYap");
            }
            catch (Exception ex)
            {
                ViewBag.HataMesaji = "Bir hata oluþtu: " + ex.Message;
                return View(yeniHasta);
            }
        }
        [HttpGet]
        public IActionResult AnaPanel(int id)
        {
            var hasta = _context.TblHastalars.Find(id);
            if (hasta == null) return RedirectToAction("GirisYap");

            
            DateTime bugun = DateTime.Today;
            var bugunkuYemekler = _context.TblYemekGunlugus
                                          .Where(x => x.HastaId == id && x.TuketimTarihi != null && x.TuketimTarihi.Value.Date == bugun)
                                          .ToList();

            
            ViewBag.BugunOgunSayisi = bugunkuYemekler.Select(x => x.KayitId).Distinct().Count();
            
            ViewBag.BugunCesitSayisi = bugunkuYemekler.Count;
            
            ViewBag.BugunPorsiyon = bugunkuYemekler.Sum(x => x.Porsiyon) ?? 0;
         

            return View(hasta);
        }
        public IActionResult Profilim()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("GirisYap");

            var kullanici = _context.TblHastalars.FirstOrDefault(x => x.Email == email);
            if (kullanici == null) return RedirectToAction("GirisYap");

            if (kullanici.Boy != null && kullanici.Kilo != null && kullanici.Boy > 0)
            {
                double boyMetre = kullanici.Boy.Value / 100.0;
                double vki = kullanici.Kilo.Value / (boyMetre * boyMetre);
                ViewBag.VKI = System.Math.Round(vki, 1);

                if (vki < 18.5) ViewBag.Durum = "Zayýf (Kilo Almalýsýnýz)";
                else if (vki < 24.9) ViewBag.Durum = "Normal (Harika!)";
                else if (vki < 29.9) ViewBag.Durum = "Fazla Kilolu (Diyete Baþlamalýsýnýz)";
                else ViewBag.Durum = "Obezite Riski (Acil Diyetisyen Desteði)";
            }
            return View(kullanici);
        }

        [HttpGet]
        public IActionResult ProfilDuzenle(int id)
        {
            var hasta = _context.TblHastalars.Find(id);
            if (hasta == null) return RedirectToAction("GirisYap");
            return View(hasta);
        }

        [HttpPost]
        public IActionResult ProfilDuzenle(TblHastalar guncelHasta)
        {
            var mailKontrol = _context.TblHastalars.FirstOrDefault(x => x.Email == guncelHasta.Email && x.HastaId != guncelHasta.HastaId);
            if (mailKontrol != null)
            {
                ViewBag.HataMesaji = "Bu e-posta adresi baþka bir kullanýcý tarafýndan kullanýlýyor!";
                return View(guncelHasta);
            }

            var bugun = DateTime.Today;
            var yas = bugun.Year - guncelHasta.DogumTarihi.Year;
            if (guncelHasta.DogumTarihi.Date > bugun.AddYears(-yas)) yas--;
            guncelHasta.Yas = yas;

            try
            {
                _context.TblHastalars.Update(guncelHasta);
                _context.SaveChanges();
                TempData["BasariMesaji"] = "Profil bilgileriniz baþarýyla güncellendi!";
                return RedirectToAction("Profilim", new { id = guncelHasta.HastaId });
            }
            catch (Exception ex)
            {
                ViewBag.HataMesaji = "Güncelleme sýrasýnda bir hata oluþtu: " + ex.Message;
                return View(guncelHasta);
            }
        }

        [HttpGet]
        public IActionResult GunlukMenu(int id, int? cId = null, int? aId = null, int? yId = null, bool yenile = false)
        {
            var hasta = _context.TblHastalars.Find(id);
            if (hasta == null) return RedirectToAction("GirisYap");

            ViewBag.HastaId = id;
            ViewBag.HastaAd = hasta.Ad;
            ViewBag.ColyakMi = hasta.ColyakMi;
            ViewBag.DiyabetMi = hasta.DiyabetMi;
            ViewBag.TansiyonMi = hasta.TansiyonHastasiMi;

            var tumYemekler = _context.TblYemeklers.AsQueryable();

            if (hasta.ColyakMi == true) { tumYemekler = tumYemekler.Where(x => x.Kategori != "Börekler ve Pideler" && x.Kategori != "Tatlýlar" && !x.YemekAdi.Contains("Makarna") && !x.YemekAdi.Contains("Bulgur") && !x.YemekAdi.Contains("Þehriye") && !x.YemekAdi.Contains("Eriþte")); }
            if (hasta.DiyabetMi == true) { tumYemekler = tumYemekler.Where(x => x.Kategori != "Tatlýlar" && !x.YemekAdi.Contains("Kýzartma") && !x.YemekAdi.Contains("Pirinç")); }
            if (hasta.TansiyonHastasiMi == true) { tumYemekler = tumYemekler.Where(x => !x.YemekAdi.Contains("Turþu") && !x.YemekAdi.Contains("Pastýrma")); }

            var guvenliListe = tumYemekler.ToList();
            var rand = new Random();

            string sKeyC = $"MenuC_{id}";
            string sKeyA = $"MenuA_{id}";
            string sKeyY = $"MenuY_{id}";

            if (yenile)
            {
                HttpContext.Session.Remove(sKeyC);
                HttpContext.Session.Remove(sKeyA);
                HttpContext.Session.Remove(sKeyY);
            }

            TblYemekler secilenCorba = null;
            if (cId == 0) secilenCorba = guvenliListe.Where(x => x.Kategori == "Çorbalar").OrderBy(x => rand.Next()).FirstOrDefault();
            else if (cId.HasValue && cId > 0) secilenCorba = guvenliListe.FirstOrDefault(x => x.YemekId == cId.Value);
            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(sKeyC))) secilenCorba = guvenliListe.FirstOrDefault(x => x.YemekId == int.Parse(HttpContext.Session.GetString(sKeyC)));
            if (secilenCorba == null) secilenCorba = guvenliListe.Where(x => x.Kategori == "Çorbalar").OrderBy(x => rand.Next()).FirstOrDefault();

            TblYemekler secilenAnaYemek = null;
            if (aId == 0) secilenAnaYemek = guvenliListe.Where(x => x.Kategori.Contains("Et") || x.Kategori.Contains("Tavuk") || x.Kategori.Contains("Balýk")).OrderBy(x => rand.Next()).FirstOrDefault();
            else if (aId.HasValue && aId > 0) secilenAnaYemek = guvenliListe.FirstOrDefault(x => x.YemekId == aId.Value);
            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(sKeyA))) secilenAnaYemek = guvenliListe.FirstOrDefault(x => x.YemekId == int.Parse(HttpContext.Session.GetString(sKeyA)));
            if (secilenAnaYemek == null) secilenAnaYemek = guvenliListe.Where(x => x.Kategori.Contains("Et") || x.Kategori.Contains("Tavuk") || x.Kategori.Contains("Balýk")).OrderBy(x => rand.Next()).FirstOrDefault();

            TblYemekler secilenTamamlayici = null;
            if (yId == 0) secilenTamamlayici = guvenliListe.Where(x => x.Kategori.Contains("Salatalar") || x.Kategori.Contains("Zeytinyaðlýlar")).OrderBy(x => rand.Next()).FirstOrDefault();
            else if (yId.HasValue && yId > 0) secilenTamamlayici = guvenliListe.FirstOrDefault(x => x.YemekId == yId.Value);
            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(sKeyY))) secilenTamamlayici = guvenliListe.FirstOrDefault(x => x.YemekId == int.Parse(HttpContext.Session.GetString(sKeyY)));
            if (secilenTamamlayici == null) secilenTamamlayici = guvenliListe.Where(x => x.Kategori.Contains("Salatalar") || x.Kategori.Contains("Zeytinyaðlýlar")).OrderBy(x => rand.Next()).FirstOrDefault();

            if (secilenCorba != null) HttpContext.Session.SetString(sKeyC, secilenCorba.YemekId.ToString());
            if (secilenAnaYemek != null) HttpContext.Session.SetString(sKeyA, secilenAnaYemek.YemekId.ToString());
            if (secilenTamamlayici != null) HttpContext.Session.SetString(sKeyY, secilenTamamlayici.YemekId.ToString());

            var secilenYemekler = new List<TblYemekler>();
            if (secilenCorba != null) secilenYemekler.Add(secilenCorba);
            if (secilenAnaYemek != null) secilenYemekler.Add(secilenAnaYemek);
            if (secilenTamamlayici != null) secilenYemekler.Add(secilenTamamlayici);

            var gercekMenu = new List<GunlukMenuViewModel>();

            foreach (var yemek in secilenYemekler)
            {
                var tarifDetaylari = (from r in _context.TblYemekMalzemeleris
                                      join m in _context.TblMalzemelers on r.MalzemeId equals m.MalzemeId
                                      where r.YemekId == yemek.YemekId
                                      select new { Gramaj = r.MiktarGram, Kalori100g = m.Kalori, Protein100g = m.Protein, Karb100g = m.Karbonhidrat, Sodyum100g = m.Sodyum, GI = m.GlisemikIndeks }).ToList();

                double topKalori = 0, topProtein = 0, topKarb = 0, topSodyum = 0; int maxGI = 0;
                foreach (var detay in tarifDetaylari)
                {
                    double gramaj = Convert.ToDouble(detay.Gramaj); double oran = gramaj / 100.0;
                    topKalori += oran * Convert.ToDouble(detay.Kalori100g); topProtein += oran * Convert.ToDouble(detay.Protein100g);
                    topKarb += oran * Convert.ToDouble(detay.Karb100g); topSodyum += oran * Convert.ToDouble(detay.Sodyum100g);
                    int currentGI = Convert.ToInt32(detay.GI); if (currentGI > maxGI) maxGI = currentGI;
                }

                gercekMenu.Add(new GunlukMenuViewModel
                {
                    YemekId = yemek.YemekId,
                    YemekAdi = yemek.YemekAdi,
                    Kategori = yemek.Kategori,
                    ToplamKalori = Math.Round(topKalori, 0),
                    ToplamProtein = Math.Round(topProtein, 1),
                    ToplamKarb = Math.Round(topKarb, 1),
                    ToplamSodyum = Math.Round(topSodyum, 1),
                    MaxGlisemikIndeks = maxGI
                });
            }
            return View(gercekMenu);
        }

        public IActionResult CikisYap()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("GirisYap");
        }

        
        [HttpGet]
        public IActionResult YemekGunluguEkle(int id)
        {
            var hasta = _context.TblHastalars.Find(id);
            if (hasta == null) return RedirectToAction("GirisYap");

            ViewBag.HastaId = id;
            ViewBag.HastaAd = hasta.Ad;

            var tumYemekler = _context.TblYemeklers.OrderBy(x => x.YemekAdi).ToList();
            var yemekKalorileri = new Dictionary<int, double>();

            foreach (var y in tumYemekler)
            {
                var detaylar = (from r in _context.TblYemekMalzemeleris
                                join m in _context.TblMalzemelers on r.MalzemeId equals m.MalzemeId
                                where r.YemekId == y.YemekId
                                select new { r.MiktarGram, m.Kalori }).ToList();

                double topKalori = 0;
                foreach (var d in detaylar)
                {
                    topKalori += (Convert.ToDouble(d.MiktarGram) / 100.0) * Convert.ToDouble(d.Kalori);
                }
                yemekKalorileri.Add(y.YemekId, Math.Round(topKalori, 0));
            }

            ViewBag.Yemekler = tumYemekler;
            ViewBag.Kaloriler = yemekKalorileri;

            return View();
        }

        [HttpPost]
        public IActionResult YemekGunluguEkle(TblYemekGunlugu data, List<int> secilenYemekler, List<double> secilenPorsiyonlar, DateTime? ozelTarih)
        {
            if (secilenYemekler == null || secilenYemekler.Count == 0)
            {
                TempData["Hata"] = "Lütfen en az bir yemek seçin!";
                return RedirectToAction("YemekGunluguEkle", new { id = data.HastaId });
            }

            DateTime ortakKayitZamani = ozelTarih ?? DateTime.Now;
            string benzersizKayitId = Guid.NewGuid().ToString().Substring(0, 8);

            for (int i = 0; i < secilenYemekler.Count; i++)
            {
                double porsiyon = (secilenPorsiyonlar != null && secilenPorsiyonlar.Count > i) ? secilenPorsiyonlar[i] : 1.0;

                var yeniKayit = new TblYemekGunlugu
                {
                    HastaId = data.HastaId,
                    YemekId = secilenYemekler[i],
                    Porsiyon = porsiyon,
                    OgunTipi = data.OgunTipi,
                    Aciklama = data.Aciklama,
                    TuketimTarihi = ortakKayitZamani, 
                    KayitId = benzersizKayitId
                };
                _context.TblYemekGunlugus.Add(yeniKayit);
            }

            _context.SaveChanges();
            TempData["Mesaj"] = "Afiyet olsun! Bütün öðünlerin baþarýyla kaydedildi.";
            return RedirectToAction("AnaPanel", new { id = data.HastaId });
        }
        
        [HttpGet]
        [HttpGet]
        public IActionResult YemekGecmisi(int id, DateTime? filtreTarih = null)
        {
            var hasta = _context.TblHastalars.Find(id);
            if (hasta == null) return RedirectToAction("GirisYap");

            ViewBag.HastaId = id;
            ViewBag.HastaAd = hasta.Ad;

            DateTime seciliTarih = filtreTarih ?? DateTime.Today;
            ViewBag.SeciliTarih = seciliTarih.ToString("yyyy-MM-dd");

            var gecmis = (from g in _context.TblYemekGunlugus
                          join y in _context.TblYemeklers on g.YemekId equals y.YemekId
                         
                          where g.HastaId == id && g.TuketimTarihi != null && g.TuketimTarihi.Value.Date == seciliTarih.Date
                          orderby g.TuketimTarihi descending
                          select new YemekGecmisiViewModel
                          {
                              GunlukId = g.GunlukId,
                              YemekAdi = y.YemekAdi,
                              Kategori = y.Kategori,
                              OgunTipi = g.OgunTipi,
                              TuketimTarihi = g.TuketimTarihi.Value,
                              Aciklama = g.Aciklama,
                              KayitId = g.KayitId
                          }).ToList();

            
            return View(gecmis);
        }

        [HttpPost]
        public IActionResult YemekGunluguSil(int gunlukId, int hastaId)
        {
            var silinecekYemek = _context.TblYemekGunlugus.Find(gunlukId);
            if (silinecekYemek != null)
            {
                _context.TblYemekGunlugus.Remove(silinecekYemek);
                _context.SaveChanges();

                TempData["Mesaj"] = "Öðün baþarýyla silindi!";
            }

            return RedirectToAction("YemekGecmisi", new { id = hastaId });
        }
    }

    
    public class GunlukMenuViewModel
    {
        public int YemekId { get; set; }
        public string YemekAdi { get; set; }
        public string Kategori { get; set; }
        public double ToplamKalori { get; set; }
        public double ToplamProtein { get; set; }
        public double ToplamKarb { get; set; }
        public double ToplamSodyum { get; set; }
        public int MaxGlisemikIndeks { get; set; }
    }

    public class YemekGecmisiViewModel
    {
        public int GunlukId { get; set; }
        public string YemekAdi { get; set; }
        public string Kategori { get; set; }
        public string? OgunTipi { get; set; }
        public DateTime? TuketimTarihi { get; set; }
        public string? Aciklama { get; set; }
        public string KayitId { get; set; }
    }
}