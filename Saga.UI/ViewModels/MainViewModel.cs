using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Saga.Core.Interfaces;
using Saga.Infrastructure.Driver;
using Saga.UI.Messages;
using System.Collections.ObjectModel; // Necesario para la lista de puertos
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Saga.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDynoDriver _driver;

        // --- ESTADO DE CONEXIÓN ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConectarCommand))]
        [NotifyCanExecuteChangedFor(nameof(DesconectarCommand))]
        [NotifyCanExecuteChangedFor(nameof(IniciarEnsayoCommand))]
        private bool _estaConectado;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(IniciarEnsayoCommand))] // No iniciar si ya está corriendo
        private bool _ensayoEnCurso;

        // --- PUERTOS COM ---
        // Usamos ObservableCollection para que el ComboBox se actualice solo
        public ObservableCollection<string> PuertosDisponibles { get; } = new ObservableCollection<string>();

        [ObservableProperty]
        private string _puertoSeleccionado;

        // --- MENSAJES Y DATOS ---
        [ObservableProperty]
        private string _mensajeEstado = "Sistema listo.";

        [ObservableProperty]
        private string _logData = "";

        public MainViewModel()
        {
            _driver = new SerialDynoDriver();
            // Buscar puertos al arrancar
            RefrescarPuertos();
        }

        // --- COMANDO: REFRESCAR PUERTOS ---
        [RelayCommand]
        private void RefrescarPuertos()
        {
            PuertosDisponibles.Clear();
            var puertos = _driver.ObtenerPuertosDisponibles();

            foreach (var p in puertos)
            {
                PuertosDisponibles.Add(p);
            }

            // Seleccionar el primero si hay alguno
            if (PuertosDisponibles.Count > 0)
                PuertoSeleccionado = PuertosDisponibles.Last(); // Normalmente el último es el recién conectado
            else
                MensajeEstado = "No se detectaron puertos COM.";
        }

        // --- COMANDO: CONECTAR ---
        [RelayCommand(CanExecute = nameof(PuedeConectar))]
        private void Conectar()
        {
            if (string.IsNullOrEmpty(PuertoSeleccionado))
            {
                MessageBox.Show("Selecciona un puerto COM primero.");
                return;
            }

            try
            {
                _driver.Conectar(PuertoSeleccionado);
                EstaConectado = true;
                MensajeEstado = $"Conectado a {PuertoSeleccionado}.";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private bool PuedeConectar() => !EstaConectado;

        // --- COMANDO: DESCONECTAR ---
        [RelayCommand(CanExecute = nameof(EstaConectado))]
        private void Desconectar()
        {
            try { _driver.Desconectar(); } catch { }
            EstaConectado = false;
            MensajeEstado = "Desconectado.";
        }

        // --- COMANDO: INICIAR PRUEBA ---
        private bool PuedeIniciar() => EstaConectado && !EnsayoEnCurso;

        [RelayCommand(CanExecute = nameof(PuedeIniciar))]
        private async Task IniciarEnsayo()
        {
            EnsayoEnCurso = true; // Bloquea el botón Iniciar y Activa Detener
            MensajeEstado = "Iniciando secuencia...";
            LogData = "";

            try
            {
                await _driver.HabilitarMaquinaAsync();

                // Configurar cantidad
                await _driver.ConfigurarAdquisicionAsync(100);

                MensajeEstado = "Motor ON...";
                await _driver.EncenderMotorAsync(5.0);

                MensajeEstado = "Adquiriendo...";

                // La lectura puede tardar. Si el usuario da DETENER, esto debe manejarse.
                var datos = await _driver.LeerDatosMuestreoAsync();

                MensajeEstado = $"Finalizado. {datos.Count} datos.";

                // Graficar y Log
                foreach (var p in datos) LogData += p.ToString() + "\n";
                if (datos.Count > 0)
                {
                    WeakReferenceMessenger.Default.Send(new UpdateGraphMessage(datos));
                }

                await _driver.DetenerMotorAsync();
            }
            catch (System.Exception ex)
            {
                MensajeEstado = $"Interrumpido: {ex.Message}";
                await _driver.DetenerMotorAsync(); // Seguridad
            }
            finally
            {
                EnsayoEnCurso = false; // Reactiva controles
            }
        }

        // --- COMANDO: DETENER (EMERGENCIA) ---
        // Este comando siempre está disponible si hay ensayo en curso
        [RelayCommand]
        private async Task DetenerPrueba()
        {
            if (EstaConectado)
            {
                MensajeEstado = "¡DETENIENDO MOTOR!";
                await _driver.DetenerMotorAsync();
                // Al detener el motor, la lectura de datos fallará o vendrá vacía, 
                // y el flujo de IniciarEnsayo caerá en el 'catch' o terminará.
            }
        }
    }
}