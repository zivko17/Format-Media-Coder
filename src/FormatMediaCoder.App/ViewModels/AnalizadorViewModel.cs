using System.Collections.ObjectModel;
using FormatMediaCoder.Core.Models;

namespace FormatMediaCoder.App.ViewModels;

/// <summary>Analizador forense: suelta un archivo y ve qué es (ffprobe).</summary>
public sealed class AnalizadorViewModel : ToolViewModelBase
{
    public ObservableCollection<InfoRow> Rows { get; } = new();

    private string _title = "Elige un archivo para analizarlo";
    public string Title { get => _title; private set => Set(ref _title, value); }

    protected override async void OnInputChanged()
    {
        if (string.IsNullOrEmpty(InputFile)) return;
        Rows.Clear();
        Title = System.IO.Path.GetFileName(InputFile);
        StatusLine = "Analizando…";
        try
        {
            var m = await Ffmpeg.ProbeAsync(InputFile);
            Fill(m);
            StatusLine = "";
        }
        catch (Exception ex)
        {
            StatusLine = "No se pudo analizar: " + ex.Message;
        }
    }

    private void Fill(MediaInfo m)
    {
        void Add(string k, string? v) { if (!string.IsNullOrEmpty(v)) Rows.Add(new InfoRow(k, v)); }

        Add("Contenedor", m.ContainerFormat);
        Add("Duración", FormatDuration(m.DurationSeconds));
        Add("Tamaño", FormatSize(m.SizeBytes));
        if (m.BitRate > 0) Add("Bitrate", FormatBitrate(m.BitRate));

        if (m.Video is { } v)
        {
            Add("Vídeo — codec", v.Codec.ToUpperInvariant());
            Add("Vídeo — resolución", v.ResolutionLabel);
            Add("Vídeo — fps", Trim(v.Fps));
            Add("Vídeo — pixel format", v.PixelFormat);
            Add("Vídeo — canal alfa", v.HasAlpha ? "Sí (transparencia)" : "No");
        }
        if (m.Audio is { } a)
        {
            Add("Audio — codec", a.Codec.ToUpperInvariant());
            Add("Audio — canales", a.Channels.ToString());
            Add("Audio — sample rate", a.SampleRate > 0 ? $"{a.SampleRate} Hz" : null);
        }
    }

    private static string FormatDuration(double s)
    {
        var t = TimeSpan.FromSeconds(s);
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
    private static string FormatSize(long b) =>
        b > 1_000_000_000 ? $"{b / 1e9:0.00} GB" : b > 1_000_000 ? $"{b / 1e6:0.0} MB" : $"{b / 1e3:0} KB";
    private static string FormatBitrate(long b) =>
        b > 1_000_000 ? $"{b / 1e6:0.0} Mb/s" : $"{b / 1e3:0} kb/s";
    private static string Trim(double v) =>
        v % 1 == 0 ? ((int)v).ToString() : v.ToString("0.##");
}

public sealed record InfoRow(string Label, string Value);
