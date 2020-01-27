using AlmaCepech.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace AlmaCepech.Controllers
{
    public class LoginController : Controller
    {
        private Fichas20Entities _db = new Fichas20Entities();
        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult LogOn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var Busqueda = _db.Usuarios.Where(x => x.Username.ToLower() == model.UserName.ToLower()).ToList();
                if (Busqueda.Count() == 0)
                {
                    ViewBag.Message = "El usuario no existe!";
                }
                else
                {
                    var oSalida = Busqueda.Where(x => x.Username.ToLower() == model.UserName.ToLower() && x.password.ToLower() == model.Password.ToLower()).FirstOrDefault();
                    //if (Membership.ValidateUser(model.UserName, model.Password))
                    if (oSalida != null)
                    {
                        FormsAuthentication.SetAuthCookie(oSalida.Nombre_Completo, model.RememberMe);

                        Session["ID_Usuario"] = oSalida.ID_Usuario;
                        Session["Username"] = oSalida.Username;

                        if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/"))
                            return Redirect(returnUrl);
                        else
                            return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Message = "El usuario/Password dado no coincide con nuestros registros!";
                    }
                }
            }
            // if we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }
    }
}