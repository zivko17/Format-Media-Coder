using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

/// <summary>Tests del parseo del JSON de ffprobe a MediaInfo.</summary>
public class ProbeParserTests
{
    private const string HapAlphaJson = """
    {
      "streams": [
        { "codec_type": "video", "codec_name": "hap", "width": 1920, "height": 1080,
          "r_frame_rate": "25/1", "pix_fmt": "rgba" }
      ],
      "format": { "format_name": "mov,mp4,m4a,3gp,3g2,mj2", "duration": "12.500000",
                  "size": "10485760", "bit_rate": "6710886" }
    }
    """;

    private const string H264WithAudioJson = """
    {
      "streams": [
        { "codec_type": "video", "codec_name": "h264", "width": 3840, "height": 2160,
          "r_frame_rate": "30000/1001", "pix_fmt": "yuv420p" },
        { "codec_type": "audio", "codec_name": "aac", "channels": 2, "sample_rate": "48000" }
      ],
      "format": { "format_name": "mov,mp4,m4a", "duration": "60.0", "size": "104857600" }
    }
    """;

    [Fact]
    public void ParsesHapAlpha_DetectsAlpha()
    {
        var m = ProbeParser.Parse("C:/clips/logo.mov", HapAlphaJson);

        Assert.NotNull(m.Video);
        Assert.Equal("hap", m.Video!.Codec);
        Assert.Equal(1920, m.Video.Width);
        Assert.Equal(1080, m.Video.Height);
        Assert.Equal(25, m.Video.Fps);
        Assert.True(m.HasAlpha);
        Assert.False(m.HasAudio);
        Assert.Equal(12.5, m.DurationSeconds, 3);
    }

    [Fact]
    public void Parses2997Fps_Correctly()
    {
        var m = ProbeParser.Parse("C:/clips/clip.mp4", H264WithAudioJson);

        Assert.Equal(29.97, m.Video!.Fps, 2);
        Assert.False(m.HasAlpha);           // yuv420p no tiene alfa
        Assert.True(m.HasAudio);
        Assert.Equal("aac", m.Audio!.Codec);
        Assert.Equal(48000, m.Audio.SampleRate);
    }

    [Fact]
    public void MissingFields_DoNotThrow()
    {
        var m = ProbeParser.Parse("C:/x.mkv", """{ "streams": [], "format": {} }""");
        Assert.Null(m.Video);
        Assert.Null(m.Audio);
        Assert.Equal(0, m.DurationSeconds);
    }
}
