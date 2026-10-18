namespace RolasApp.Modelo.Entidades;

using System;

public class Rola
{
    public int IdRola { get; set; }
    public int IdInterprete { get; set; }
    public int IdAlbum { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string Titulo { get; set; } = "Unknown";
    public int Pista { get; set; } = 0;
    public int Anio { get; set; } = DateTime.Now.Year;
    public string Genero { get; set; } = "Unknown";

    // Propiedades de lectura para mostrar en las columnas de la tabla y cuadrícula
    public string NombreInterprete { get; set; } = "Unknown";
    public string NombreAlbum { get; set; } = "Unknown";

    public Rola() { }
}

public class ConstructorRola
{
    private readonly Rola _rola = new();

    public ConstructorRola ConId(int idRola)
    {
        _rola.IdRola = idRola;
        return this;
    }

    public ConstructorRola ConInterprete(int idInterprete, string nombreInterprete = "Unknown")
    {
        _rola.IdInterprete = idInterprete;
        _rola.NombreInterprete = string.IsNullOrWhiteSpace(nombreInterprete) ? "Unknown" : nombreInterprete.Trim();
        return this;
    }

    public ConstructorRola ConAlbum(int idAlbum, string nombreAlbum = "Unknown")
    {
        _rola.IdAlbum = idAlbum;
        _rola.NombreAlbum = string.IsNullOrWhiteSpace(nombreAlbum) ? "Unknown" : nombreAlbum.Trim();
        return this;
    }

    public ConstructorRola ConRuta(string ruta)
    {
        _rola.Ruta = ruta ?? string.Empty;
        return this;
    }

    public ConstructorRola ConTitulo(string? titulo)
    {
        _rola.Titulo = string.IsNullOrWhiteSpace(titulo) ? "Unknown" : titulo.Trim();
        return this;
    }

    public ConstructorRola ConPista(int pista)
    {
        _rola.Pista = pista >= 0 ? pista : 0;
        return this;
    }

    public ConstructorRola ConAnio(int anio)
    {
        _rola.Anio = anio > 0 ? anio : DateTime.Now.Year;
        return this;
    }

    public ConstructorRola ConGenero(string? genero)
    {
        _rola.Genero = string.IsNullOrWhiteSpace(genero) ? "Unknown" : genero.Trim();
        return this;
    }

    public Rola Construir()
    {
        if (string.IsNullOrWhiteSpace(_rola.Ruta))
            throw new InvalidOperationException("Toda rola debe tener una ruta de archivo MP3 válida.");

        return _rola;
    }
}