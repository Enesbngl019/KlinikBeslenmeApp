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
    }
}