namespace RolasApp.Modelo.Steve;

using System;
using System.Collections.Generic;
using System.IO;
using RolasApp.Modelo.Entidades;
using RolasApp.Modelo.Persistencia;

// Escanea el sistema para extraer etiquetas ID3v2 y poblar la base de datos.
public class Minero
{
    // Valor por omision cuando una etiqueta ID3v2 viene vacia o no existe.
    private const string Desconocido = "Unknown";

    // Patron de busqueda para localizar pistas en formato MP3.
    private const string PatronMp3 = "*.mp3";

    private readonly ConexionBaseDatos _bd;
    private readonly DaoInterpretes _daoInterpretes;
    private readonly DaoAlbumes _daoAlbumes;
    private readonly DaoRolas _daoRolas;

    // Inicializa el minero con la conexion compartida y los tres DAOs.
    public Minero()
    {
        _bd = ConexionBaseDatos.Instancia;
        _daoInterpretes = new DaoInterpretes();
        _daoAlbumes = new DaoAlbumes();
        _daoRolas = new DaoRolas();
    }

    // Devuelve la ruta del directorio de musica del usuario en el sistema.
    public static string ObtenerRutaMusica() =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

    // Recorre la ruta indicada, extrae las etiquetas ID3v2 y guarda las rolas en una transaccion.
    public int Minar(string? rutaRaiz = null)
    {
        string rutaBusqueda = string.IsNullOrWhiteSpace(rutaRaiz)
            ? ObtenerRutaMusica()
            : rutaRaiz.Trim();

        if (!Directory.Exists(rutaBusqueda))
        {
            return 0;
        }

        IEnumerable<string> rutasMp3 = BuscarMp3(rutaBusqueda);
        int totalMinadas = 0;

        // Iniciamos una unica transaccion para insertar todo el lote en una sola operacion de disco.
        _bd.IniciarTransaccion();

        try
        {
            foreach (string rutaMp3 in rutasMp3)
            {
                if (ProcesarMp3(rutaMp3))
                {
                    totalMinadas++;
                }
            }

            _bd.ConfirmarTransaccion();
        }
        catch
        {
            _bd.RevertirTransaccion();
            throw;
        }

        return totalMinadas;
    }

    // Busca recursivamente todos los directorios MP3 ignorando aquellos sin permisos de lectura.
    private static IEnumerable<string> BuscarMp3(string rutaRaiz)
    {
        var opciones = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        return Directory.EnumerateFiles(rutaRaiz, PatronMp3, opciones);
    }

    // Extrae las etiquetas ID3v2 de un MP3 y sincroniza su interprete, album y rola.
    private bool ProcesarMp3(string rutaMp3)
    {
        try
        {
            using var mp3 = TagLib.File.Create(rutaMp3);
            TagLib.Tag etiquetas = mp3.Tag;

            // Extraemos las etiquetas ID3v2: TPE1, TALB, TIT2, TCON, TRCK y TDRC.
            string nombreInterprete = LimpiarTexto(etiquetas.FirstPerformer);
            string nombreAlbum = LimpiarTexto(etiquetas.Album);
            string titulo = LimpiarTexto(etiquetas.Title);
            string genero = LimpiarTexto(etiquetas.FirstGenre);
            int pista = etiquetas.Track > 0 ? (int)etiquetas.Track : 0;
            int anio = etiquetas.Year > 0 ? (int)etiquetas.Year : DateTime.Now.Year;
            string rutaPadre = Path.GetDirectoryName(rutaMp3) ?? string.Empty;

            // Obtener o registrar primero el interprete y el album para cumplir las llaves foraneas.
            int idInterprete = _daoInterpretes.ObtenerOCrearInterprete(nombreInterprete);
            int idAlbum = _daoAlbumes.ObtenerOCrearAlbum(rutaPadre, nombreAlbum, anio);

            //Construir la entidad Rola y guardarla en la base de datos.
            var rola = new Rola
            {
                IdInterprete = idInterprete,
                IdAlbum = idAlbum,
                Ruta = rutaMp3,
                Titulo = titulo,
                Pista = pista,
                Anio = anio,
                Genero = genero,
                NombreInterprete = nombreInterprete,
                NombreAlbum = nombreAlbum,
            };

            _daoRolas.GuardarRola(rola);
            return true;
        }
        catch (TagLib.CorruptFileException)
        {
            return false;
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    // Normaliza una cadena vacia o nula al valor por omision Unknown.
    private static string LimpiarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? Desconocido : texto.Trim();
}