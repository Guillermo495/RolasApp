namespace RolasApp.Modelo.Persistencia;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using RolasApp.Modelo.Entidades;

// Maneja las operaciones de base de datos para la tabla albums.
public class DaoAlbumes
{
    private readonly ConexionBaseDatos _bd;

    // Obtiene la instancia compartida de la conexion.
    public DaoAlbumes()
    {
        _bd = ConexionBaseDatos.Instancia;
    }

    // Busca un album por ruta y nombre, si no existe, lo inserta y devuelve su ID.
    public int ObtenerOCrearAlbum(string? ruta, string? nombre, int anio)
    {
        string rutaLimpia = string.IsNullOrWhiteSpace(ruta) ? string.Empty : ruta.Trim();
        string nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Unknown" : nombre.Trim();
        int anioValido = anio > 0 ? anio : DateTime.Now.Year;

        // Consultar si el album ya existe en esa carpeta.
        using (var cmdBuscar = _bd.CrearComando("SELECT id_album FROM albums WHERE path = @ruta AND name = @nombre LIMIT 1;"))
        {
            cmdBuscar.Parameters.AddWithValue("@ruta", rutaLimpia);
            cmdBuscar.Parameters.AddWithValue("@nombre", nombreLimpio);

            object? resultado = cmdBuscar.ExecuteScalar();
            if (resultado != null && resultado != DBNull.Value)
            {
                return Convert.ToInt32(resultado);
            }
        }

        // Si no existe, insertarlo y devolver el nuevo ID generado.
        string sqlInsertar = @"
            INSERT INTO albums (path, name, year) VALUES (@ruta, @nombre, @anio);
            SELECT last_insert_rowid();";

        using var cmdInsertar = _bd.CrearComando(sqlInsertar);
        cmdInsertar.Parameters.AddWithValue("@ruta", rutaLimpia);
        cmdInsertar.Parameters.AddWithValue("@nombre", nombreLimpio);
        cmdInsertar.Parameters.AddWithValue("@anio", anioValido);

        return Convert.ToInt32(cmdInsertar.ExecuteScalar());
    }

    // Obtiene un album a partir de su llave primaria.
    public Album? ObtenerPorId(int idAlbum)
    {
        using var comando = _bd.CrearComando("SELECT id_album, path, name, year FROM albums WHERE id_album = @id;");
        comando.Parameters.AddWithValue("@id", idAlbum);

        using var lector = comando.ExecuteReader();
        if (lector.Read())
        {
            return new Album(
                lector.GetInt32(0),
                lector.IsDBNull(1) ? string.Empty : lector.GetString(1),
                lector.IsDBNull(2) ? "Unknown" : lector.GetString(2),
                lector.IsDBNull(3) ? DateTime.Now.Year : lector.GetInt32(3));
        }

        return null;
    }

    // Devuelve la lista completa de albumes ordenados por nombre.
    public List<Album> ObtenerTodos()
    {
        var lista = new List<Album>();

        using var comando = _bd.CrearComando("SELECT id_album, path, name, year FROM albums ORDER BY name, year;");
        using var lector = comando.ExecuteReader();

        while (lector.Read())
        {
            var album = new Album(
                lector.GetInt32(0),
                lector.IsDBNull(1) ? string.Empty : lector.GetString(1),
                lector.IsDBNull(2) ? "Unknown" : lector.GetString(2),
                lector.IsDBNull(3) ? DateTime.Now.Year : lector.GetInt32(3));

            lista.Add(album);
        }

        return lista;
    }

    // Actualiza los datos de un album existente.
    public void ActualizarAlbum(Album album)
    {
        string sql = "UPDATE albums SET path = @ruta, name = @nombre, year = @anio WHERE id_album = @id;";

        using var comando = _bd.CrearComando(sql);
        comando.Parameters.AddWithValue("@ruta", album.Ruta.Trim());
        comando.Parameters.AddWithValue("@nombre", string.IsNullOrWhiteSpace(album.Nombre) ? "Unknown" : album.Nombre.Trim());
        comando.Parameters.AddWithValue("@anio", album.Anio);
        comando.Parameters.AddWithValue("@id", album.IdAlbum);

        comando.ExecuteNonQuery();
    }
}