using Saga.Core.Models;
using System.Collections.Generic;

namespace Saga.UI.Messages
{
    // Este es un "paquete" que enviaremos desde el ViewModel a la Ventana
    // Contiene los puntos que queremos dibujar.
    public class UpdateGraphMessage
    {
        public List<DatoCrudo> Puntos { get; }

        public UpdateGraphMessage(List<DatoCrudo> puntos)
        {
            Puntos = puntos;
        }
    }
}
