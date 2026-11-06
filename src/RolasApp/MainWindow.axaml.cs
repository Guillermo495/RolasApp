namespace RolasApp;

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using RolasApp.Controlador;
using RolasApp.Modelo.Entidades;

// Ventana principal que presenta el catalogo musical y el editor de etiquetas.
public partial class MainWindow : Window
{
    private readonly ControladorRolas _controlador;
    private Rola? _rolaSeleccionada;

    // Inicializa la ventana y carga las rolas existentes en la base de datos.
    public MainWindow()
    {
        InitializeComponent();
        _controlador = new ControladorRolas();
        CargarCatalogo();
    }

    // Consulta las rolas mediante el controlador y actualiza la lista visual.
    private void CargarCatalogo(string? filtro = null)
    {
        List<Rola> rolas = string.IsNullOrWhiteSpace(filtro)
            ? _controlador.ObtenerTodasLasRolas()
            : _controlador.BuscarRolas(filtro);

        ListaRolas.ItemsSource = rolas;
        EtiquetaEstado.Text = $"Mostrando {rolas.Count} rola(s) en la biblioteca.";
    }

    // Filtra la lista cada vez que el usuario escribe en la barra de busqueda.
    private void AlCambiarBusqueda(object? sender, TextChangedEventArgs e)
    {
        CargarCatalogo(CajaBusqueda.Text);
    }

    // Limpia el texto de busqueda y restaura la lista completa.
    private void AlPulsarLimpiar(object? sender, RoutedEventArgs e)
    {
        CajaBusqueda.Text = string.Empty;
        CargarCatalogo();
    }

    // Ejecuta el minado en segundo plano actualizando la barra de progreso.
    private async void AlPulsarMinar(object? sender, RoutedEventArgs e)
    {
        BotonMinar.IsEnabled = false;
        BarraProgreso.Value = 0;
        BarraProgreso.IsVisible = true;
        EtiquetaEstado.Text = "Steve está minando el directorio de música...";

        var reportador = new Progress<int>(porcentaje =>
        {
            BarraProgreso.Value = porcentaje;
            EtiquetaEstado.Text = $"Minando pistas MP3... ({porcentaje}%)";
        });

        try
        {
            string rutaMusica = ObtenerDirectorioMusica();
            int totalMinadas = await _controlador.MinarAsync(rutaMusica, reportador);

            CargarCatalogo(CajaBusqueda.Text);
            EtiquetaEstado.Text = $"Minado completado: {totalMinadas} pista(s) procesadas.";
        }
        catch (Exception ex)
        {
            EtiquetaEstado.Text = $"Error durante el minado: {ex.Message}.";
        }
        finally
        {
            BarraProgreso.IsVisible = false;
            BotonMinar.IsEnabled = true;
        }
    }

    // Muestra los metadatos y la portada APIC cuando se selecciona una rola en la tabla.
    private void AlSeleccionarRola(object? sender, SelectionChangedEventArgs e)
    {
        if (ListaRolas.SelectedItem is not Rola rola)
        {
            LimpiarPanelDetalles();
            return;
        }

        _rolaSeleccionada = rola;

        CampoTitulo.Text = rola.Titulo;
        CampoInterprete.Text = rola.NombreInterprete;
        CampoAlbum.Text = rola.NombreAlbum;
        CampoPista.Text = rola.Pista.ToString();
        CampoAnio.Text = rola.Anio.ToString();
        CampoGenero.Text = rola.Genero;
        CampoRuta.Text = rola.Ruta;

        BotonGuardar.IsEnabled = true;
        BotonEliminar.IsEnabled = true;

        MostrarPortada(rola);
    }

    // Guarda las modificaciones en SQLite y sobrescribe las etiquetas ID3v2 del MP3.
    private void AlPulsarGuardar(object? sender, RoutedEventArgs e)
    {
        if (_rolaSeleccionada is null)
        {
            return;
        }

        _rolaSeleccionada.Titulo = CampoTitulo.Text ?? "Unknown";
        _rolaSeleccionada.NombreInterprete = CampoInterprete.Text ?? "Unknown";
        _rolaSeleccionada.NombreAlbum = CampoAlbum.Text ?? "Unknown";
        _rolaSeleccionada.Genero = CampoGenero.Text ?? "Unknown";

        if (int.TryParse(CampoPista.Text, out int pista))
        {
            _rolaSeleccionada.Pista = pista;
        }

        if (int.TryParse(CampoAnio.Text, out int anio))
        {
            _rolaSeleccionada.Anio = anio;
        }

        bool mp3Actualizado = _controlador.GuardarCambiosRola(_rolaSeleccionada);
        CargarCatalogo(CajaBusqueda.Text);

        EtiquetaEstado.Text = mp3Actualizado
            ? "Rola actualizada en la base de datos y en el directorio MP3."
            : "Rola actualizada en la base de datos (no se pudo escribir en el MP3).";
    }

    // Elimina la rola seleccionada de la base de datos y limpia el panel derecho.
    private void AlPulsarEliminar(object? sender, RoutedEventArgs e)
    {
        if (_rolaSeleccionada is null)
        {
            return;
        }

        _controlador.EliminarRola(_rolaSeleccionada.IdRola);
        LimpiarPanelDetalles();
        CargarCatalogo(CajaBusqueda.Text);
        EtiquetaEstado.Text = "Rola eliminada de la base de datos.";
    }

    // Convierte los bytes de la portada APIC en un Bitmap de Avalonia.
    private void MostrarPortada(Rola rola)
    {
        byte[]? bytesPortada = _controlador.ObtenerPortada(rola);
        if (bytesPortada != null && bytesPortada.Length > 0)
        {
            try
            {
                using var flujo = new MemoryStream(bytesPortada);
                ImagenPortada.Source = new Bitmap(flujo);
                TextoSinPortada.IsVisible = false;
                return;
            }
            catch
            {
                // Si los bytes de la imagen no son validos, mostramos el recuadro vacio.
            }
        }

        ImagenPortada.Source = null;
        TextoSinPortada.IsVisible = true;
    }

    // Vacía los controles del panel derecho cuando no hay ninguna rola seleccionada.
    private void LimpiarPanelDetalles()
    {
        _rolaSeleccionada = null;
        CampoTitulo.Text = string.Empty;
        CampoInterprete.Text = string.Empty;
        CampoAlbum.Text = string.Empty;
        CampoPista.Text = string.Empty;
        CampoAnio.Text = string.Empty;
        CampoGenero.Text = string.Empty;
        CampoRuta.Text = string.Empty;
        ImagenPortada.Source = null;
        TextoSinPortada.IsVisible = true;
        BotonGuardar.IsEnabled = false;
        BotonEliminar.IsEnabled = false;
    }

    // Resuelve la ruta del directorio de musica del usuario con o sin tilde.
    private static string ObtenerDirectorioMusica()
    {
        string ruta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Música");

        if (!Directory.Exists(ruta))
        {
            ruta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Musica");
        }

        return ruta;
    }
}