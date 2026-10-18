namespace RolasApp.Modelo.Entidades;

using System;

public class Album
{
    public int IdAlbum { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string Nombre { get; set; } = "Unknown";
    public int Anio { get; set; } = DateTime.Now.Year;

    public static Album Desconocido => new(0, string.Empty, "Unknown", 0);

    public Album() { }

    public Album(int idAlbum, string ruta, string nombre, int anio)
    {
        IdAlbum = idAlbum;
        Ruta = ruta;
        Nombre = string.IsNullOrWhiteSpace(nombre) ? "Unknown" : nombre.Trim();
        Anio = anio > 0 ? anio : DateTime.Now.Year;
    }
    public override string ToString() => Nombre;

}