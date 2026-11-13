namespace RolasApp.Vista;

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RolasApp.Controlador;
using RolasApp.Modelo.Entidades;

// Ventana secundaria para clasificar interpretes, dar de alta personas y gestionar bandas.
public partial class VentanaInterpretes : Window
{
    private readonly ControladorInterpretes _controlador;
    private Interprete? _interpreteActual;
    private Persona? _personaActual;
    private Grupo? _grupoActual;
    private bool _actualizandoCombo;

    // Constructor sin parametros requerido por Avalonia UI.
    public VentanaInterpretes()
    {
        InitializeComponent();
        _controlador = new ControladorInterpretes();
        CargarInterpretes();
    }

    // Abre la ventana preseleccionando el interprete de la rola activa si existe.
    public VentanaInterpretes(string? nombrePreseleccionado) : this()
    {
        if (!string.IsNullOrWhiteSpace(nombrePreseleccionado) &&
            ListaInterpretes.ItemsSource is List<Interprete> lista)
        {
            Interprete? encontrado = lista.Find(i => i.Nombre == nombrePreseleccionado);
            if (encontrado != null)
            {
                ListaInterpretes.SelectedItem = encontrado;
            }
        }
    }

    // Carga la lista de interpretes desde la base de datos.
    private void CargarInterpretes(int? idSeleccionar = null)
    {
        List<Interprete> interpretes = _controlador.ObtenerTodosLosInterpretes();
        ListaInterpretes.ItemsSource = interpretes;

        if (idSeleccionar.HasValue)
        {
            ListaInterpretes.SelectedItem = interpretes.Find(i => i.IdInterprete == idSeleccionar.Value);
        }
    }

    // Muestra los datos del interprete seleccionado en la lista izquierda.
    private void AlSeleccionarInterprete(object? sender, SelectionChangedEventArgs e)
    {
        if (ListaInterpretes.SelectedItem is not Interprete interprete)
        {
            _interpreteActual = null;
            ComboTipo.IsEnabled = false;
            OcultarPaneles();
            AvisoDesconocido.IsVisible = true;
            return;
        }

        _interpreteActual = interprete;
        ComboTipo.IsEnabled = true;

        _actualizandoCombo = true;
        ComboTipo.SelectedIndex = (int)interprete.Tipo;
        _actualizandoCombo = false;

        DesplegarFormularioSegunTipo(interprete);
    }

    // Cambia la clasificacion del interprete entre Persona (0), Grupo (1) y Desconocido (2).
    private void AlCambiarTipo(object? sender, SelectionChangedEventArgs e)
    {
        if (_actualizandoCombo || _interpreteActual is null || ComboTipo.SelectedIndex < 0)
        {
            return;
        }

        var nuevoTipo = (TipoInterprete)ComboTipo.SelectedIndex;
        int idActual = _interpreteActual.IdInterprete;

        if (nuevoTipo == TipoInterprete.Persona)
        {
            _personaActual = _controlador.ObtenerODefinirPersona(_interpreteActual);
        }
        else if (nuevoTipo == TipoInterprete.Grupo)
        {
            _grupoActual = _controlador.ObtenerODefinirGrupo(_interpreteActual);
        }
        else
        {
            _controlador.DefinirComoDesconocido(_interpreteActual);
        }

        CargarInterpretes(idActual);
        EtiquetaEstadoInterpretes.Text = $"Intérprete '{_interpreteActual.Nombre}' definido como {nuevoTipo}.";
    }

    // Activa el subpanel correspondiente al tipo del interprete.
    private void DesplegarFormularioSegunTipo(Interprete interprete)
    {
        OcultarPaneles();

        if (interprete.Tipo == TipoInterprete.Persona)
        {
            _personaActual = _controlador.ObtenerODefinirPersona(interprete);
            CampoNombreArtistico.Text = _personaActual.NombreArtistico;
            CampoNombreReal.Text = _personaActual.NombreReal ?? string.Empty;
            CampoFechaNacimiento.Text = _personaActual.FechaNacimiento ?? string.Empty;
            CampoFechaFallecimiento.Text = _personaActual.FechaMuerte ?? string.Empty;
            PanelPersona.IsVisible = true;
        }
        else if (interprete.Tipo == TipoInterprete.Grupo)
        {
            _grupoActual = _controlador.ObtenerODefinirGrupo(interprete);
            CampoNombreGrupo.Text = _grupoActual.Nombre;
            CampoFechaInicio.Text = _grupoActual.FechaInicio ?? string.Empty;
            CampoFechaFin.Text = _grupoActual.FechaFin ?? string.Empty;
            RefrescarMiembrosYPersonas(_grupoActual.IdGrupo);
            PanelGrupo.IsVisible = true;
        }
        else
        {
            AvisoDesconocido.IsVisible = true;
        }
    }

    // Guarda los detalles del interprete cuando esta definido como Persona.
    private void AlPulsarGuardarPersona(object? sender, RoutedEventArgs e)
    {
        if (_interpreteActual is null || _personaActual is null)
        {
            return;
        }

        _personaActual.NombreArtistico = string.IsNullOrWhiteSpace(CampoNombreArtistico.Text)
            ? _interpreteActual.Nombre
            : CampoNombreArtistico.Text.Trim();
        _personaActual.NombreReal = LimpiarOpcional(CampoNombreReal.Text);
        _personaActual.FechaNacimiento = LimpiarOpcional(CampoFechaNacimiento.Text);
        _personaActual.FechaMuerte = LimpiarOpcional(CampoFechaFallecimiento.Text);

        _controlador.GuardarInterpreteComoPersona(_interpreteActual, _personaActual);
        CargarInterpretes(_interpreteActual.IdInterprete);
        EtiquetaEstadoInterpretes.Text = $"Datos de '{_personaActual.NombreArtistico}' actualizados en la base de datos.";
    }

    // Guarda los detalles del interprete cuando esta definido como Grupo.
    private void AlPulsarGuardarGrupo(object? sender, RoutedEventArgs e)
    {
        if (_interpreteActual is null || _grupoActual is null)
        {
            return;
        }

        _grupoActual.Nombre = string.IsNullOrWhiteSpace(CampoNombreGrupo.Text)
            ? _interpreteActual.Nombre
            : CampoNombreGrupo.Text.Trim();
        _grupoActual.FechaInicio = LimpiarOpcional(CampoFechaInicio.Text);
        _grupoActual.FechaFin = LimpiarOpcional(CampoFechaFin.Text);

        _controlador.GuardarInterpreteComoGrupo(_interpreteActual, _grupoActual);
        CargarInterpretes(_interpreteActual.IdInterprete);
        EtiquetaEstadoInterpretes.Text = $"Datos de la banda '{_grupoActual.Nombre}' actualizados en la base de datos.";
    }

    // Agrega la persona elegida en el desplegable a la banda actual.
    private void AlPulsarAgregarMiembro(object? sender, RoutedEventArgs e)
    {
        if (_grupoActual is null || ComboPersonasDisponibles.SelectedItem is not Persona persona)
        {
            EtiquetaEstadoInterpretes.Text = "Primero selecciona o da de alta una persona para agregarla a la banda.";
            return;
        }

        _controlador.AgregarMiembroAGrupo(persona.IdPersona, _grupoActual.IdGrupo);
        RefrescarMiembrosYPersonas(_grupoActual.IdGrupo);
        EtiquetaEstadoInterpretes.Text = $"'{persona.NombreArtistico}' agregado(a) a la banda '{_grupoActual.Nombre}'.";
    }

    // Elimina al integrante seleccionado de la banda actual.
    private void AlPulsarQuitarMiembro(object? sender, RoutedEventArgs e)
    {
        if (_grupoActual is null || ListaMiembros.SelectedItem is not Persona miembro)
        {
            return;
        }

        _controlador.EliminarMiembroDeGrupo(miembro.IdPersona, _grupoActual.IdGrupo);
        RefrescarMiembrosYPersonas(_grupoActual.IdGrupo);
        EtiquetaEstadoInterpretes.Text = $"'{miembro.NombreArtistico}' retirado(a) de la banda '{_grupoActual.Nombre}'.";
    }

    // Da de alta un nuevo individuo directamente en la tabla persons aunque no tenga pistas.
    private void AlPulsarAltaIndividuo(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NuevoArtistico.Text))
        {
            EtiquetaEstadoInterpretes.Text = "Ingresa al menos el nombre artístico para dar de alta al individuo.";
            return;
        }

        var nuevaPersona = new Persona(
            0,
            NuevoArtistico.Text.Trim(),
            LimpiarOpcional(NuevoReal.Text),
            LimpiarOpcional(NuevoNacimiento.Text),
            LimpiarOpcional(NuevoMuerte.Text));

        _controlador.RegistrarPersonaIndependiente(nuevaPersona);

        NuevoArtistico.Text = string.Empty;
        NuevoReal.Text = string.Empty;
        NuevoNacimiento.Text = string.Empty;
        NuevoMuerte.Text = string.Empty;

        if (_grupoActual != null)
        {
            RefrescarMiembrosYPersonas(_grupoActual.IdGrupo);
        }

        EtiquetaEstadoInterpretes.Text = $"Individuo '{nuevaPersona.NombreArtistico}' dado de alta en la base de datos.";
    }

    // Recarga la lista de integrantes del grupo y el ComboBox de personas disponibles.
    private void RefrescarMiembrosYPersonas(int idGrupo)
    {
        ListaMiembros.ItemsSource = _controlador.ObtenerMiembrosDeGrupo(idGrupo);
        List<Persona> personas = _controlador.ObtenerTodasLasPersonas();
        ComboPersonasDisponibles.ItemsSource = personas;
        if (personas.Count > 0)
        {
            ComboPersonasDisponibles.SelectedIndex = 0;
        }
    }

    // Oculta los tres bloques condicionales del panel derecho.
    private void OcultarPaneles()
    {
        AvisoDesconocido.IsVisible = false;
        PanelPersona.IsVisible = false;
        PanelGrupo.IsVisible = false;
    }

    // Convierte cadenas en blanco en null para almacenarlas limpiamente en SQLite.
    private static string? LimpiarOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Cierra la ventana secundaria.
    private void AlPulsarCerrar(object? sender, RoutedEventArgs e) => Close();
}