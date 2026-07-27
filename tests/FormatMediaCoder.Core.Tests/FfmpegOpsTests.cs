using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>Tests de los builders de argumentos de las herramientas sueltas.</summary>
public class FfmpegOpsTests
{
    [Fact]
    public void Convert_Hap_Alpha_KeepsFormatFlag()
    {
        var args = FfmpegOps.BuildConvertArgs(new FfmpegOps.ConvertOptions
        {
            Input = "in.mp4", Output = "out.mov", VideoCodec = "hap", HapVariant = "hap_alpha",
        });
        int i = args.IndexOf("-format");
        Assert.True(i >= 0);
        Assert.Equal("hap_alpha", args[i + 1]);
        Assert.Contains("format=rgba", args[args.IndexOf("-vf") + 1]);
        Assert.Equal("out.mov", args[^1]);
    }

    [Fact]
    public void Convert_Prores_DoesNotForceBitrate()
    {
        var args = FfmpegOps.BuildConvertArgs(new FfmpegOps.ConvertOptions
        {
            Input = "in.mp4", Output = "out.mov", VideoCodec = "prores", VideoBitrate = "8000k",
        });
        Assert.DoesNotContain("-b:v", args);           // intra-frame: no bitrate forzado
        Assert.Contains("prores_ks", args);
    }

    [Fact]
    public void Convert_Normalize_AddsLoudnorm()
    {
        var args = FfmpegOps.BuildConvertArgs(new FfmpegOps.ConvertOptions
        {
            Input = "in.mp4", Output = "out.mp4", VideoCodec = "libx264", NormalizeAudio = true,
        });
        Assert.Contains("loudnorm=I=-23:LRA=7:TP=-2.0", args[args.IndexOf("-af") + 1]);
    }

    [Fact]
    public void Compress_Scale_UsesColonSyntax()
    {
        var args = FfmpegOps.BuildCompressArgs(new FfmpegOps.CompressOptions
        {
            Input = "in.mp4", Output = "out.mp4", Crf = 20, Preset = "slow", Resolution = "1280x720",
        });
        Assert.Contains("scale=1280:720", args[args.IndexOf("-vf") + 1]);
        Assert.Contains("20", args);
        Assert.Contains("slow", args);
    }

    [Fact]
    public void ExtractAudio_Wav_NoBitrate()
    {
        var args = FfmpegOps.BuildExtractAudioArgs("in.mp4", "out.wav", "wav", "192k");
        Assert.Contains("pcm_s16le", args);
        Assert.DoesNotContain("-b:a", args);
    }

    [Fact]
    public void ExtractAudio_Mp3_HasBitrateAndCodec()
    {
        var args = FfmpegOps.BuildExtractAudioArgs("in.mp4", "out.mp3", "mp3", "256k");
        Assert.Contains("libmp3lame", args);
        Assert.Equal("256k", args[args.IndexOf("-b:a") + 1]);
    }

    [Fact]
    public void Trim_ComputesDuration_AndStreamCopies()
    {
        var args = FfmpegOps.BuildTrimArgs("in.mp4", "out.mp4", "00:00:10", "00:00:25");
        // -t 15
        Assert.Equal("15", args[args.IndexOf("-t") + 1]);
        Assert.Contains("copy", args);
    }

    [Theory]
    [InlineData("00:01:30", 90)]
    [InlineData("01:00:00", 3600)]
    [InlineData("45", 45)]
    public void TimeToSeconds_Parses(string t, double expected) =>
        Assert.Equal(expected, FfmpegOps.TimeToSeconds(t));

    [Fact]
    public void ImageConvert_Ico_ForcesSquare256()
    {
        var args = FfmpegOps.BuildImageConvertArgs("in.png", "out.ico", "ico");
        Assert.Contains("scale=256:256", args[args.IndexOf("-vf") + 1]);
    }
}
