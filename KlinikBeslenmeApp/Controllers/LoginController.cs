using Microsoft.AspNetCore.Mvc;

namespace KlinikBeslenmeApp.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Yonlendir(string rol)
        {
            if (rol == "Hasta")
                return RedirectToAction("Index", "Home"); 
            else if (rol == "Doktor")
                return RedirectToAction("Index", "Doktor"); 
            else if (rol == "Admin")
                return RedirectToAction("Index", "Admin"); 

            return RedirectToAction("Index");
        }
    }
}
