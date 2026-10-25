namespace RolasApp.Modelo.Persistencia;

using System;
using System.IO;
using Microsoft.Data.Sqlite;

// Maneja la conexion con SQLite aplicando el patron Singleton.
public sealed class ConexionBaseDatos : IDisposable
{
    // Constantes de configuracion y rutas.
    private const string NombreAplicacion = "RolasApp";
    private const string NombreBaseDatos = "rolas.db";

    // Consultas SQL del sistema.
    private const string SqlActivarForaneas = "PRAGMA foreign_keys = ON;";
    private const string SqlVerificarTablas =
        "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'rolas';";

    // Script SQL para crear las 7 tablas si no existen.
    private const string EsquemaSql = @"
        CREATE TABLE IF NOT EXISTS types (
            id_type     INTEGER PRIMARY KEY,
            description TEXT
        );

        INSERT OR IGNORE INTO types VALUES (0, 'Person');
        INSERT OR IGNORE INTO types VALUES (1, 'Group');
        INSERT OR IGNORE INTO types VALUES (2, 'Unknown');

        CREATE TABLE IF NOT EXISTS performers (
            id_performer INTEGER PRIMARY KEY,
            id_type      INTEGER,
            name         TEXT,
            FOREIGN KEY (id_type) REFERENCES types (id_type)
        );

        CREATE TABLE IF NOT EXISTS persons (
            id_person  INTEGER PRIMARY KEY,
            stage_name TEXT,
            real_name  TEXT,
            birth_date TEXT,
            death_date TEXT
        );

        CREATE TABLE IF NOT EXISTS groups (
            id_group   INTEGER PRIMARY KEY,
            name       TEXT,
            start_date TEXT,
            end_date   TEXT
        );

        CREATE TABLE IF NOT EXISTS albums (
            id_album INTEGER PRIMARY KEY,
            path     TEXT,
            name     TEXT,
            year     INTEGER
        );

        CREATE TABLE IF NOT EXISTS rolas (
            id_rola      INTEGER PRIMARY KEY,
            id_performer INTEGER,
            id_album     INTEGER,
            path         TEXT,
            title        TEXT,
            track        INTEGER,
            year         INTEGER,
            genre        TEXT,
            FOREIGN KEY (id_performer) REFERENCES performers (id_performer),
            FOREIGN KEY (id_album)     REFERENCES albums (id_album)
        );

        CREATE TABLE IF NOT EXISTS in_group (
            id_person INTEGER,
            id_group  INTEGER,
            PRIMARY KEY (id_person, id_group),
            FOREIGN KEY (id_person) REFERENCES persons (id_person),
            FOREIGN KEY (id_group)  REFERENCES groups (id_group)
        );";

    // Instancia unica compartida (Singleton) creada de forma perezosa y segura.
    private static readonly Lazy<ConexionBaseDatos> _instancia = new(() => new());

    private readonly SqliteConnection _conexion;
    private SqliteTransaction? _transaccionActual;

    // Constructor privado, abre la conexion y crea las tablas si es la primera vez.
    private ConexionBaseDatos()
    {
        string rutaBd = ObtenerRutaBaseDatos();
        bool bdNueva = !File.Exists(rutaBd);

        _conexion = new SqliteConnection($"Data Source={rutaBd}");
        _conexion.Open();

        // SQLite requiere activar manualmente la revision de llaves foraneas.
        Ejecutar(SqlActivarForaneas);

        if (bdNueva || !ExistenTablas())
        {
            Ejecutar(EsquemaSql);
        }
    }

    // Punto de acceso global a la conexion.
    public static ConexionBaseDatos Instancia => _instancia.Value;

    // Propiedad para acceder directamente a la conexion de SQLite.
    public SqliteConnection Conexion => _conexion;

    // Transaccion que se encuentre activa en este momento.
    public SqliteTransaction? TransaccionActual => _transaccionActual;

    // Crea un comando SQL y le asocia la transaccion activa si existe.
    public SqliteCommand CrearComando(string sql)
    {
        var comando = _conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Transaction = _transaccionActual;
        return comando;
    }

    // Inicia una transaccion ACID si no hay una en curso.
    public void IniciarTransaccion() =>
        _transaccionActual ??= _conexion.BeginTransaction();

    // Guarda los cambios de la transaccion de forma definitiva.
    public void ConfirmarTransaccion() =>
        FinalizarTransaccion(t => t.Commit());

    // Deshace los cambios si algo fallo durante la transaccion.
    public void RevertirTransaccion() =>
        FinalizarTransaccion(t => t.Rollback());

    // Cierra y libera la conexion y transaccion al salir.
    public void Dispose()
    {
        _transaccionActual?.Dispose();
        _conexion.Dispose();
    }

    // Obtiene la ruta ~/.local/share/RolasApp/rolas.db en Linux.
    private static string ObtenerRutaBaseDatos()
    {
        string carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            NombreAplicacion);

        Directory.CreateDirectory(carpeta);
        return Path.Combine(carpeta, NombreBaseDatos);
    }

    // Revisa en sqlite_master si ya existe la tabla 'rolas'.
    private bool ExistenTablas() =>
        Convert.ToInt64(Escalar(SqlVerificarTablas) ?? 0L) > 0;

    // Ejecuta una sentencia SQL que no devuelve filas.
    private void Ejecutar(string sql)
    {
        using var comando = CrearComando(sql);
        comando.ExecuteNonQuery();
    }

    // Ejecuta una consulta y devuelve unicamente el primer valor.
    private object? Escalar(string sql)
    {
        using var comando = CrearComando(sql);
        object? resultado = comando.ExecuteScalar();
        return resultado is DBNull ? null : resultado;
    }

    // Ejecuta commit o rollback y limpia la variable de transaccion.
    private void FinalizarTransaccion(Action<SqliteTransaction> accion)
    {
        if (_transaccionActual == null)
        {
            return;
        }

        try
        {
            accion(_transaccionActual);
        }
        finally
        {
            _transaccionActual.Dispose();
            _transaccionActual = null;
        }
    }
}
