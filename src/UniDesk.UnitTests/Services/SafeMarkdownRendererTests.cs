using UniDesk.Web.Services;

namespace UniDesk.UnitTests.Services;

public class SafeMarkdownRendererTests
{
    [Fact]
    public void ToSafeHtml_RendersMarkdownWithoutAllowingRawHtml()
    {
        var renderer = new SafeMarkdownRenderer();

        var html = renderer.ToSafeHtml("**bold** `<tag>` <script>alert(1)</script>");

        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<code>&lt;tag&gt;</code>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void ToSafeHtml_RendersFencedCodeBlockSafely()
    {
        var renderer = new SafeMarkdownRenderer();

        var html = renderer.ToSafeHtml("""
            ```
            <script>alert(1)</script>
            ```
            """);

        Assert.Contains("<pre><code>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }
}
