namespace Saga.Core
{
    public static class SagaProtocol
    {
        public const string HabilitarEquipo = ":C00DAZ";
        public const string DeshabilitarEquipo = ":C00DHZ";
        public const string EncenderMotorHeader = ":C15D";
        public const string DetenerMotor = ":C16Z";

        // Adquisición
        public const string ConfigurarMuestreo = ":C17D";
        public const string IniciarBuffer = ":C18Z";
        public const string SolicitarPaquete = "Q";

        public const string RespuestaOK = ":C99Z";
        public const string RespuestaError = ":C88Z";
        public const string Terminador = "Z";
        public const int TamañoPaqueteQ = 118;
    }
}