namespace RolasApp.Modelo.Entidades;

using System.Collections.Generic;

public class Grupo
{
    public int IdGrupo { get; set; }
    public string Nombre { get; set; } = "Unknown";
    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }

    // Lista en memoria para facilitar la gestión de la tabla 'in_group'
    public List<Persona> Miembros { get; set; } = new();

    public Grupo() { }

    public Grupo(int idGrupo, string nombre, string? fechaInicio = null, string? fechaFin = null)
    {
        IdGrupo = idGrupo;
        Nombre = string.IsNullOrWhiteSpace(nombre) ? "Unknown" : nombre.Trim();
        FechaInicio = string.IsNullOrWhiteSpace(fechaInicio) ? null : fechaInicio.Trim();
        FechaFin = string.IsNullOrWhiteSpace(fechaFin) ? null : fechaFin.Trim();
    }

    public override string ToString() => Nombre;
}