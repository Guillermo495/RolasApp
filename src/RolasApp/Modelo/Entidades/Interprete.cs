using System;

namespace RolasApp.Modelo.Entidades;

public class Interprete
{
    public int IdInterprete { get; set; }
    public TipoInterprete Tipo { get; set; } = TipoInterprete.Desconocido;
    public string Nombre { get; set; } = "Unknow";

    public Interprete() { }

    public Interprete(int idInterprete, TipoInterprete tipo, string nombre)
    {
        IdInterprete = idInterprete;
        Tipo = tipo;
        Nombre = string.IsNullOrWhiteSpace(nombre) ? "Unknown" : nombre.Trim();
    }
    public override string ToString() => Nombre;

}