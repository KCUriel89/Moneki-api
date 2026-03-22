using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//app
namespace Proyecto_servicio.Helpers
{
    public static class UserSession
    {
        public static int IdUsuario { get; set; }
        public static string Email { get; set; }
        public static string Rol { get; set; } // Usuario, Admin, Trabajador

        public static bool IsLoggedIn => IdUsuario > 0;

        public static void Clear()
        {
            IdUsuario = 0;
            Email = null;
            
        }
    }

}
