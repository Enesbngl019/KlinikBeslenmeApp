using Microsoft.AspNetCore.Mvc;
using KlinikBeslenmeApp.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.Data;

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
                HttpContext.Session.SetInt32("HastaId", girisYapanHasta.HastaId);
                HttpContext.Session.SetString("Colyak", girisYapanHasta.ColyakMi == true ? "1" : "0");
                HttpContext.Session.SetString("Diyabet", girisYapanHasta.DiyabetMi == true ? "1" : "0");
                HttpContext.Session.SetString("Tansiyon", girisYapanHasta.TansiyonHastasiMi == true ? "1" : "0");

                return RedirectToAction("AnaPanel", new { id = girisYapanHasta.HastaId });
            }
            ViewBag.HataMesaji = "E-posta adresiniz veya şifreniz hatalı. Lütfen tekrar deneyin!";
            return View();
        }

        [HttpGet]
        public IActionResult KayitOl()
        {
           
            ViewBag.Doktorlar = _context.TblDoktorlars.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult KayitOl(TblHastalar yeniHasta)
        {
            var mailKontrol = _context.TblHastalars.FirstOrDefault(x => x.Email == yeniHasta.Email);
            if (mailKontrol != null)
            {
                ViewBag.HataMesaji = "Bu e-posta adresi zaten kayıtlıdır!";
                ViewBag.Doktorlar = _context.TblDoktorlars.ToList(); 
                return View(yeniHasta);
            }

           
            if (yeniHasta.DoktorId == null || yeniHasta.DoktorId == 0)
            {
              
                var doktorHastaSayilari = _context.TblDoktorlars
                    .Select(d => new {
                        DoktorId = d.DoktorId,
                        HastaSayisi = _context.TblHastalars.Count(h => h.DoktorId == d.DoktorId)
                    }).ToList();

                if (doktorHastaSayilari.Any())
                {
                 
                    var minHastaSayisi = doktorHastaSayilari.Min(d => d.HastaSayisi);

               
                    var musaitDoktorlar = doktorHastaSayilari.Where(d => d.HastaSayisi == minHastaSayisi).ToList();

                    var rastgele = new Random();
                    int secilenIndex = rastgele.Next(musaitDoktorlar.Count);

                    yeniHasta.DoktorId = musaitDoktorlar[secilenIndex].DoktorId;
                }
            }

    
            yeniHasta.DoktorOnayDurumu = 0;
   

            if (!string.IsNullOrEmpty(yeniHasta.Telefon)) yeniHasta.Telefon = "+90" + yeniHasta.Telefon;

            var bugun = DateTime.Today;
            var yas = bugun.Year - yeniHasta.DogumTarihi.Year;
            if (yeniHasta.DogumTarihi.Date > bugun.AddYears(-yas)) yas--;
            yeniHasta.Yas = yas;

            try
            {
                _context.TblHastalars.Add(yeniHasta);
                _context.SaveChanges();
                TempData["BasariMesaji"] = "Kayıt işleminiz başarıyla tamamlandı! Lütfen giriş yapınız.";
                return RedirectToAction("GirisYap");
            }
            catch (Exception ex)
            {
                ViewBag.HataMesaji = "Bir hata oluştu: " + ex.Message;
                ViewBag.Doktorlar = _context.TblDoktorlars.ToList();
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

            ViewBag.Aktiviteler = _context.TblAktivitelers.OrderBy(x => x.AktiviteAdi).ToList();

            ViewBag.BugunkuAktiviteler = (from g in _context.TblHastaAktiviteGunlugus
                                          join a in _context.TblAktivitelers on g.AktiviteId equals a.AktiviteId
                                          where g.HastaId == id && g.Tarih.Date == bugun
                                          orderby g.Tarih descending
                                          select new AktiviteViewModel
                                          {
                                              AktiviteAdi = a.AktiviteAdi,
                                              SureDakika = g.SureDakika,
                                              YakilanKalori = g.YakilanKalori,
                                              Tarih = g.Tarih
                                          }).ToList();


            double alinanKalori = 0;
            foreach (var yg in bugunkuYemekler)
            {
                var malzemeler = (from r in _context.TblYemekMalzemeleris
                                  join m in _context.TblMalzemelers on r.MalzemeId equals m.MalzemeId
                                  where r.YemekId == yg.YemekId
                                  select new { r.MiktarGram, m.Kalori }).ToList();

                double yemekKalori = 0;
                foreach (var m in malzemeler)
                {
                    yemekKalori += (Convert.ToDouble(m.MiktarGram) / 100.0) * Convert.ToDouble(m.Kalori);
                }
                alinanKalori += yemekKalori * (yg.Porsiyon ?? 1.0);
            }
            ViewBag.AlinanKalori = Math.Round(alinanKalori);

            double yakilanKalori = 0;
            if (ViewBag.BugunkuAktiviteler != null)
            {
                var aktiviteListesi = ViewBag.BugunkuAktiviteler as List<AktiviteViewModel>;
                yakilanKalori = aktiviteListesi.Sum(x => x.YakilanKalori);
            }
            ViewBag.YakilanKalori = Math.Round(yakilanKalori);

            double hedefKalori = hasta.GunlukKaloriHedefi ?? 0;
            ViewBag.HedefKalori = hedefKalori;

            double kalanKalori = hedefKalori > 0 ? (hedefKalori - alinanKalori + yakilanKalori) : 0;
            ViewBag.KalanKalori = Math.Round(kalanKalori);

            var bugunkuSular = _context.TblSuGunlugus
                                       .Where(x => x.HastaId == id && x.Tarih.Date == bugun)
                                       .ToList();
            ViewBag.BugunSuMililitre = bugunkuSular.Sum(x => x.MiktarMililitre);

            ViewBag.YapayZekaOnerisi = YapayZekaOneriGetir(id);
            var kiloGecmisi = _context.TblKiloGecmisis
                                       .Where(x => x.HastaId == id)
                                       .AsEnumerable() 
                                       .GroupBy(x => x.TartilmaTarihi.Date)
                                       .Select(g => g.OrderByDescending(x => x.TartilmaTarihi).First())
                                       .OrderBy(x => x.TartilmaTarihi)
                                       .ToList();

            if (kiloGecmisi.Count >= 2)
            {
                var ilkTarih = kiloGecmisi.First().TartilmaTarihi;
                var sonTarih = kiloGecmisi.Last().TartilmaTarihi;

                if ((sonTarih - ilkTarih).TotalDays >= 1)
                {
                    try
                    {
                        var mlContext = new MLContext();

                        var egitimVerileri = kiloGecmisi.Select(x => new KiloVerisi
                        {
                            GecenGunSayisi = (float)(x.TartilmaTarihi - ilkTarih).TotalDays,
                            Kilo = (float)x.Kilo
                        }).ToList();

                        IDataView trainingData = mlContext.Data.LoadFromEnumerable(egitimVerileri);

                        var pipeline = mlContext.Transforms.Concatenate("Features", "GecenGunSayisi")
                            .Append(mlContext.Regression.Trainers.Ols(labelColumnName: "Kilo"));

                        var model = pipeline.Fit(trainingData);

                        var predictionEngine = mlContext.Model.CreatePredictionEngine<KiloVerisi, KiloTahmini>(model);

                        var sonKayitGunu = egitimVerileri.Last().GecenGunSayisi;
                        var gelecekVeri = new KiloVerisi { GecenGunSayisi = sonKayitGunu + 30f };

                        var tahmin = predictionEngine.Predict(gelecekVeri);

                        ViewBag.GelecekKiloTahmini = Math.Round(tahmin.BeklenenKilo, 1);
                        ViewBag.TahminMesaji = "30 Gün Sonraki Tahmini Kilonuz";

                        double beklenen = Math.Round(tahmin.BeklenenKilo, 1);
                        if (beklenen < 30 || beklenen > 300)
                        {
                            ViewBag.TahminMesaji = "Yapay zeka ivmeyi hesaplıyor, biraz daha veri girin.";
                            ViewBag.GelecekKiloTahmini = null;
                        }
                        else
                        {
                            ViewBag.GelecekKiloTahmini = beklenen;
                            ViewBag.TahminMesaji = "30 Gün Sonraki Tahmini Kilonuz";
                        }
                    }
                    catch (Exception)
                    {
                        ViewBag.TahminMesaji = "Yapay zeka analiz için daha fazla düzenli tartım verisi bekliyor.";
                    }
                }
                else
                {
                    ViewBag.TahminMesaji = "Yapay zeka analizi için farklı günlerde (en az 1 gün arayla) tartılmış olmanız gerekmektedir.";
                }
            }
            else
            {
                ViewBag.TahminMesaji = "Tahmin motorunun çalışması için en az 2 farklı kilo kaydı girmelisiniz.";
            }
            
            return View(hasta);
        }

        [HttpPost]
        public IActionResult AktiviteEkle(int AktiviteId, int SureDakika)
        {
            var hastaId = HttpContext.Session.GetInt32("HastaId");
            if (hastaId == null) return RedirectToAction("GirisYap");

            var hasta = _context.TblHastalars.Find(hastaId);
            var aktivite = _context.TblAktivitelers.Find(AktiviteId);

            if (hasta != null && aktivite != null && SureDakika > 0)
            {

                double anlikKilo = hasta.Kilo ?? 70.0;

                double yakilan = (aktivite.MetDegeri * anlikKilo * SureDakika) / 60.0;

                var yeniKayit = new TblHastaAktiviteGunlugu
                {
                    HastaId = hasta.HastaId,
                    AktiviteId = AktiviteId,
                    SureDakika = SureDakika,
                    YakilanKalori = Math.Round(yakilan, 2), 
                    Tarih = DateTime.Now
                };

                _context.TblHastaAktiviteGunlugus.Add(yeniKayit);
                _context.SaveChanges();
                TempData["Mesaj"] = $"Tebrikler! {SureDakika} dakika {aktivite.AktiviteAdi} yaparak tam {Math.Round(yakilan)} kalori yaktınız! 🔥";
            }
            return RedirectToAction("AnaPanel", new { id = hasta.HastaId });
        }

        [HttpPost]
        public IActionResult SuEkle(int miktar)
        {
            var hastaId = HttpContext.Session.GetInt32("HastaId");
            if (hastaId == null) return RedirectToAction("GirisYap");

            if (miktar > 0)
            {
                var yeniSu = new TblSuGunlugu
                {
                    HastaId = hastaId.Value,
                    MiktarMililitre = miktar,
                    Tarih = DateTime.Now
                };
                _context.TblSuGunlugus.Add(yeniSu);
                _context.SaveChanges();
                TempData["Mesaj"] = $"Harika! Sisteme {miktar} ml su eklendi. Vücudunu susuz bırakmadığın için tebrikler! 💧";
            }
            return RedirectToAction("AnaPanel", new { id = hastaId.Value });
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

                if (vki < 18.5) ViewBag.Durum = "Zayıf (Kilo Almalısınız)";
                else if (vki < 24.9) ViewBag.Durum = "Normal (Harika!)";
                else if (vki < 29.9) ViewBag.Durum = "Fazla Kilolu (Diyete Başlamalısınız)";
                else ViewBag.Durum = "Obezite Riski (Acil Diyetisyen Desteği)";
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
                ViewBag.HataMesaji = "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor!";
                return View(guncelHasta);
            }

            var bugun = DateTime.Today;
            var yas = bugun.Year - guncelHasta.DogumTarihi.Year;
            if (guncelHasta.DogumTarihi.Date > bugun.AddYears(-yas)) yas--;
            guncelHasta.Yas = yas;

            try
            {
                _context.TblHastalars.Update(guncelHasta);

                if (guncelHasta.Kilo != null && guncelHasta.Kilo > 0)
                {
                    var sonKilo = _context.TblKiloGecmisis
                                          .Where(x => x.HastaId == guncelHasta.HastaId)
                                          .OrderByDescending(x => x.TartilmaTarihi)
                                          .FirstOrDefault();

                    if (sonKilo == null || sonKilo.Kilo != guncelHasta.Kilo)
                    {
                        var yeniKiloKaydi = new TblKiloGecmisi
                        {
                            HastaId = guncelHasta.HastaId,
                            Kilo = guncelHasta.Kilo.Value,
                            TartilmaTarihi = DateTime.Now
                        };
                        _context.TblKiloGecmisis.Add(yeniKiloKaydi);
                    }
                }

                _context.SaveChanges();
                TempData["BasariMesaji"] = "Profil bilgileriniz başarıyla güncellendi!";
                return RedirectToAction("Profilim", new { id = guncelHasta.HastaId });
            }
            catch (Exception ex)
            {
                ViewBag.HataMesaji = "Güncelleme sırasında bir hata oluştu: " + ex.Message;
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

            if (hasta.ColyakMi == true) { tumYemekler = tumYemekler.Where(x => x.Kategori != "Börekler ve Pideler" && x.Kategori != "Tatlılar" && !x.YemekAdi.Contains("Makarna") && !x.YemekAdi.Contains("Bulgur") && !x.YemekAdi.Contains("Şehriye") && !x.YemekAdi.Contains("Erişte")); }
            if (hasta.DiyabetMi == true) { tumYemekler = tumYemekler.Where(x => x.Kategori != "Tatlılar" && !x.YemekAdi.Contains("Kızartma") && !x.YemekAdi.Contains("Pirinç")); }
            if (hasta.TansiyonHastasiMi == true) { tumYemekler = tumYemekler.Where(x => !x.YemekAdi.Contains("Turşu") && !x.YemekAdi.Contains("Pastırma")); }

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
            if (aId == 0) secilenAnaYemek = guvenliListe.Where(x => x.Kategori.Contains("Et") || x.Kategori.Contains("Tavuk") || x.Kategori.Contains("Balık")).OrderBy(x => rand.Next()).FirstOrDefault();
            else if (aId.HasValue && aId > 0) secilenAnaYemek = guvenliListe.FirstOrDefault(x => x.YemekId == aId.Value);
            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(sKeyA))) secilenAnaYemek = guvenliListe.FirstOrDefault(x => x.YemekId == int.Parse(HttpContext.Session.GetString(sKeyA)));
            if (secilenAnaYemek == null) secilenAnaYemek = guvenliListe.Where(x => x.Kategori.Contains("Et") || x.Kategori.Contains("Tavuk") || x.Kategori.Contains("Balık")).OrderBy(x => rand.Next()).FirstOrDefault();

            TblYemekler secilenTamamlayici = null;
            if (yId == 0) secilenTamamlayici = guvenliListe.Where(x => x.Kategori.Contains("Salatalar") || x.Kategori.Contains("Zeytinyağlılar")).OrderBy(x => rand.Next()).FirstOrDefault();
            else if (yId.HasValue && yId > 0) secilenTamamlayici = guvenliListe.FirstOrDefault(x => x.YemekId == yId.Value);
            else if (!string.IsNullOrEmpty(HttpContext.Session.GetString(sKeyY))) secilenTamamlayici = guvenliListe.FirstOrDefault(x => x.YemekId == int.Parse(HttpContext.Session.GetString(sKeyY)));
            if (secilenTamamlayici == null) secilenTamamlayici = guvenliListe.Where(x => x.Kategori.Contains("Salatalar") || x.Kategori.Contains("Zeytinyağlılar")).OrderBy(x => rand.Next()).FirstOrDefault();

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
            TempData["Mesaj"] = "Afiyet olsun! Bütün öğünlerin başarıyla kaydedildi.";
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

                TempData["Mesaj"] = "Öğün başarıyla silindi!";
            }

            return RedirectToAction("YemekGecmisi", new { id = hastaId });
        }


        [HttpGet]
        public IActionResult YemekGunluguDuzenle(int gunlukId)
        {
            var yemek = _context.TblYemekGunlugus.Find(gunlukId);
            if (yemek == null) return RedirectToAction("GirisYap");


            ViewBag.Yemekler = _context.TblYemeklers.ToList();
            return View(yemek);
        }


        [HttpPost]
        public IActionResult YemekGunluguDuzenle(KlinikBeslenmeApp.Models.TblYemekGunlugu guncelYemek)
        {
            var mevcutYemek = _context.TblYemekGunlugus.Find(guncelYemek.GunlukId);
            if (mevcutYemek != null)
            {

                mevcutYemek.YemekId = guncelYemek.YemekId;
                mevcutYemek.Porsiyon = guncelYemek.Porsiyon;
                mevcutYemek.OgunTipi = guncelYemek.OgunTipi;
                mevcutYemek.TuketimTarihi = guncelYemek.TuketimTarihi;
                mevcutYemek.Aciklama = guncelYemek.Aciklama;

                _context.SaveChanges();


                TempData["Mesaj"] = "Öğün başarıyla güncellendi!";

                return RedirectToAction("YemekGecmisi", new { id = mevcutYemek.HastaId });
            }
            return RedirectToAction("GirisYap");
        }

        private string YapayZekaOneriGetir(int hastaId)
        {
            var sonYemek = _context.TblYemekGunlugus
                .Where(x => x.HastaId == hastaId && x.TuketimTarihi.HasValue && x.TuketimTarihi.Value.Date == DateTime.Today)
                .OrderByDescending(x => x.GunlukId) 
                .FirstOrDefault();

            if (sonYemek == null) return "Bugün henüz bir öğün girmediniz. Sağlıklı bir öğün ekleyerek AKBİS önerilerinden faydalanabilirsiniz!";

            var ayniYemeginYendigiAdisyonlar = _context.TblYemekGunlugus
                .Where(x => x.YemekId == sonYemek.YemekId)
                .Select(x => x.KayitId)
                .Distinct() 
                .ToList();

            var onerilenYemekId = _context.TblYemekGunlugus
                .Where(x => ayniYemeginYendigiAdisyonlar.Contains(x.KayitId) && x.YemekId != sonYemek.YemekId)
                .GroupBy(x => x.YemekId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            if (onerilenYemekId == 0) return "Şu anki menünüz harika ve dengeli görünüyor, afiyet olsun!";

            var oneriYemekAd = _context.TblYemeklers.Find(onerilenYemekId)?.YemekAdi;
            var sonYemekAd = _context.TblYemeklers.Find(sonYemek.YemekId)?.YemekAdi;

            return $"AKBİS Karar Destek Sistemi Analizi: '{sonYemekAd}' tüketen hastalarımızın büyük bir kısmı, metabolizmayı dengelemek için bunun yanında '{oneriYemekAd}' de tercih etti. Diyetinize eklemeyi düşünebilirsiniz!";
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
    public class AktiviteViewModel
    {
        public string AktiviteAdi { get; set; }
        public int SureDakika { get; set; }
        public double YakilanKalori { get; set; }
        public DateTime Tarih { get; set; }
    }
}