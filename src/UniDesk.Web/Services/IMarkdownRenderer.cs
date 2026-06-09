namespace UniDesk.Web.Services;

public interface IMarkdownRenderer
{
    string ToSafeHtml(string markdown);
}
