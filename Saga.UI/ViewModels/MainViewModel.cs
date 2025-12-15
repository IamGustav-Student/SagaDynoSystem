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
using Saga.Core.Logic;

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
            EnsayoEnCurso = true;
            MensajeEstado = "Iniciando secuencia...";
            LogData = "";

            try
            {
                await _driver.HabilitarMaquinaAsync();
                await _driver.ConfigurarAdquisicionAsync(100);

                MensajeEstado = "Motor ON...";
                await _driver.EncenderMotorAsync(5.0); // Frecuencia del motor

                MensajeEstado = "Adquiriendo datos crudos...";
                var datosCrudos = await _driver.LeerDatosMuestreoAsync();

                MensajeEstado = "Procesando física (Calculando velocidad)...";

                // --- NUEVO: CÁLCULO DE VELOCIDAD ---
                // Asumimos una tasa de muestreo fija por ahora (ej: 50Hz) 
                // En el futuro esto vendrá de la configuración real de la máquina
                double sampleRate = 50.0;
                var datosProcesados = CalculadoraFisica.ProcesarDatos(datosCrudos, sampleRate);
                // -----------------------------------

                MensajeEstado = $"Finalizado. {datosProcesados.Count} puntos procesados.";

                // Logueamos datos completos (F, P, V)
                foreach (var p in datosProcesados)
                {
                    LogData += $"T:{p.Tiempo:F2}s | P:{p.Posicion:F1}mm | F:{p.Fuerza:F1}kg | V:{p.Velocidad:F1}mm/s\n";
                }

                if (datosProcesados.Count > 0)
                {
                    // Enviamos datos PROCESADOS
                    WeakReferenceMessenger.Default.Send(new UpdateGraphMessage(datosProcesados));
                }

                await _driver.DetenerMotorAsync();
            }
            catch (System.Exception ex)
            {
                MensajeEstado = $"Error: {ex.Message}";
                await _driver.DetenerMotorAsync();
            }
            finally
            {
                EnsayoEnCurso = false;
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