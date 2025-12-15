using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Saga.Core.Models; // Para PuntoEnsayo
using Saga.UI.Messages;
using ScottPlot;

namespace Saga.UI
{
    public partial class MainWindow : Window
    {
        // Guardamos los últimos datos recibidos para poder cambiar de gráfica sin re-ejecutar el ensayo
        private List<PuntoEnsayo> _ultimosDatos = new List<PuntoEnsayo>();

        public MainWindow()
        {
            InitializeComponent();
            ConfigurarEstiloBase();

            WeakReferenceMessenger.Default.Register<UpdateGraphMessage>(this, (r, m) =>
            {
                _ultimosDatos = m.Puntos;
                ActualizarGrafica(); // Dibujar cuando llegan datos nuevos
            });
        }

        private void ConfigurarEstiloBase()
        {
            GraficaPrincipal.Plot.FigureBackground.Color = Color.FromHex("#1E1E1E");
            GraficaPrincipal.Plot.DataBackground.Color = Color.FromHex("#2D2D30");
            GraficaPrincipal.Plot.Axes.Color(Color.FromHex("#AAAAAA"));
            GraficaPrincipal.Plot.Grid.MajorLineColor = Color.FromHex("#444444");
        }

        private void ActualizarGrafica()
        {
            if (_ultimosDatos == null || _ultimosDatos.Count == 0) return;

            GraficaPrincipal.Plot.Clear();

            // Determinar qué dibujar según el ComboBox
            int opcion = ComboTipoGrafica.SelectedIndex;

            double[] dataX, dataY;
            string labelX, labelY;

            if (opcion == 0) // Fuerza vs Posición
            {
                dataX = _ultimosDatos.Select(p => p.Posicion).ToArray();
                dataY = _ultimosDatos.Select(p => p.Fuerza).ToArray();
                labelX = "Posición (mm)";
                labelY = "Fuerza (kg)";
            }
            else if (opcion == 1) // Fuerza vs Velocidad
            {
                dataX = _ultimosDatos.Select(p => p.Velocidad).ToArray();
                dataY = _ultimosDatos.Select(p => p.Fuerza).ToArray();
                labelX = "Velocidad (mm/s)";
                labelY = "Fuerza (kg)";
            }
            else // Posición vs Tiempo
            {
                dataX = _ultimosDatos.Select(p => p.Tiempo).ToArray();
                dataY = _ultimosDatos.Select(p => p.Posicion).ToArray();
                labelX = "Tiempo (s)";
                labelY = "Posición (mm)";
            }

            // Dibujar
            var scatter = GraficaPrincipal.Plot.Add.Scatter(dataX, dataY);
            scatter.Color = Color.FromHex("#00FFFF");
            scatter.LineWidth = 2;

            // Configurar etiquetas
            GraficaPrincipal.Plot.Axes.Bottom.Label.Text = labelX;
            GraficaPrincipal.Plot.Axes.Left.Label.Text = labelY;
            GraficaPrincipal.Plot.Title(((ComboBoxItem)ComboTipoGrafica.SelectedItem).Content.ToString());

            GraficaPrincipal.Plot.Axes.AutoScale();
            GraficaPrincipal.Refresh();
        }

        // Evento cuando el usuario cambia el ComboBox manualmente
        private void ComboTipoGrafica_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarGrafica();
        }
    }
}