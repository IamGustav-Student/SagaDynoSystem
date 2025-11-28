using System.Collections.Generic;
using System.Threading.Tasks;
using Saga.Core.Models;

namespace Saga.Core.Interfaces
{
    public interface IDynoDriver
    {
        bool EstaConectado { get; }
        void Conectar(string puertoCom);
        void Desconectar();

        // --- ESTA LINEA ES LA QUE FALTABA ---
        string[] ObtenerPuertosDisponibles();

        // Control Máquina
        Task HabilitarMaquinaAsync();
        Task DeshabilitarMaquinaAsync();
        Task EncenderMotorAsync(double velocidadHz);
        Task DetenerMotorAsync();

        // Adquisición de Buffer
        Task ConfigurarAdquisicionAsync(int cantidadPuntos);
        Task<List<DatoCrudo>> LeerDatosMuestreoAsync();

        // Lectura de Sensores
        Task<string> LeerSensoresAsync();
    }
}
