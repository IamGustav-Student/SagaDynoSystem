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

        public void Conectar(string puertoCom)
        {
            if (EstaConectado) _serialPort.Close();

            _serialPort.PortName = puertoCom;
            _serialPort.BaudRate = DefaultBaudRate;
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            try
            {
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
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
            await EnviarComandoVerificadoAsync(SagaProtocol.DetenerMotor);
        }

        public async Task ConfigurarAdquisicionAsync(int cantidadPuntos)
        {
            string hexCantidad = cantidadPuntos.ToString("X4");
            string comando = $"{SagaProtocol.ConfigurarMuestreo}{hexCantidad}{SagaProtocol.Terminador}";
            await EnviarComandoVerificadoAsync(comando);
        }

        // Implementación real basada en frmVisor.frm (Timer tmrPide)
        public async Task<string> LeerSensoresAsync()
        {
            if (!EstaConectado) return string.Empty;

            // VB6: .Output = ":C1AZ"
            // Este comando pide valores instantáneos (no de buffer)
            _serialPort.DiscardInBuffer();
            _serialPort.Write(":C1AZ");

            // Esperamos un tiempo prudencial para que responda (VB6 usaba 0.1s)
            await Task.Delay(100);

            // Leemos la respuesta cruda
            // La respuesta esperada tiene el formato: ...:C1BDFFFFPPPP...
            // Donde FFFF es fuerza y PPPP es posición en Hex
            string respuesta = _serialPort.ReadExisting();
            return respuesta;
        }

        public async Task<List<DatoCrudo>> LeerDatosMuestreoAsync()
        {
            if (!EstaConectado) return new List<DatoCrudo>();

            // 1. Iniciar Buffer
            await EnviarComandoVerificadoAsync(SagaProtocol.IniciarBuffer);
            await Task.Delay(100);

            // 2. Pedir Datos
            _serialPort.Write(SagaProtocol.SolicitarPaquete);

            byte[] buffer = new byte[SagaProtocol.TamañoPaqueteQ];
            int leidos = 0;
            int intentos = 0;

            // Loop de lectura
            while (leidos < 10 && intentos < 20)
            {
                await Task.Delay(50);
                int disponibles = _serialPort.BytesToRead;
                if (disponibles > 0)
                {
                    int aLeer = Math.Min(disponibles, SagaProtocol.TamañoPaqueteQ - leidos);
                    _serialPort.Read(buffer, leidos, aLeer);
                    leidos += aLeer;
                }
                intentos++;
            }

            string tramaRecibida = Encoding.ASCII.GetString(buffer, 0, leidos);

            // Decodificar
            return DecodificarPaquete(tramaRecibida);
        }

        private async Task EnviarComandoVerificadoAsync(string comando)
        {
            if (!EstaConectado) throw new Exception("Puerto desconectado");
            _serialPort.DiscardInBuffer();
            _serialPort.Write(comando);
            await Task.Delay(150);

            // Opcional: Verificar respuesta :C99Z si es crítico
        }

        private List<DatoCrudo> DecodificarPaquete(string tramaHex)
        {
            var listaPuntos = new List<DatoCrudo>();
            tramaHex = tramaHex.Replace("\r", "").Replace("\n", "").Trim();
            int longitudPunto = 7; // 4 fuerza + 3 posicion
            int cantidadPuntos = tramaHex.Length / longitudPunto;

            for (int i = 0; i < cantidadPuntos; i++)
            {
                try
                {
                    int startIndex = i * longitudPunto;
                    string hexFuerza = tramaHex.Substring(startIndex, 4);
                    string hexPosicion = tramaHex.Substring(startIndex + 4, 3);

                    int valFuerza = Convert.ToInt32(hexFuerza, 16);
                    int valPosicion = Convert.ToInt32(hexPosicion, 16);

                    listaPuntos.Add(new DatoCrudo
                    {
                        Indice = i,
                        FuerzaRaw = valFuerza * 0.1,
                        PosicionRaw = valPosicion
                    });
                }
                catch { /* Ignorar puntos corruptos */ }
            }
            return listaPuntos;
        }
    }
}
