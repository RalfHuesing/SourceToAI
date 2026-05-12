using SourceToAI.CLI.Services.Export.AiFeed;

namespace SourceToAI.Tests.Export.AiFeed;

public class YamlDoubleQuotedEscapingTests
{
    [Fact]
    public void EscapeYamlDoubleQuoted_empty_string_unchanged()
    {
        Assert.Equal(string.Empty, YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(string.Empty));
    }

    [Fact]
    public void EscapeYamlDoubleQuoted_plain_text_unchanged()
    {
        Assert.Equal("Sol (MyApp)", YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted("Sol (MyApp)"));
    }

    [Fact]
    public void EscapeYamlDoubleQuoted_escapes_backslash_quote_cr_lf_tab()
    {
        var input = "a\\b\"c\r\nd\te";
        var expected = "a\\\\b\\\"c\\r\\nd\\te";
        Assert.Equal(expected, YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(input));
    }

    [Fact]
    public void EscapeYamlDoubleQuoted_matches_composer_regression_case()
    {
        var display = YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted("S: \"x\" (P\nline2)");
        Assert.Equal("S: \\\"x\\\" (P\\nline2)", display);
    }

    [Fact]
    public void EscapeYamlDoubleQuoted_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => YamlDoubleQuotedEscaping.EscapeYamlDoubleQuoted(null!));
    }
}
