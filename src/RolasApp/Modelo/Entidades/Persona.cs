namespace RolasApp.Modelo.Entidades;

public class Persona
{
    public int IdPersona { get; set; }
    public string NombreArtistico { get; set; } = string.Empty;
    public string? NombreReal { get; set; }
    public string? FechaNacimiento { get; set; }
    public string? FechaMuerte { get; set; }

    public Persona() { }

    public Persona(int idPersona, string nombreArtistico, string? nombreReal = null, string? fechaNacimiento = null, string? fechaMuerte = null)
    {
        IdPersona = idPersona;
        NombreArtistico = nombreArtistico.Trim();
        NombreReal = string.IsNullOrWhiteSpace(nombreReal) ? null : nombreReal.Trim();
        FechaNacimiento = string.IsNullOrWhiteSpace(fechaNacimiento) ? null : fechaNacimiento.Trim();
        FechaMuerte = string.IsNullOrWhiteSpace(fechaMuerte) ? null : fechaMuerte.Trim();
    }

    public override string ToString()
    {
        return !string.IsNullOrWhiteSpace(NombreArtistico)
            ? NombreArtistico
            : (NombreReal ?? "Unknown");
    }
}