using FormatMediaCoder.Core.Models;
using FormatMediaCoder.Core.Services;
using Xunit;

namespace FormatMediaCoder.Core.Tests;

public class MoreOpsTests
{
    [Fact]
    public void Concat_UsesConcatDemuxer_StreamCopy()
    {
        var args = FfmpegOps.BuildConcatArgs("C:/tmp/list.txt", "out.mp4");
        Assert.Contains("concat", args);
        Assert.Contains("-safe", args);
        Assert.Contains("copy", args);
        Assert.Equal("out.mp4", args[^1]);
    }

    [Fact]
    public void ConcatListLine_EscapesQuotes()
    {
        var line = FfmpegOps.ConcatListLine(@"C:\clips\o'brien.mov");
        Assert.StartsWith("file '", line);
        Assert.Contains(@"o'\''brien", line);
    }

    [Fact]
    public void Filters_ScaleAndRotate()
    {
        var args = FfmpegOps.BuildFiltersArgs("in.mp4", "out.mp4", new Resolution(1280, 720), "cw");
        var vf = args[args.IndexOf("-vf") + 1];
        Assert.Contains("scale=1280:720", vf);
        Assert.Contains("transpose=1", vf);
        Assert.Contains("copy", args); // audio copy
    }

    [Fact]
    public void Filters_180_FlipsBoth()
    {
        var args = FfmpegOps.BuildFiltersArgs("in.mp4", "out.mp4", null, "180");
        var vf = args[args.IndexOf("-vf") + 1];
        Assert.Contains("hflip", vf);
        Assert.Contains("vflip", vf);
    }

    [Fact]
    public void YtDlp_Video_RemuxesToMp4()
    {
        var args = YtDlpArgs.Build("https://x/y", @"C:\dl", YtDlpArgs.Kind.Video);
        Assert.Contains("--merge-output-format", args);
        Assert.Equal("mp4", args[args.IndexOf("--merge-output-format") + 1]);
        Assert.Contains("--no-playlist", args);
        Assert.Equal("https://x/y", args[^1]);
    }

    [Fact]
    public void YtDlp_AudioMp3_Extracts()
    {
        var args = YtDlpArgs.Build("https://x/y", @"C:\dl", YtDlpArgs.Kind.AudioMp3);
        Assert.Contains("-x", args);
        Assert.Equal("mp3", args[args.IndexOf("--audio-format") + 1]);
    }
}
