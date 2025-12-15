namespace Saga.Core.Models
{
    public class PuntoEnsayo
    {
        public double Tiempo { get; set; }       // Segundos
        public double Posicion { get; set; }     // mm
        public double Fuerza { get; set; }       // kg
        public double Velocidad { get; set; }    // mm/s (Calculada)

        // Constructor vacío
        public PuntoEnsayo() { }

        public PuntoEnsayo(double t, double p, double f, double v)
        {
            Tiempo = t;
            Posicion = p;
            Fuerza = f;
            Velocidad = v;
        }
    }
}