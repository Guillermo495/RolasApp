namespace RolasApp.Controlador;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RolasApp.Modelo.Entidades;
using RolasApp.Modelo.Minero;
using RolasApp.Modelo.Persistencia;

// Coordina la comunicacion entre la interfaz grafica, los DAOs y el minero Steve.
public class ControladorRolas
{
    private readonly DaoInterpretes _daoInterpretes;
    private readonly DaoAlbumes _daoAlbumes;
    private readonly DaoRolas _daoRolas;
    private readonly Steve _steve;

    // Inicializa el controlador instanciando los DAOs y el minero.
    public ControladorRolas()
    {
        _daoInterpretes = new DaoInterpretes();
        _daoAlbumes = new DaoAlbumes();
        _daoRolas = new DaoRolas();
        _steve = new Steve();
    }

    // Ejecuta el minado de forma sincrona sobre la ruta indicada o la carpeta de musica.
    public int Minar(string? rutaRaiz = null, IProgress<int>? progreso = null) =>
        _steve.Minar(rutaRaiz, progreso);

    // Ejecuta el minado en un hilo secundario para no bloquear la interfaz grafica.
    public Task<int> MinarAsync(string? rutaRaiz = null, IProgress<int>? progreso = null) =>
        Task.Run(() => _steve.Minar(rutaRaiz, progreso));

    // Devuelve todas las rolas registradas en la base de datos ordenadas por titulo.
    public List<Rola> ObtenerTodasLasRolas() =>
        _daoRolas.ObtenerTodas();

    // Busca una rola especifica a partir de su identificador unico.
    public Rola? ObtenerRolaPorId(int idRola) =>
        _daoRolas.ObtenerPorId(idRola);

    // Devuelve un album especifico a partir de su identificador unico.
    public Album? ObtenerAlbumPorId(int idAlbum) =>
        _daoAlbumes.ObtenerPorId(idAlbum);

    // Devuelve todos los albumes registrados en la base de datos.
    public List<Album> ObtenerTodosLosAlbumes() =>
        _daoAlbumes.ObtenerTodos();

    // Actualiza directamente los datos de un album (nombre, anio, ruta) en la base de datos.
    public void GuardarCambiosAlbum(Album album)
    {
        if (album.IdAlbum > 0)
        {
            _daoAlbumes.ActualizarAlbum(album);
        }
    }

    // Devuelve todas las rolas que pertenecen a un album especifico.
    public List<Rola> ObtenerRolasPorAlbum(int idAlbum) =>
        _daoRolas.ObtenerPorAlbum(idAlbum);

    // Devuelve todas las rolas asociadas a un interprete especifico.
    public List<Rola> ObtenerRolasPorInterprete(int idInterprete) =>
        _daoRolas.ObtenerPorInterprete(idInterprete);

    // Filtra la coleccion de rolas por texto libre o por prefijos.
    public List<Rola> BuscarRolas(string? consulta)
    {
        List<Rola> todas = _daoRolas.ObtenerTodas();
        if (string.IsNullOrWhiteSpace(consulta))
        {
            return todas;
        }

        string[] terminos = consulta.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtradas = new List<Rola>();

        foreach (Rola rola in todas)
        {
            if (CumpleTodosLosTerminos(rola, terminos))
            {
                filtradas.Add(rola);
            }
        }

        return filtradas;
    }

    // Actualiza la rola, su interprete y su album exclusivamente en SQLite sin tocar el MP3.
    public bool GuardarCambiosRola(Rola rola)
    {
        if (rola.IdRola <= 0)
        {
            return false;
        }

        string rutaPadre = Path.GetDirectoryName(rola.Ruta) ?? string.Empty;

        // Asegura que el interprete exista en performers y asignamos su llave foranea.
        rola.IdInterprete = _daoInterpretes.ObtenerOCrearInterprete(rola.NombreInterprete);

        // Obtiene o crea el album y sincronizamos su anio en la tabla albums.
        rola.IdAlbum = _daoAlbumes.ObtenerOCrearAlbum(rutaPadre, rola.NombreAlbum, rola.Anio);
        Album? albumActual = _daoAlbumes.ObtenerPorId(rola.IdAlbum);
        if (albumActual != null && albumActual.Anio != rola.Anio)
        {
            albumActual.Anio = rola.Anio;
            _daoAlbumes.ActualizarAlbum(albumActual);
        }

        // Guarda los cambios en la tabla rolas de SQLite.
        _daoRolas.ActualizarRola(rola);
        return true;
    }

    // Elimina una rola del registro de la base de datos sin borrar el directorio MP3 fisico.
    public void EliminarRola(int idRola) =>
        _daoRolas.EliminarRola(idRola);

    // Extrae los bytes de la portada incrustada en el directorio MP3 de la rola.
    public byte[]? ObtenerPortada(Rola? rola) =>
        rola is null ? null : _steve.ObtenerPortada(rola.Ruta);

    // Extrae los bytes de la fotografia del artista incrustada en el directorio MP3 de la rola.
    public byte[]? ObtenerFotoArtista(Rola? rola) =>
        rola is null ? null : _steve.ObtenerFotoArtista(rola.Ruta);

    // Evalua si una rola coincide con todos los terminos ingresados en la barra de busqueda.
    private static bool CumpleTodosLosTerminos(Rola rola, string[] terminos)
    {
        foreach (string termino in terminos)
        {
            if (!CumpleTermino(rola, termino))
            {
                return false;
            }
        }

        return true;
    }

    // Verifica si un termino individual coincide con los atributos de la rola.
    private static bool CumpleTermino(Rola rola, string termino)
    {
        int indiceDosPuntos = termino.IndexOf(':');
        if (indiceDosPuntos > 0 && indiceDosPuntos < termino.Length - 1)
        {
            string prefijo = termino[..indiceDosPuntos].ToLowerInvariant();
            string valor = termino[(indiceDosPuntos + 1)..];

            return prefijo switch
            {
                "titulo" or "title" => Contiene(rola.Titulo, valor),
                "interprete" or "artista" or "artist" => Contiene(rola.NombreInterprete, valor),
                "album" => Contiene(rola.NombreAlbum, valor),
                "genero" or "genre" => Contiene(rola.Genero, valor),
                "anio" or "year" => Contiene(rola.Anio.ToString(), valor),
                _ => CoincideTextoLibre(rola, termino),
            };
        }

        return CoincideTextoLibre(rola, termino);
    }

    // Comprueba si el texto buscado aparece en cualquiera de los campos principales de la rola.
    private static bool CoincideTextoLibre(Rola rola, string valor) =>
        Contiene(rola.Titulo, valor) ||
        Contiene(rola.NombreInterprete, valor) ||
        Contiene(rola.NombreAlbum, valor) ||
        Contiene(rola.Genero, valor) ||
        Contiene(rola.Anio.ToString(), valor);

    // Compara dos cadenas ignorando diferencias entre mayusculas y minusculas.
    private static bool Contiene(string origen, string busqueda) =>
        !string.IsNullOrEmpty(origen) &&
        origen.Contains(busqueda, StringComparison.OrdinalIgnoreCase);
}
