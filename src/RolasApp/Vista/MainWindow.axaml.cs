namespace RolasApp.Vista;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using RolasApp.Controlador;
using RolasApp.Modelo.Entidades;
using RolasApp.Modelo.Minero;

// Ventana principal que presenta el catalogo musical y el editor de datos en SQLite.
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

    // Abre la ventana secundaria para gestionar interpretes, personas y bandas.
    private async void AlPulsarInterpretes(object? sender, RoutedEventArgs e)
    {
        var ventana = new VentanaInterpretes(_rolaSeleccionada?.NombreInterprete);
        await ventana.ShowDialog(this);
        CargarCatalogo(CajaBusqueda.Text);
    }

    // Abre el selector nativo del sistema para elegir cualquier directorio a minar.
    private async void AlPulsarElegirDirectorio(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> seleccion = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Seleccionar directorio con musica MP3",
                AllowMultiple = false,
            });

        if (seleccion.Count > 0)
        {
            string? rutaElegida = seleccion[0].TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(rutaElegida))
            {
                await EjecutarMinadoAsync(rutaElegida);
            }
        }
    }

    // Ejecuta el minado sobre el directorio de musica resuelto mediante el estandar XDG.
    private async void AlPulsarMinar(object? sender, RoutedEventArgs e)
    {
        string rutaMusica = Steve.ObtenerRutaMusica();
        await EjecutarMinadoAsync(rutaMusica);
    }

    // Coordina el minado asincrono y actualiza la barra de progreso en la interfaz.
    private async Task EjecutarMinadoAsync(string rutaDirectorio)
    {
        BotonMinar.IsEnabled = false;
        BotonElegirDirectorio.IsEnabled = false;
        BarraProgreso.Value = 0;
        BarraProgreso.IsVisible = true;
        EtiquetaEstado.Text = $"Steve está minando en: {rutaDirectorio}...";

        var reportador = new Progress<int>(porcentaje =>
        {
            BarraProgreso.Value = porcentaje;
            EtiquetaEstado.Text = $"Minando pistas MP3... ({porcentaje}%)";
        });

        try
        {
            int totalMinadas = await _controlador.MinarAsync(rutaDirectorio, reportador);

            CargarCatalogo(CajaBusqueda.Text);
            EtiquetaEstado.Text = $"Minado completado en {rutaDirectorio}: {totalMinadas} pista(s) procesadas.";
        }
        catch (Exception ex)
        {
            EtiquetaEstado.Text = $"Error durante el minado: {ex.Message}.";
        }
        finally
        {
            BarraProgreso.IsVisible = false;
            BotonMinar.IsEnabled = true;
            BotonElegirDirectorio.IsEnabled = true;
        }
    }

    // Muestra los metadatos y las imagenes APIC cuando se selecciona una rola en la tabla.
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

        MostrarImagenes(rola);
    }

    // Guarda las modificaciones exclusivamente en la base de datos SQLite sin alterar el MP3.
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

        _controlador.GuardarCambiosRola(_rolaSeleccionada);
        CargarCatalogo(CajaBusqueda.Text);
        EtiquetaEstado.Text = "Cambios guardados en la base de datos SQLite (directorio MP3 intacto).";
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

    // Carga en la interfaz tanto la portada frontal como la fotografia del artista.
    private void MostrarImagenes(Rola rola)
    {
        AsignarImagen(ImagenPortada, TextoSinPortada, _controlador.ObtenerPortada(rola));
        AsignarImagen(ImagenArtista, TextoSinArtista, _controlador.ObtenerFotoArtista(rola));
    }

    // Convierte un arreglo de bytes APIC en un Bitmap y lo asigna al control Image indicado.
    private static void AsignarImagen(Image controlImagen, TextBlock etiquetaVacia, byte[]? bytesImagen)
    {
        if (bytesImagen != null && bytesImagen.Length > 0)
        {
            try
            {
                using var flujo = new MemoryStream(bytesImagen);
                controlImagen.Source = new Bitmap(flujo);
                etiquetaVacia.IsVisible = false;
                return;
            }
            catch
            {
                // Si los bytes no corresponden a una imagen valida, se muestra el texto alternativo.
            }
        }

        controlImagen.Source = null;
        etiquetaVacia.IsVisible = true;
    }

    // Vacia los controles del panel derecho cuando no hay ninguna rola seleccionada.
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
        ImagenArtista.Source = null;
        TextoSinArtista.IsVisible = true;
        BotonGuardar.IsEnabled = false;
        BotonEliminar.IsEnabled = false;
    }
}