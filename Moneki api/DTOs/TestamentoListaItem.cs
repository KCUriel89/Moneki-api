using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_servicio.DTOs
{
    public class TestamentoListaItem
    {
        public int IdTramite { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
    }

}
