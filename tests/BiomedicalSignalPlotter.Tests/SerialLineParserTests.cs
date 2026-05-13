using BiomedicalSignalPlotter.Serial;

namespace BiomedicalSignalPlotter.Tests;

public class SerialLineParserTests
{
    private readonly SerialLineParser _parser = new();

    [Theory]
    [MemberData(nameof(ValidLines))]
    public void TryParse_AcceptsExpectedChannelCounts(string line, int expectedChannelCount, double[] expectedValues)
    {
        bool parsed = _parser.TryParse(line, expectedChannelCount, out SerialChannelValues? values);

        Assert.True(parsed);
        Assert.NotNull(values);
        Assert.Equal(expectedValues, values.Values);
    }

    [Theory]
    [InlineData("512", 2)]
    [InlineData("512,310", 1)]
    [InlineData("512,310,99", 2)]
    [InlineData("512,310,203,102,850", 6)]
    [InlineData("A0,A1", 2)]
    public void TryParse_RejectsMalformedLines(string line, int expectedChannelCount)
    {
        bool parsed = _parser.TryParse(line, expectedChannelCount, out SerialChannelValues? values);

        Assert.False(parsed);
        Assert.Null(values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# A0,A1")]
    [InlineData("# channel_count=2")]
    public void TryParse_IgnoresBlankAndCommentLines(string line)
    {
        bool parsed = _parser.TryParse(line, expectedChannelCount: 2, out SerialChannelValues? values);

        Assert.False(parsed);
        Assert.Null(values);
    }

    public static TheoryData<string, int, double[]> ValidLines()
    {
        return new TheoryData<string, int, double[]>
        {
            { "512", 1, [512.0] },
            { "512,310", 2, [512.0, 310.0] },
            { "512, 310", 2, [512.0, 310.0] },
            { "512.0,310.0", 2, [512.0, 310.0] },
            { "512,310,203,102,850,914", 6, [512.0, 310.0, 203.0, 102.0, 850.0, 914.0] }
        };
    }
}
