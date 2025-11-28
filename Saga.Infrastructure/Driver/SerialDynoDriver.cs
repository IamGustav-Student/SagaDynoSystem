using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using Saga.Core;
using Saga.Core.Interfaces;
using Saga.Core.Models;

namespace Saga.Infrastructure.Driver
{
    public class SerialDynoDriver : IDynoDriver
    {
        private SerialPort _serialPort;
        private const int DefaultBaudRate = 57600;

        public bool EstaConectado => _serialPort != null && _serialPort.IsOpen;

        public SerialDynoDriver()
        {
            _serialPort = new SerialPort();
        }

        // --- IMPLEMENTACIÓN DEL MÉTODO NUEVO ---
        public string[] ObtenerPuertosDisponibles()
        {
            return SerialPort.GetPortNames();
        }

        public void Conectar(string puertoCom)
        {
            if (EstaConectado) _serialPort.Close();

            _serialPort.PortName = puertoCom;
            _serialPort.BaudRate = DefaultBaudRate;
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 1000;
            _serialPort.WriteTimeout = 500;

            try
            {
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                throw new Exception($"No se pudo abrir {puertoCom}. {ex.Message}");
            }
        }

        public void Desconectar()
        {
            if (EstaConectado)
            {
                try { _serialPort.Write(SagaProtocol.DeshabilitarEquipo); } catch { }
                _serialPort.Close();
            }
        }

        public async Task HabilitarMaquinaAsync()
        {
            await EnviarComandoVerificadoAsync(SagaProtocol.HabilitarEquipo);
        }

        public async Task DeshabilitarMaquinaAsync()
        {
            await EnviarComandoVerificadoAsync(SagaProtocol.DeshabilitarEquipo);
        }

        public async Task EncenderMotorAsync(double velocidadHz)
        {
            int valorEntero = (int)(velocidadHz * 10);
            string valorHex = valorEntero.ToString("X2");
            string comando = $"{SagaProtocol.EncenderMotorHeader}{valorHex}{SagaProtocol.Terminador}";

            await EnviarComandoVerificadoAsync(comando);
        }

        public async Task DetenerMotorAsync()
        {
            if (EstaConectado)
            {
                _serialPort.Write(SagaProtocol.DetenerMotor);
                await Task.Delay(50);
            }
        }

        public async Task ConfigurarAdquisicionAsync(int cantidadPuntos)
        {
            string hexCantidad = cantidadPuntos.ToString("X4");
            string comando = $"{SagaProtocol.ConfigurarMuestreo}{hexCantidad}{SagaProtocol.Terminador}";
            await EnviarComandoVerificadoAsync(comando);
        }

        public async Task<string> LeerSensoresAsync()
        {
            if (!EstaConectado) return string.Empty;
            _serialPort.DiscardInBuffer();
            _serialPort.Write(SagaProtocol.LeerSensores);
            await Task.Delay(100);
            return _serialPort.ReadExisting();
        }

        public async Task<List<DatoCrudo>> LeerDatosMuestreoAsync()
        {
            if (!EstaConectado) return new List<DatoCrudo>();

            await EnviarComandoVerificadoAsync(SagaProtocol.IniciarBuffer);
            await Task.Delay(100);

            _serialPort.Write(SagaProtocol.SolicitarPaquete);

            byte[] buffer = new byte[SagaProtocol.TamañoPaqueteQ];
            int leidos = 0;
            int intentos = 0;

            while (leidos < 10 && intentos < 20)
            {
                await Task.Delay(50);
                if (_serialPort.BytesToRead > 0)
                {
                    int aLeer = Math.Min(_serialPort.BytesToRead, SagaProtocol.TamañoPaqueteQ - leidos);
                    _serialPort.Read(buffer, leidos, aLeer);
                    leidos += aLeer;
                }
                intentos++;
            }

            string tramaRecibida = Encoding.ASCII.GetString(buffer, 0, leidos);
            return DecodificarPaquete(tramaRecibida);
        }

        private async Task EnviarComandoVerificadoAsync(string comando)
        {
            if (!EstaConectado) throw new Exception("Desconectado");
            _serialPort.DiscardInBuffer();
            _serialPort.Write(comando);
            await Task.Delay(150);
        }

        private List<DatoCrudo> DecodificarPaquete(string tramaHex)
        {
            var lista = new List<DatoCrudo>();
            tramaHex = tramaHex.Replace("\r", "").Replace("\n", "").Trim();
            int longitudPunto = 7;
            int cantidad = tramaHex.Length / longitudPunto;

            for (int i = 0; i < cantidad; i++)
            {
                try
                {
                    int start = i * longitudPunto;
                    string hFuerza = tramaHex.Substring(start, 4);
                    string hPos = tramaHex.Substring(start + 4, 3);

                    lista.Add(new DatoCrudo
                    {
                        Indice = i,
                        FuerzaRaw = Convert.ToInt32(hFuerza, 16) * 0.1,
                        PosicionRaw = Convert.ToInt32(hPos, 16)
                    });
                }
                catch { }
            }
            return lista;
        }
    }
}
