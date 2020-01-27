using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlmaCepech.Models
{
    [Serializable]
    public class dtoNoticias : Alma_Noticia
    {
        public string NombreUsuario { get; set; }
    }
}