using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.Tests.Processing;

public class HashServiceTests
{
    private readonly HashService _sut = new();

    [Fact]
    public void ComputeShortHash_empty_string_returns_documented_prefix()
    {
        var hash = _sut.ComputeShortHash(string.Empty);

        Assert.Equal("D41D8CD9", hash);
    }

    [Fact]
    public void ComputeShortHash_known_content_returns_first_eight_hex_chars_of_md5_utf8()
    {
        // MD5("hello") = 5d41402abc4b2a76b9719d911017c592
        var hash = _sut.ComputeShortHash("hello");

        Assert.Equal("5D41402A", hash);
    }
}
