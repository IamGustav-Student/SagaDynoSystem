using Saga.Core.Models;
using System.Collections.Generic;

namespace Saga.UI.Messages
{
    public class UpdateGraphMessage
    {
        // Cambiamos List<DatoCrudo> por List<PuntoEnsayo>
        public List<PuntoEnsayo> Puntos { get; }

        public UpdateGraphMessage(List<PuntoEnsayo> puntos)
        {
            Puntos = puntos;
        }
    }
}