using BiomedicalSignalPlotter.Serial;

namespace BiomedicalSignalPlotter.Tests;

public class SerialLineParserTests
{
    private readonly SerialLineParser _parser = new();

    [Theory]
    [InlineData("512,310", 512.0, 310.0)]
    [InlineData("512, 310", 512.0, 310.0)]
    [InlineData("512.0,310.0", 512.0, 310.0)]
    public void TryParse_AcceptsValidTwoChannelLines(string line, double expectedChannel1, double expectedChannel2)
    {
        bool parsed = _parser.TryParse(line, out SerialChannelValues values);

        Assert.True(parsed);
        Assert.Equal(expectedChannel1, values.Channel1);
        Assert.Equal(expectedChannel2, values.Channel2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# A0,A1")]
    [InlineData("A0,A1")]
    [InlineData("512")]
    [InlineData("512,310,99")]
    public void TryParse_IgnoresBlankCommentAndMalformedLines(string line)
    {
        bool parsed = _parser.TryParse(line, out SerialChannelValues values);

        Assert.False(parsed);
        Assert.Equal(default, values);
    }
}
