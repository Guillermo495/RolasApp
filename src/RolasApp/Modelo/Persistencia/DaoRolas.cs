namespace RolasApp.Modelo.Persistencia;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using RolasApp.Modelo.Entidades;

// Maneja las operaciones de base de datos para la tabla rolas.
public class DaoRolas
{
    // Constante para textos vacios.
    private const string Desconocido = "Unknown";

    // SELECT base: trae la rola junto con el nombre del interprete y del album.
    private const string SelectRolas = @"
        SELECT r.id_rola, r.id_performer, r.id_album, r.path, r.title,
               r.track, r.year, r.genre, p.name, a.name
        FROM rolas r
        LEFT JOIN performers p ON p.id_performer = r.id_performer
        LEFT JOIN albums     a ON a.id_album     = r.id_album";

    private const string SqlActualizar = @"
        UPDATE rolas
        SET id_performer = @idInterprete,
            id_album     = @idAlbum,
            path         = @ruta,
            title        = @titulo,
            track        = @pista,
            year         = @anio,
            genre        = @genero
        WHERE id_rola = @id;";

    private readonly ConexionBaseDatos _bd;

    // Obtiene la instancia compartida de la conexion.
    public DaoRolas()
    {
        _bd = ConexionBaseDatos.Instancia;
    }

    // Inserta una rola nueva o actualiza sus datos si ya existe en la base de datos.
    public int GuardarRola(Rola rola)
    {
        (string, object?)[] parametros = ParametrosDe(rola);

        // Verifica si la rola ya existe buscando por su ruta en el disco.
        object? idExistente = Escalar(
            "SELECT id_rola FROM rolas WHERE path = @ruta LIMIT 1;",
            ("@ruta", LimpiarRuta(rola.Ruta)));

        // Si ya existe, actualizamos sus metadatos y devolvemos su ID.
        if (idExistente != null)
        {
            rola.IdRola = Convert.ToInt32(idExistente);
            Ejecutar(SqlActualizar, [.. parametros, ("@id", rola.IdRola)]);
            return rola.IdRola;
        }

        // Si no existe, la insertamos y devolvemos el nuevo ID generado.
        rola.IdRola = Convert.ToInt32(Escalar(@"
            INSERT INTO rolas (id_performer, id_album, path, title, track, year, genre)
            VALUES (@idInterprete, @idAlbum, @ruta, @titulo, @pista, @anio, @genero);
            SELECT last_insert_rowid();",
            parametros));

        return rola.IdRola;
    }

    // Obtiene una rola a partir de su llave primaria.
    public Rola? ObtenerPorId(int idRola) =>
        LeerUno($"{SelectRolas} WHERE r.id_rola = @id;", ("@id", idRola));

    // Obtiene una rola buscando por su ruta en el disco.
    public Rola? ObtenerPorRuta(string ruta) =>
        LeerUno($"{SelectRolas} WHERE r.path = @ruta LIMIT 1;", ("@ruta", LimpiarRuta(ruta)));

    // Devuelve la lista completa de rolas ordenadas por titulo.
    public List<Rola> ObtenerTodas() =>
        Leer($"{SelectRolas} ORDER BY r.title;");

    // Devuelve las rolas que pertenecen a un album ordenadas por numero de pista.
    public List<Rola> ObtenerPorAlbum(int idAlbum) =>
        Leer($"{SelectRolas} WHERE r.id_album = @idAlbum ORDER BY r.track, r.title;",
            ("@idAlbum", idAlbum));

    // Devuelve las rolas asociadas a un interprete especifico.
    public List<Rola> ObtenerPorInterprete(int idInterprete) =>
        Leer($"{SelectRolas} WHERE r.id_performer = @idInterprete ORDER BY r.year, r.title;",
            ("@idInterprete", idInterprete));

    // Actualiza los datos de una rola existente desde la interfaz grafica.
    public void ActualizarRola(Rola rola) =>
        Ejecutar(SqlActualizar, [.. ParametrosDe(rola), ("@id", rola.IdRola)]);

    // Elimina una rola de la base de datos a partir de su llave primaria.
    public void EliminarRola(int idRola) =>
        Ejecutar("DELETE FROM rolas WHERE id_rola = @id;", ("@id", idRola));

    // Normaliza los valores de la rola y arma los parametros comunes de INSERT/UPDATE.
    private static (string, object?)[] ParametrosDe(Rola rola) =>
    [
        ("@idInterprete", rola.IdInterprete > 0 ? rola.IdInterprete : null),
        ("@idAlbum", rola.IdAlbum > 0 ? rola.IdAlbum : null),
        ("@ruta", LimpiarRuta(rola.Ruta)),
        ("@titulo", LimpiarTexto(rola.Titulo)),
        ("@pista", rola.Pista >= 0 ? rola.Pista : 0),
        ("@anio", rola.Anio > 0 ? rola.Anio : DateTime.Now.Year),
        ("@genero", LimpiarTexto(rola.Genero)),
    ];

    private static string LimpiarRuta(string? ruta) =>
        string.IsNullOrWhiteSpace(ruta) ? string.Empty : ruta.Trim();

    private static string LimpiarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? Desconocido : texto.Trim();

    // Crea el comando SQL, null debe enviarse como DBNull.Value.
    private SqliteCommand CrearComando(string sql, (string Nombre, object? Valor)[] parametros)
    {
        var comando = _bd.CrearComando(sql);
        foreach (var (nombre, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }
        return comando;
    }

    // Ejecuta una consulta de tipo INSERT, UPDATE o DELETE.
    private void Ejecutar(string sql, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        comando.ExecuteNonQuery();
    }

    // Ejecuta una consulta y devuelve un solo dato.
    private object? Escalar(string sql, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        object? resultado = comando.ExecuteScalar();
        return resultado is null or DBNull ? null : resultado;
    }

    // Recorre varias filas y las convierte a una lista de rolas.
    private List<Rola> Leer(string sql, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        using var lector = comando.ExecuteReader();

        var resultados = new List<Rola>();
        while (lector.Read())
        {
            resultados.Add(MapearRola(lector));
        }
        return resultados;
    }

    // Lee una sola fila o devuelve null si no hubo coincidencia.
    private Rola? LeerUno(string sql, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        using var lector = comando.ExecuteReader();
        return lector.Read() ? MapearRola(lector) : null;
    }

    // Convierte una fila a un objeto Rola, SQLite guarda INTEGER como Int64,
    // GetInt32 hace la conversion, IsDBNull evita excepciones con columnas NULL.
    private static Rola MapearRola(SqliteDataReader lector) =>
        new()
        {
            IdRola = lector.GetInt32(0),
            IdInterprete = lector.IsDBNull(1) ? 0 : lector.GetInt32(1),
            IdAlbum = lector.IsDBNull(2) ? 0 : lector.GetInt32(2),
            Ruta = lector.IsDBNull(3) ? string.Empty : lector.GetString(3),
            Titulo = lector.IsDBNull(4) ? Desconocido : lector.GetString(4),
            Pista = lector.IsDBNull(5) ? 0 : lector.GetInt32(5),
            Anio = lector.IsDBNull(6) ? DateTime.Now.Year : lector.GetInt32(6),
            Genero = lector.IsDBNull(7) ? Desconocido : lector.GetString(7),
            NombreInterprete = lector.IsDBNull(8) ? Desconocido : lector.GetString(8),
            NombreAlbum = lector.IsDBNull(9) ? Desconocido : lector.GetString(9),
        };
}