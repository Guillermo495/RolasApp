namespace RolasApp.Controlador;

using System;
using System.IO;
using LibVLCSharp.Shared;
using RolasApp.Modelo.Entidades;

// Coordina la reproduccion de audio mediante LibVLC manteniendo la vista desacoplada.
public class ControladorReproductor : IDisposable
{
    private readonly LibVLC? _libVlc;
    private readonly MediaPlayer? _reproductor;
    private Media? _medioActual;
    private bool _liberado;

    public bool EstaDisponible { get; }
    public Rola? RolaActual { get; private set; }

    public bool EstaReproduciendo => _reproductor?.IsPlaying ?? false;
    public bool EstaPausado => _reproductor?.State == VLCState.Paused;

    public event Action? EstadoCambiado;

    public ControladorReproductor()
    {
        try
        {
            Core.Initialize();
            _libVlc = new LibVLC("--no-video");
            _reproductor = new MediaPlayer(_libVlc);

            _reproductor.Playing += (_, _) => EstadoCambiado?.Invoke();
            _reproductor.Paused += (_, _) => EstadoCambiado?.Invoke();
            _reproductor.Stopped += (_, _) => EstadoCambiado?.Invoke();
            _reproductor.EndReached += (_, _) => EstadoCambiado?.Invoke();

            EstaDisponible = true;
        }
        catch
        {
            EstaDisponible = false;
        }
    }

    // Inicia la reproduccion de la rola indicada a partir de su ruta en disco.
    public bool Reproducir(Rola? rola)
    {
        if (!EstaDisponible || _libVlc is null || _reproductor is null)
        {
            return false;
        }

        if (rola is null || string.IsNullOrWhiteSpace(rola.Ruta) || !File.Exists(rola.Ruta))
        {
            return false;
        }

        _reproductor.Stop();
        _medioActual?.Dispose();

        string rutaCompleta = Path.GetFullPath(rola.Ruta);
        _medioActual = new Media(_libVlc, new Uri(rutaCompleta));
        RolaActual = rola;

        bool iniciado = _reproductor.Play(_medioActual);
        if (!iniciado)
        {
            EstadoCambiado?.Invoke();
        }

        return iniciado;
    }

    public void Pausar()
    {
        if (_reproductor is { CanPause: true, IsPlaying: true })
        {
            _reproductor.Pause();
        }
    }

    public void Reanudar()
    {
        if (_reproductor is { State: VLCState.Paused })
        {
            _reproductor.Play();
        }
    }

    public void AlternarPausa()
    {
        if (!EstaDisponible || _reproductor is null)
        {
            return;
        }

        if (_reproductor.IsPlaying)
        {
            Pausar();
        }
        else if (_reproductor.State == VLCState.Paused)
        {
            Reanudar();
        }
        else if (RolaActual is not null)
        {
            Reproducir(RolaActual);
        }
    }

    public void Detener()
    {
        if (_reproductor is null)
        {
            return;
        }

        _reproductor.Stop();
        EstadoCambiado?.Invoke();
    }

    public void EstablecerVolumen(int volumen)
    {
        if (_reproductor is null)
        {
            return;
        }

        _reproductor.Volume = Math.Clamp(volumen, 0, 100);
    }

    public void Dispose()
    {
        if (_liberado)
        {
            return;
        }

        if (_reproductor is not null)
        {
            _reproductor.Stop();
            _reproductor.Dispose();
        }

        _medioActual?.Dispose();
        _libVlc?.Dispose();
        _liberado = true;
        GC.SuppressFinalize(this);
    }
}