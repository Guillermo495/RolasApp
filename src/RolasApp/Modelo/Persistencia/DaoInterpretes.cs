namespace RolasApp.Modelo.Persistencia;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using RolasApp.Modelo.Entidades;

// Objeto de acceso a datos para interpretes, personas y grupos.
public class DaoInterpretes
{
    // Constante por omision para nombres vacios.
    private const string Desconocido = "Unknown";

    // Columnas comunes para no escribirlas repetidas en los SELECT.
    private const string ColumnasInterprete = "id_performer, id_type, name";
    private const string ColumnasPersona = "id_person, stage_name, real_name, birth_date, death_date";
    private const string ColumnasGrupo = "id_group, name, start_date, end_date";

    // Referencia a la conexion unica
    private readonly ConexionBaseDatos _bd = ConexionBaseDatos.Instancia;

    // Busca un interprete por nombre; si no existe, lo inserta como Unknown.
    public int ObtenerOCrearInterprete(string? nombre)
    {
        string nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? Desconocido : nombre.Trim();

        // Revisa si ya esta registrado.
        object? id = Escalar(
            "SELECT id_performer FROM performers WHERE name = @nombre LIMIT 1;",
            ("@nombre", nombreLimpio));

        // Si no existe, lo inserta y recupera el id autoincremental generado.
        id ??= Escalar(@"
            INSERT INTO performers (id_type, name) VALUES (@tipo, @nombre);
            SELECT last_insert_rowid();",
            ("@tipo", (int)TipoInterprete.Desconocido),
            ("@nombre", nombreLimpio));

        return Convert.ToInt32(id);
    }

    // Obtiene un interprete segun su id.
    public Interprete? ObtenerPorId(int idInterprete) =>
        LeerUno(
            $"SELECT {ColumnasInterprete} FROM performers WHERE id_performer = @id;",
            MapearInterprete,
            ("@id", idInterprete));

    // Obtiene la lista completa de interpretes ordenados por nombre.
    public List<Interprete> ObtenerTodos() =>
        Leer($"SELECT {ColumnasInterprete} FROM performers ORDER BY name;", MapearInterprete);

    // Cambia el tipo del interprete a Persona y guarda sus datos personales.
    public void DefinirComoPersona(int idInterprete, Persona persona)
    {
        ActualizarInterprete(idInterprete, TipoInterprete.Persona, persona.NombreArtistico);
        GuardarPersona(persona);
    }

    // Inserta una persona o actualiza sus datos si el nombre artistico ya existe.
    public int GuardarPersona(Persona persona)
    {
        (string, object?)[] parametros =
        [
            ("@artistico", persona.NombreArtistico),
            ("@real", persona.NombreReal),
            ("@nacimiento", persona.FechaNacimiento),
            ("@muerte", persona.FechaMuerte),
        ];

        Persona? existente = ObtenerPersonaPorNombreArtistico(persona.NombreArtistico);

        if (existente != null)
        {
            // Si ya existe, actualiza sus fechas y nombre real.
            Ejecutar(@"
                UPDATE persons
                SET stage_name = @artistico,
                    real_name  = @real,
                    birth_date = @nacimiento,
                    death_date = @muerte
                WHERE id_person = @id;",
                [.. parametros, ("@id", existente.IdPersona)]);

            persona.IdPersona = existente.IdPersona;
        }
        else
        {
            // Si es nueva, inserta y guarda el nuevo id generado.
            persona.IdPersona = Convert.ToInt32(Escalar(@"
                INSERT INTO persons (stage_name, real_name, birth_date, death_date)
                VALUES (@artistico, @real, @nacimiento, @muerte);
                SELECT last_insert_rowid();",
                parametros));
        }

        return persona.IdPersona;
    }

    // Busca los datos de una persona por su nombre artistico.
    public Persona? ObtenerPersonaPorNombreArtistico(string nombreArtistico) =>
        LeerUno(
            $"SELECT {ColumnasPersona} FROM persons WHERE stage_name = @nombre LIMIT 1;",
            MapearPersona,
            ("@nombre", nombreArtistico.Trim()));

    // Lista todas las personas registradas en la tabla 'persons'.
    public List<Persona> ObtenerTodasLasPersonas() =>
        Leer($"SELECT {ColumnasPersona} FROM persons ORDER BY stage_name;", MapearPersona);

    // Cambia el tipo del interprete a Grupo y guarda los datos de la banda.
    public void DefinirComoGrupo(int idInterprete, Grupo grupo)
    {
        ActualizarInterprete(idInterprete, TipoInterprete.Grupo, grupo.Nombre);
        GuardarGrupo(grupo);
    }

    // Inserta un grupo o actualiza sus fechas si el nombre ya existe.
    public int GuardarGrupo(Grupo grupo)
    {
        (string, object?)[] parametros =
        [
            ("@nombre", grupo.Nombre),
            ("@inicio", grupo.FechaInicio),
            ("@fin", grupo.FechaFin),
        ];

        Grupo? existente = ObtenerGrupoPorNombre(grupo.Nombre);

        if (existente != null)
        {
            Ejecutar(@"
                UPDATE groups
                SET name       = @nombre,
                    start_date = @inicio,
                    end_date   = @fin
                WHERE id_group = @id;",
                [.. parametros, ("@id", existente.IdGrupo)]);

            grupo.IdGrupo = existente.IdGrupo;
        }
        else
        {
            grupo.IdGrupo = Convert.ToInt32(Escalar(@"
                INSERT INTO groups (name, start_date, end_date)
                VALUES (@nombre, @inicio, @fin);
                SELECT last_insert_rowid();",
                parametros));
        }

        return grupo.IdGrupo;
    }

    // Busca un grupo por nombre y carga su lista de integrantes.
    public Grupo? ObtenerGrupoPorNombre(string nombre)
    {
        Grupo? grupo = LeerUno(
            $"SELECT {ColumnasGrupo} FROM groups WHERE name = @nombre LIMIT 1;",
            MapearGrupo,
            ("@nombre", nombre.Trim()));

        if (grupo != null)
        {
            grupo.Miembros = ObtenerMiembrosDeGrupo(grupo.IdGrupo);
        }

        return grupo;
    }

    // Relaciona a un musico con una agrupacion en la tabla in_group.
    public void AgregarPersonaAGrupo(int idPersona, int idGrupo) =>
        Ejecutar(
            "INSERT OR IGNORE INTO in_group (id_person, id_group) VALUES (@idPersona, @idGrupo);",
            ("@idPersona", idPersona),
            ("@idGrupo", idGrupo));

    // Quita la relacion entre una persona y un grupo.
    public void EliminarPersonaDeGrupo(int idPersona, int idGrupo) =>
        Ejecutar(
            "DELETE FROM in_group WHERE id_person = @idPersona AND id_group = @idGrupo;",
            ("@idPersona", idPersona),
            ("@idGrupo", idGrupo));

    // Trae todas las personas que tocan en un grupo haciendo un JOIN.
    public List<Persona> ObtenerMiembrosDeGrupo(int idGrupo) =>
        Leer(@"
            SELECT p.id_person, p.stage_name, p.real_name, p.birth_date, p.death_date
            FROM persons p
            INNER JOIN in_group ig ON p.id_person = ig.id_person
            WHERE ig.id_group = @idGrupo
            ORDER BY p.stage_name;",
            MapearPersona,
            ("@idGrupo", idGrupo));

    // Actualiza el tipo y nombre en la tabla performers.
    private void ActualizarInterprete(int idInterprete, TipoInterprete tipo, string nombre) =>
        Ejecutar(
            "UPDATE performers SET id_type = @tipo, name = @nombre WHERE id_performer = @id;",
            ("@tipo", (int)tipo),
            ("@nombre", nombre),
            ("@id", idInterprete));

    // Crea el comando SQL y convierte null a DBNull.Value para evitar errores en SQLite.
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

    // Ejecuta una consulta y devuelve un solo dato (como un ID o conteo).
    private object? Escalar(string sql, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        object? resultado = comando.ExecuteScalar();
        return resultado is DBNull ? null : resultado;
    }

    // Recorre varias filas y las convierte a una lista de objetos.
    private List<T> Leer<T>(string sql, Func<SqliteDataReader, T> mapear, params (string, object?)[] parametros)
    {
        using var comando = CrearComando(sql, parametros);
        using var lector = comando.ExecuteReader();

        var resultados = new List<T>();
        while (lector.Read())
        {
            resultados.Add(mapear(lector));
        }
        return resultados;
    }

    // Lee una sola fila o devuelve null si no hubo coincidencia.
    private T? LeerUno<T>(string sql, Func<SqliteDataReader, T> mapear, params (string, object?)[] parametros)
        where T : class
    {
        using var comando = CrearComando(sql, parametros);
        using var lector = comando.ExecuteReader();
        return lector.Read() ? mapear(lector) : null;
    }

    // Si la celda es nula en la base de datos devuelve null de C#.
    private static string? TextoONulo(SqliteDataReader lector, int columna) =>
        lector.IsDBNull(columna) ? null : lector.GetString(columna);

    // Convierte una fila de la base de datos a un objeto Interprete.
    private static Interprete MapearInterprete(SqliteDataReader lector) =>
        new(
            lector.GetInt32(0),
            (TipoInterprete)lector.GetInt32(1),
            TextoONulo(lector, 2) ?? Desconocido);

    // Convierte una fila a un objeto Persona.
    private static Persona MapearPersona(SqliteDataReader lector) =>
        new(
            lector.GetInt32(0),
            TextoONulo(lector, 1) ?? Desconocido,
            TextoONulo(lector, 2),
            TextoONulo(lector, 3),
            TextoONulo(lector, 4));

    // Convierte una fila a un objeto Grupo.
    private static Grupo MapearGrupo(SqliteDataReader lector) =>
        new(
            lector.GetInt32(0),
            TextoONulo(lector, 1) ?? Desconocido,
            TextoONulo(lector, 2),
            TextoONulo(lector, 3));
}
