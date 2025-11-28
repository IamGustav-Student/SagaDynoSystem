using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Saga.Core.Interfaces;
using Saga.Core.Models;
using Saga.Infrastructure.Driver;
using System.Threading.Tasks;
using System.Windows;

namespace Saga.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDynoDriver _driver;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConectarCommand))]
        [NotifyCanExecuteChangedFor(nameof(DesconectarCommand))]
        [NotifyCanExecuteChangedFor(nameof(IniciarEnsayoCommand))]
        private bool _estaConectado;

        [ObservableProperty]
        private string _puertoSeleccionado = "COM6"; // Ajusta a tu puerto real

        [ObservableProperty]
        private string _mensajeEstado = "Sistema listo.";

        [ObservableProperty]
        private string _logData = "";

        public MainViewModel()
        {
            _driver = new SerialDynoDriver();
        }

        [RelayCommand(CanExecute = nameof(PuedeConectar))]
        private void Conectar()
        {
            try
            {
                _driver.Conectar(PuertoSeleccionado);
                EstaConectado = true;
                MensajeEstado = "Conectado.";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool PuedeConectar() => !EstaConectado;

        [RelayCommand(CanExecute = nameof(EstaConectado))]
        private void Desconectar()
        {
            _driver.Desconectar();
            EstaConectado = false;
            MensajeEstado = "Desconectado.";
        }

        [RelayCommand(CanExecute = nameof(EstaConectado))]
        private async Task IniciarEnsayo()
        {
            MensajeEstado = "Iniciando...";
            LogData = "";

            try
            {
                // 1. Habilitar
                await _driver.HabilitarMaquinaAsync();

                // 2. Configurar Cantidad (IMPORTANTE: paso nuevo)
                int puntos = 100;
                MensajeEstado = $"Configurando {puntos} puntos...";
                await _driver.ConfigurarAdquisicionAsync(puntos);

                // 3. Motor
                MensajeEstado = "Encendiendo motor...";
                await _driver.EncenderMotorAsync(5.0);

                // 4. Leer
                MensajeEstado = "Adquiriendo...";
                var datos = await _driver.LeerDatosMuestreoAsync();

                MensajeEstado = $"Fin. {datos.Count} datos.";

                foreach (var p in datos)
                {
                    LogData += p.ToString() + "\n";
                }
            }
            catch (System.Exception ex)
            {
                MensajeEstado = $"Error: {ex.Message}";
                await _driver.DetenerMotorAsync();
            }
        }
    }
}