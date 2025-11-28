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

        Task HabilitarMaquinaAsync();
        Task DeshabilitarMaquinaAsync();

        Task EncenderMotorAsync(double velocidadHz);
        Task DetenerMotorAsync();

        // Paso crítico que agregamos
        Task ConfigurarAdquisicionAsync(int cantidadPuntos);

        // Lectura devuelve Lista, no string
        Task<List<DatoCrudo>> LeerDatosMuestreoAsync();

        Task<string> LeerSensoresAsync();
    }
}
