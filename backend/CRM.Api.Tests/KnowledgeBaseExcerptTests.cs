using CRM.Api.KnowledgeBase;

namespace CRM.Api.Tests;

public class KnowledgeBaseExcerptTests
{
    [Fact]
    public void Build_ReturnsEmptyString_ForNullOrWhitespace()
    {
        Assert.Equal(string.Empty, KnowledgeBaseExcerpt.Build(null));
        Assert.Equal(string.Empty, KnowledgeBaseExcerpt.Build(string.Empty));
        Assert.Equal(string.Empty, KnowledgeBaseExcerpt.Build("   \n\t  "));
    }

    [Fact]
    public void Build_ReturnsUnchanged_WhenShorterThanMaxLength()
    {
        var result = KnowledgeBaseExcerpt.Build("Short body text.");

        Assert.Equal("Short body text.", result);
    }

    [Fact]
    public void Build_CollapsesWhitespaceRuns()
    {
        var result = KnowledgeBaseExcerpt.Build("Line one.\n\n\n   Line   two.\t\tLine three.");

        Assert.Equal("Line one. Line two. Line three.", result);
    }

    [Fact]
    public void Build_TrimsOnWordBoundary_WhenLongerThanMaxLength()
    {
        var content = string.Join(" ", Enumerable.Repeat("word", 60)); // far longer than 200 chars

        var result = KnowledgeBaseExcerpt.Build(content, maxLength: 50);

        Assert.True(result.Length <= 51); // 50 + ellipsis char
        Assert.EndsWith("…", result);
        Assert.DoesNotContain("wor…", result); // never cuts mid-word
    }

    [Fact]
    public void Build_UsesDefaultMaxLengthOf200()
    {
        var content = new string('a', 500);

        var result = KnowledgeBaseExcerpt.Build(content);

        // No spaces to break on, so it hard-cuts at maxLength.
        Assert.Equal(201, result.Length); // 200 chars + ellipsis
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void Build_HandlesExactlyMaxLength()
    {
        var content = new string('a', 200);

        var result = KnowledgeBaseExcerpt.Build(content, maxLength: 200);

        Assert.Equal(content, result);
    }
}
