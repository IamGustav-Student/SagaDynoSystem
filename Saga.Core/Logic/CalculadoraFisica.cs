using Saga.Core.Models;
using System.Collections.Generic;

namespace Saga.Core.Logic
{
    public static class CalculadoraFisica
    {
        /// <summary>
        /// Convierte datos crudos (Hex convertidos) en datos físicos con Velocidad y Tiempo.
        /// </summary>
        /// <param name="datosCrudos">Lista que viene del driver</param>
        /// <param name="frecuenciaMuestreoHz">Puntos por segundo (ej: 100Hz)</param>
        public static List<PuntoEnsayo> ProcesarDatos(List<DatoCrudo> datosCrudos, double frecuenciaMuestreoHz)
        {
            var resultados = new List<PuntoEnsayo>();

            // Delta Tiempo (dt) es el tiempo entre cada punto.
            // Si leemos a 100Hz, cada punto está separado por 0.01 segundos.
            double dt = 1.0 / frecuenciaMuestreoHz;

            for (int i = 0; i < datosCrudos.Count; i++)
            {
                var actual = datosCrudos[i];

                double tiempoActual = i * dt;
                double velocidadCalculada = 0;

                // Para calcular velocidad necesitamos el punto anterior (Derivada)
                if (i > 0)
                {
                    var anterior = datosCrudos[i - 1];

                    double deltaPosicion = actual.PosicionRaw - anterior.PosicionRaw;

                    // v = dP / dt
                    velocidadCalculada = deltaPosicion / dt;
                }

                resultados.Add(new PuntoEnsayo
                {
                    Tiempo = tiempoActual,
                    Posicion = actual.PosicionRaw,
                    Fuerza = actual.FuerzaRaw,
                    Velocidad = velocidadCalculada
                });
            }

            return resultados;
        }
    }
}
