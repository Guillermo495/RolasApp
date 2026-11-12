namespace RolasApp.Controlador;

using System.Collections.Generic;
using RolasApp.Modelo.Entidades;
using RolasApp.Modelo.Persistencia;

// Controlador encargado de gestionar interpretes, personas, bandas y sus integrantes.
public class ControladorInterpretes
{
    private readonly DaoInterpretes _daoInterpretes;

    // Inicializa el controlador instanciando el DAO de interpretes.
    public ControladorInterpretes()
    {
        _daoInterpretes = new DaoInterpretes();
    }

    // Devuelve todos los interpretes registrados en la tabla performers.
    public List<Interprete> ObtenerTodosLosInterpretes() =>
        _daoInterpretes.ObtenerTodos();

    // Clasifica a un interprete como Desconocido en la base de datos.
    public void DefinirComoDesconocido(Interprete interprete)
    {
        interprete.Tipo = TipoInterprete.Desconocido;
        _daoInterpretes.DefinirComoDesconocido(interprete.IdInterprete, interprete.Nombre);
    }

    // Obtiene los datos de Persona asociados a un interprete o crea una instancia inicial.
    public Persona ObtenerODefinirPersona(Interprete interprete)
    {
        Persona? existente = _daoInterpretes.ObtenerPersonaPorNombreArtistico(interprete.Nombre);
        if (existente != null)
        {
            return existente;
        }

        var nueva = new Persona(0, interprete.Nombre);
        _daoInterpretes.DefinirComoPersona(interprete.IdInterprete, nueva);
        interprete.Tipo = TipoInterprete.Persona;
        return nueva;
    }

    // Guarda los detalles de un interprete tipo Persona en las tablas performers y persons.
    public void GuardarInterpreteComoPersona(Interprete interprete, Persona persona)
    {
        interprete.Tipo = TipoInterprete.Persona;
        interprete.Nombre = persona.NombreArtistico;
        _daoInterpretes.DefinirComoPersona(interprete.IdInterprete, persona);
    }

    // Da de alta o actualiza un individuo en la tabla persons aunque no interprete ninguna pista.
    public int RegistrarPersonaIndependiente(Persona persona) =>
        _daoInterpretes.GuardarPersona(persona);

    // Devuelve todas las personas registradas en la tabla persons.
    public List<Persona> ObtenerTodasLasPersonas() =>
        _daoInterpretes.ObtenerTodasLasPersonas();

    // Obtiene los datos de Grupo asociados a un interprete o crea una instancia inicial.
    public Grupo ObtenerODefinirGrupo(Interprete interprete)
    {
        Grupo? existente = _daoInterpretes.ObtenerGrupoPorNombre(interprete.Nombre);
        if (existente != null)
        {
            return existente;
        }

        var nuevo = new Grupo(0, interprete.Nombre);
        _daoInterpretes.DefinirComoGrupo(interprete.IdInterprete, nuevo);
        interprete.Tipo = TipoInterprete.Grupo;
        return nuevo;
    }

    // Guarda los detalles de un interprete tipo Grupo en las tablas performers y groups.
    public void GuardarInterpreteComoGrupo(Interprete interprete, Grupo grupo)
    {
        interprete.Tipo = TipoInterprete.Grupo;
        interprete.Nombre = grupo.Nombre;
        _daoInterpretes.DefinirComoGrupo(interprete.IdInterprete, grupo);
    }

    // Devuelve la lista de personas que integran un grupo especifico.
    public List<Persona> ObtenerMiembrosDeGrupo(int idGrupo) =>
        _daoInterpretes.ObtenerMiembrosDeGrupo(idGrupo);

    // Agrega una persona como integrante de una banda en la tabla in_group.
    public void AgregarMiembroAGrupo(int idPersona, int idGrupo) =>
        _daoInterpretes.AgregarPersonaAGrupo(idPersona, idGrupo);

    // Retira a una persona de una banda en la tabla in_group.
    public void EliminarMiembroDeGrupo(int idPersona, int idGrupo) =>
        _daoInterpretes.EliminarPersonaDeGrupo(idPersona, idGrupo);
}
