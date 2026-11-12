namespace RolasApp.Modelo.Minero;

using System;
using System.Collections.Generic;
using System.IO;
using RolasApp.Modelo.Entidades;
using RolasApp.Modelo.Persistencia;

// Escanea el sistema para leer etiquetas ID3v2.4 y poblar la base de datos sin modificar los MP3.
public class Steve
{
    // Valor por omision cuando una etiqueta ID3v2 viene vacia o no existe.
    private const string Desconocido = "Unknown";

    // Patron de busqueda para localizar pistas en formato MP3.
    private const string PatronMp3 = "*.mp3";

    private readonly ConexionBaseDatos _bd;
    private readonly DaoInterpretes _daoInterpretes;
    private readonly DaoAlbumes _daoAlbumes;
    private readonly DaoRolas _daoRolas;

    // Inicializa a Steve con la conexion compartida y los tres DAOs.
    public Steve()
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
    public int Minar(string? rutaRaiz = null, IProgress<int>? progreso = null)
    {
        string rutaBusqueda = string.IsNullOrWhiteSpace(rutaRaiz)
            ? ObtenerRutaMusica()
            : rutaRaiz.Trim();

        if (!Directory.Exists(rutaBusqueda))
        {
            return 0;
        }

        List<string> rutasMp3 = BuscarMp3(rutaBusqueda);
        int totalRutas = rutasMp3.Count;
        int procesadas = 0;
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

                procesadas++;
                if (progreso != null && totalRutas > 0)
                {
                    int porcentaje = (procesadas * 100) / totalRutas;
                    progreso.Report(porcentaje);
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

    // Extrae los bytes de la imagen de portada (APIC) incrustada en el directorio MP3.
    public byte[]? ObtenerPortada(string? rutaMp3)
    {
        if (string.IsNullOrWhiteSpace(rutaMp3) || !File.Exists(rutaMp3))
        {
            return null;
        }

        try
        {
            using var mp3 = TagLib.File.Create(rutaMp3);
            TagLib.IPicture[] imagenes = mp3.Tag.Pictures;

            if (imagenes != null && imagenes.Length > 0)
            {
                foreach (TagLib.IPicture imagen in imagenes)
                {
                    if (imagen.Type == TagLib.PictureType.FrontCover)
                    {
                        return imagen.Data.Data;
                    }
                }

                return imagenes[0].Data.Data;
            }
        }
        catch
        {
            // Si falla la lectura de la imagen, regresamos null para mostrar el recuadro vacio.
        }

        return null;
    }

    // Extrae los bytes de la fotografia del artista (APIC con tipo Artist o LeadArtist).
    public byte[]? ObtenerFotoArtista(string? rutaMp3)
    {
        if (string.IsNullOrWhiteSpace(rutaMp3) || !File.Exists(rutaMp3))
        {
            return null;
        }

        try
        {
            using var mp3 = TagLib.File.Create(rutaMp3);
            foreach (TagLib.IPicture foto in mp3.Tag.Pictures)
            {
                if (foto.Type == TagLib.PictureType.Artist || foto.Type == TagLib.PictureType.LeadArtist)
                {
                    return foto.Data.Data;
                }
            }
        }
        catch
        {
            // En caso de error de lectura, regresa nulo para evitar fallos.
        }

        return null;
    }

    // Busca recursivamente todas las pistas MP3 ignorando los directorios sin permisos de lectura.
    private static List<string> BuscarMp3(string rutaRaiz)
    {
        var opciones = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        return new List<string>(Directory.EnumerateFiles(rutaRaiz, PatronMp3, opciones));
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

            // Construimos la entidad Rola aplicando el patron Builder (ConstructorRola).
            Rola rola = new ConstructorRola()
                .ConInterprete(idInterprete, nombreInterprete)
                .ConAlbum(idAlbum, nombreAlbum)
                .ConRuta(rutaMp3)
                .ConTitulo(titulo)
                .ConPista(pista)
                .ConAnio(anio)
                .ConGenero(genero)
                .Construir();

            _daoRolas.GuardarRola(rola);
            return true;
        }
        catch (TagLib.CorruptFileException)
        {
            // Si el encabezado MP3 esta corrupto, lo omitimos sin detener el minado general.
            return false;
        }
        catch (TagLib.UnsupportedFormatException)
        {
            // Si el formato no es valido pese a la extension, continuamos con el siguiente.
            return false;
        }
        catch (IOException)
        {
            // Si ocurre un error de lectura fisica, continuamos con el resto de la coleccion.
            return false;
        }
    }

    // Normaliza una cadena vacia o nula al valor por omision Unknown.
    private static string LimpiarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? Desconocido : texto.Trim();
}