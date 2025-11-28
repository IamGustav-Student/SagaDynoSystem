using Saga.Infrastructure.Driver;
using System;
using System.Threading.Tasks;

Console.WriteLine("--- VISOR DE SENSORES SAGA + PARADA SEGURA ---");

var driver = new SerialDynoDriver();

Console.Write("Puerto COM: ");
string puerto = Console.ReadLine();

try
{
    Console.WriteLine("Conectando...");
    driver.Conectar(puerto);

    // Opcional: Habilitar máquina al inicio para asegurar que responda
    await driver.HabilitarMaquinaAsync();

    Console.WriteLine(">> CONECTADO <<");
    Console.WriteLine("Presiona 'ESPACIO' para DETENER EL MOTOR DE EMERGENCIA.");
    Console.WriteLine("Presiona 'ESC' para salir suavemente.");
    Console.WriteLine("------------------------------------------------");

    bool continuar = true;

    while (continuar)
    {
        // 1. Verificar si se presionó una tecla para salir o parar
        if (Console.KeyAvailable)
        {
            var tecla = Console.ReadKey(true).Key;

            if (tecla == ConsoleKey.Escape)
            {
                continuar = false;
                Console.WriteLine("\n[SALIENDO] Iniciando secuencia de parada...");
            }
            else if (tecla == ConsoleKey.Spacebar)
            {
                Console.WriteLine("\n[!!! PARADA DE EMERGENCIA !!!]");
                await driver.DetenerMotorAsync();
                Console.WriteLine("Comando de paro enviado (:C16Z).");
            }
        }

        // 2. Leer sensores (Lectura pasiva, no afecta al motor)
        try
        {
            string tramaRaw = await driver.LeerSensoresAsync();

            // Parseo rápido para visualización
            int indiceInicio = tramaRaw.IndexOf(":C1BD");
            string info = "Sin datos";

            if (indiceInicio >= 0 && tramaRaw.Length >= indiceInicio + 12)
            {
                string hexFuerza = tramaRaw.Substring(indiceInicio + 5, 4);
                string hexPosicion = tramaRaw.Substring(indiceInicio + 9, 3);

                int valFuerza = Convert.ToInt32(hexFuerza, 16);
                int valPosicion = Convert.ToInt32(hexPosicion, 16);

                info = $"F: {valFuerza} | P: {valPosicion}";
            }

            Console.Write($"\rEstado: {info} | [ESC]=Salir [ESPACIO]=Paro Motor   ");
        }
        catch
        {
            Console.Write($"\rEstado: Error de lectura... reintentando                 ");
        }

        // Pausa para no saturar
        await Task.Delay(100);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nERROR CRÍTICO: {ex.Message}");
}
finally
{
    // --- BLOQUE DE SEGURIDAD CRÍTICO ---
    // Esto se ejecuta SIEMPRE, haya error o no.
    if (driver.EstaConectado)
    {
        Console.WriteLine("\n\n--- SECUENCIA DE APAGADO ---");
        try
        {
            Console.Write("1. Deteniendo motor (:C16Z)... ");
            await driver.DetenerMotorAsync();
            Console.WriteLine("OK");
        }
        catch { Console.WriteLine("Fallo al enviar paro (¿Cable desconectado?)"); }

        try
        {
            Console.Write("2. Deshabilitando equipo (:C00DHZ)... ");
            await driver.DeshabilitarMaquinaAsync();
            Console.WriteLine("OK");
        }
        catch { }

        driver.Desconectar();
        Console.WriteLine("3. Puerto cerrado.");
    }
}

Console.WriteLine("Programa finalizado. Presiona tecla para cerrar.");
Console.ReadKey();