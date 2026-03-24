using Microsoft.AspNetCore.Mvc;
using KlinikBeslenmeApp.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace KlinikBeslenmeApp.Controllers
{
    public class DoktorController : Controller
    {
        KlinikBeslenmeDbContext _context = new KlinikBeslenmeDbContext();

  
        [HttpGet]
        public IActionResult GirisYap()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GirisYap(string Email, string Sifre)
        {
            var doktor = _context.TblDoktorlars.FirstOrDefault(x => x.Email == Email && x.Sifre == Sifre);

            if (doktor != null)
            {
  
                HttpContext.Session.SetInt32("DoktorId", doktor.DoktorId);
                HttpContext.Session.SetString("DoktorAd", doktor.AdSoyad);


                if (doktor.IlkGirisMi == true)
                {
                    TempData["Uyari"] = "Güvenliğiniz için lütfen sistemin verdiği geçici şifreyi değiştirin.";
                    return RedirectToAction("SifreDegistir"); 
                }


                TempData["Mesaj"] = "Yönetim Paneline Hoş Geldiniz!";
                return RedirectToAction("AdminPanel");
            }

            ViewBag.Hata = "Hatalı e-posta veya şifre girdiniz!";
            return View();
        }


        [HttpGet]
        public IActionResult SifreDegistir()
        {

            if (HttpContext.Session.GetInt32("DoktorId") == null) return RedirectToAction("GirisYap");
            return View();
        }

        [HttpPost]
        public IActionResult SifreDegistir(string YeniSifre, string YeniSifreTekrar)
        {
            var doktorId = HttpContext.Session.GetInt32("DoktorId");
            if (doktorId == null) return RedirectToAction("GirisYap");

            if (YeniSifre != YeniSifreTekrar)
            {
                ViewBag.Hata = "Şifreler birbiriyle uyuşmuyor, lütfen tekrar deneyin!";
                return View();
            }

            var doktor = _context.TblDoktorlars.Find(doktorId);
            if (doktor != null)
            {

                doktor.Sifre = YeniSifre;
                doktor.IlkGirisMi = false;
                _context.SaveChanges();

                TempData["Mesaj"] = "Şifreniz başarıyla güncellendi! Artık sistemi güvenle kullanabilirsiniz.";
                return RedirectToAction("AdminPanel");
            }

            return View();
        }

        public IActionResult AdminPanel()
        {
            var doktorId = HttpContext.Session.GetInt32("DoktorId");
            if (doktorId == null) return RedirectToAction("GirisYap");


            ViewBag.OnayBekleyenler = _context.TblHastalars
                .Where(x => x.DoktorId == doktorId && x.DoktorOnayDurumu == 0)
                .OrderByDescending(x => x.HastaId)
                .ToList();


            var mevcutHastalar = _context.TblHastalars
                .Where(x => x.DoktorId == doktorId && x.DoktorOnayDurumu == 1)
                .OrderByDescending(x => x.HastaId)
                .ToList();

            return View(mevcutHastalar); 
        }

        [HttpPost]
        public IActionResult HastaOnayla(int hastaId)
        {
            var hasta = _context.TblHastalars.Find(hastaId);
            if (hasta != null)
            {
                hasta.DoktorOnayDurumu = 1; 
                _context.SaveChanges();
                TempData["Mesaj"] = $"{hasta.Ad} {hasta.Soyad} isimli hastayı başarıyla kabul ettiniz!";
            }
            return RedirectToAction("AdminPanel");
        }


        [HttpPost]
        public IActionResult HastaReddet(int hastaId)
        {
            var hasta = _context.TblHastalars.Find(hastaId);
            if (hasta != null)
            {
                hasta.DoktorId = null;
                hasta.DoktorOnayDurumu = 0;
                _context.SaveChanges();
                TempData["Uyari"] = $"{hasta.Ad} {hasta.Soyad} isimli hastanın talebi reddedildi. Hasta genel havuza geri gönderildi.";
            }
            return RedirectToAction("AdminPanel");
        }


        public IActionResult CikisYap()
        {
            HttpContext.Session.Remove("DoktorId");
            HttpContext.Session.Remove("DoktorAd");
            return RedirectToAction("GirisYap");
        }

        [HttpGet]
        public IActionResult HastaDetay(int id, DateTime? filtreTarih = null)
        {
            var doktorId = HttpContext.Session.GetInt32("DoktorId");
            if (doktorId == null) return RedirectToAction("GirisYap");


            var hasta = _context.TblHastalars.FirstOrDefault(x => x.HastaId == id && x.DoktorId == doktorId && x.DoktorOnayDurumu == 1);
            if (hasta == null) return RedirectToAction("AdminPanel");

            ViewBag.HastaAd = $"{hasta.Ad} {hasta.Soyad}";
            ViewBag.HastaId = hasta.HastaId;

            DateTime seciliTarih = filtreTarih ?? DateTime.Today;
            ViewBag.SeciliTarih = seciliTarih.ToString("yyyy-MM-dd");

            var gecmis = (from g in _context.TblYemekGunlugus
                          join y in _context.TblYemeklers on g.YemekId equals y.YemekId
                          where g.HastaId == id && g.TuketimTarihi != null && g.TuketimTarihi.Value.Date == seciliTarih.Date
                          orderby g.TuketimTarihi descending
                          select new DoktorHastaDetayViewModel
                          {
                              YemekAdi = y.YemekAdi,
                              OgunTipi = g.OgunTipi,
                              TuketimTarihi = g.TuketimTarihi.Value,
                              Porsiyon = g.Porsiyon ?? 1.0,
                              Aciklama = g.Aciklama
                          }).ToList();
            ViewBag.GuncelKilo = hasta.Kilo;
            ViewBag.GuncelBoy = hasta.Boy;
            ViewBag.DoktorNotu = hasta.DoktorNotu;
            ViewBag.KiloGecmisi = _context.TblKiloGecmisis
                                          .Where(x => x.HastaId == id)
                                          .OrderByDescending(x => x.TartilmaTarihi)
                                          .ToList();
            return View(gecmis);
        }
        [HttpPost]
        public IActionResult DoktorNotuKaydet(int HastaId, string DoktorNotu)
        {
            var doktorId = HttpContext.Session.GetInt32("DoktorId");
            if (doktorId == null) return RedirectToAction("GirisYap");

            var hasta = _context.TblHastalars.FirstOrDefault(x => x.HastaId == HastaId && x.DoktorId == doktorId);
            if (hasta != null)
            {
                hasta.DoktorNotu = DoktorNotu; 
                _context.SaveChanges();
                TempData["BasariMesaji"] = "Hastaya özel diyet programı ve notlar başarıyla kaydedildi!";
            }
            return RedirectToAction("HastaDetay", new { id = HastaId });
        }
    }
    public class DoktorHastaDetayViewModel
    {
        public string YemekAdi { get; set; }
        public string OgunTipi { get; set; }
        public DateTime TuketimTarihi { get; set; }
        public double Porsiyon { get; set; }
        public string Aciklama { get; set; }
    }
}