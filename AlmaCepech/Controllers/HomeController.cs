using AlmaCepech.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AlmaCepech.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private Fichas20Entities _db = new Fichas20Entities();
        public ActionResult Index()
        {
            if (Session["ID_Usuario"] == null)
                return RedirectToAction("LogOn", "Login");
            ViewBag.Data = _db.Alma_Noticia.OrderByDescending(x => x.Fecha).Take(20).ToList();
            return View();
        }

        public ActionResult agregar(int id = 0)
        {
            Alma_Noticia o = new Alma_Noticia();
            if (id != 0)
                o = _db.Alma_Noticia.Find(id);
            else
            {
                o.Fecha = DateTime.Now;
                o.IdUsuario = Convert.ToInt32(Session["ID_Usuario"]);
            }
            return View(o);
        }

        [HttpPost]
        public ActionResult comentario(int id = 0)
        {
            ViewBag.Data = _db.Alma_Noticia.Find(id).Alma_Comentario.ToList().OrderByDescending(x => x.Fecha).Take(3).ToList();
            ViewBag.id = id;
            return PartialView();
        }

        [HttpPost]
        public JsonResult GrabarComentario(int idpost, string comentario, int idusuario)
        {
            _db.Alma_Comentario.Add(new Alma_Comentario() { IdPost = idpost, Comentario = comentario, IdUsuario = idusuario, Fecha = DateTime.Now });
            _db.SaveChanges();
            return Json("OK");
        }

        [HttpPost]
        public JsonResult SaveLike(int id, int idusuario)
        {
            var Salida = _db.Alma_Aprobacion.FirstOrDefault(x => x.IdUsuario == idusuario && x.IdPost == id);
            if (Salida == null)
            {
                _db.Alma_Aprobacion.Add(new Alma_Aprobacion() { IdPost = id, IdUsuario = idusuario, Fecha = DateTime.Now, Gusta = 1 });
                _db.SaveChanges();
                return Json(string.Format("OK|{0}", getNombreUsuario(idusuario)));
            }
            else
                return Json("NO");
        }

        [HttpPost]
        public JsonResult eliminarImagen(int id)
        {
            try
            {
                var imagen = _db.Alma_Imagen.Find(id);
                _db.Alma_Imagen.Remove(imagen);
                _db.SaveChanges();
                return Json("OK");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public JsonResult eliminarComentario(int id)
        {
            try
            {
                var comentario = _db.Alma_Comentario.Find(id);
                _db.Alma_Comentario.Remove(comentario);
                _db.SaveChanges();
                return Json("OK");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        public String getNombreUsuario(int IdUsuario = 0)
        {
            return _db.Usuarios.Find(IdUsuario).Nombre_Completo;
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var noticia = _db.Alma_Noticia.Find(id);
                _db.Alma_Noticia.Remove(noticia);
                _db.SaveChanges();
                return Json("OK");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult agregar(Alma_Noticia obj, IEnumerable<HttpPostedFileBase> files)
        {
            ViewBag.error = "";
            try
            {
                if (obj.Titulo == "")
                    throw new ArgumentException("Debes ingresar un titulo a la noticia");
                if (obj.IdPost == 0 && files.Count() == 0)
                    throw new ArgumentException("Debes ingresar al menos una photo!");
                if (ModelState.IsValid)
                {
                    if (obj.IdPost == 0) //solo insertamos si es nuevo
                        _db.Alma_Noticia.Add(obj);
                    else
                    {
                        _db.Entry(obj).State = System.Data.Entity.EntityState.Modified;
                    }
                    _db.SaveChanges();

                    var idpost = obj.IdPost;
                    if (files.Where(x => x != null).Count() > 0)
                    {
                        foreach (var item in files)
                        {
                            byte[] data;
                            using (Stream inputStream = item.InputStream)
                            {
                                MemoryStream memoryStream = inputStream as MemoryStream;
                                if (memoryStream == null)
                                {
                                    memoryStream = new MemoryStream();
                                    inputStream.CopyTo(memoryStream);
                                }
                                data = memoryStream.ToArray();
                            }
                            Alma_Imagen i = new Alma_Imagen();
                            i.IdPost = idpost;
                            i.ContentType = item.ContentType;
                            i.NombreImagen = item.FileName;
                            i.Contenido = CreateThumbnail(data, 400);
                            _db.Alma_Imagen.Add(i);
                            _db.SaveChanges();
                        }
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.error = "Modelo inválido";
                    return View(obj);
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return View(obj);
            }

        }


        #region Util
        public static byte[] CreateThumbnail(byte[] PassedImage, int LargestSide)
        {
            byte[] ReturnedThumbnail;

            using (MemoryStream StartMemoryStream = new MemoryStream(),
                                NewMemoryStream = new MemoryStream())
            {
                // write the string to the stream  
                StartMemoryStream.Write(PassedImage, 0, PassedImage.Length);

                // create the start Bitmap from the MemoryStream that contains the image  
                Bitmap startBitmap = new Bitmap(StartMemoryStream);

                // set thumbnail height and width proportional to the original image.  
                int newHeight;
                int newWidth;
                double HW_ratio;
                if (startBitmap.Height > startBitmap.Width)
                {
                    newHeight = LargestSide;
                    HW_ratio = (double)((double)LargestSide / (double)startBitmap.Height);
                    newWidth = (int)(HW_ratio * (double)startBitmap.Width);
                }
                else
                {
                    newWidth = LargestSide;
                    HW_ratio = (double)((double)LargestSide / (double)startBitmap.Width);
                    newHeight = (int)(HW_ratio * (double)startBitmap.Height);
                }

                // create a new Bitmap with dimensions for the thumbnail.  
                Bitmap newBitmap = new Bitmap(newWidth, newHeight);

                // Copy the image from the START Bitmap into the NEW Bitmap.  
                // This will create a thumnail size of the same image.  
                newBitmap = ResizeImage(startBitmap, newWidth, newHeight);

                // Save this image to the specified stream in the specified format.  
                newBitmap.Save(NewMemoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                // Fill the byte[] for the thumbnail from the new MemoryStream.  
                ReturnedThumbnail = NewMemoryStream.ToArray();
            }

            // return the resized image as a string of bytes.  
            return ReturnedThumbnail;
        }

        // Resize a Bitmap  
        private static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(width, height);
            using (Graphics gfx = Graphics.FromImage(resizedImage))
            {
                gfx.DrawImage(image, new Rectangle(0, 0, width, height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            }
            return resizedImage;
        }
        #endregion
    }
}