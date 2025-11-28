namespace Saga.Core.Models
{
    public class DatoCrudo
    {
        public double FuerzaRaw { get; set; }
        public double PosicionRaw { get; set; }
        public int Indice { get; set; }

        public override string ToString()
        {
            return $"#{Indice} | F: {FuerzaRaw:F2} | P: {PosicionRaw:F2}";
        }
    }
}
