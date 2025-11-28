using System;
using System.Collections.Generic;

namespace Saga.Core.Models
{
    public class Ensayo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string Titulo { get; set; } = string.Empty;
        public string Comentarios { get; set; } = string.Empty;

        // Datos numéricos
        public List<DatoCrudo> Puntos { get; set; } = new List<DatoCrudo>();
    }
}
