using Microsoft.AspNetCore.Html;

namespace CallLogManagementSystem.Helpers
{
    // Minimal self-contained inline-SVG icon set for the app shell (sidebar/topbar), so the
    // whole UI has zero external font/CDN dependency — consistent with Section 3's "self-host
    // everything under wwwroot/libraries" approach, important for a plant network that may not
    // have internet access. Each icon is a single simple geometric glyph, not traced from any
    // third-party icon font.
    public static class IconHelper
    {
        private static readonly Dictionary<string, string> Paths = new()
        {
            ["menu"] = "<path d=\"M3 6h18M3 12h18M3 18h18\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" fill=\"none\"/>",
            ["dashboard"] = "<rect x=\"3\" y=\"3\" width=\"8\" height=\"8\" rx=\"1.5\"/><rect x=\"13\" y=\"3\" width=\"8\" height=\"5\" rx=\"1.5\"/><rect x=\"13\" y=\"10\" width=\"8\" height=\"11\" rx=\"1.5\"/><rect x=\"3\" y=\"13\" width=\"8\" height=\"8\" rx=\"1.5\"/>",
            ["plus-call"] = "<circle cx=\"12\" cy=\"12\" r=\"9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/><path d=\"M12 8v8M8 12h8\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/>",
            ["list"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"3.2\" rx=\"1\"/><rect x=\"3\" y=\"10.4\" width=\"18\" height=\"3.2\" rx=\"1\"/><rect x=\"3\" y=\"16.8\" width=\"18\" height=\"3.2\" rx=\"1\"/>",
            ["user-list"] = "<circle cx=\"8\" cy=\"7\" r=\"3.2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/><path d=\"M2.5 20c0-3.3 2.5-6 5.5-6s5.5 2.7 5.5 6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/><path d=\"M16 5h6M16 9h6M16 13h6\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/>",
            ["folder-open"] = "<path d=\"M3 7a1.5 1.5 0 0 1 1.5-1.5H9l2 2h8.5A1.5 1.5 0 0 1 21 9v9.5a1.5 1.5 0 0 1-1.5 1.5H4.5A1.5 1.5 0 0 1 3 18.5V7z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/>",
            ["check-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/><path d=\"M8 12.5l2.5 2.5L16 9.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            ["rotate"] = "<path d=\"M4 12a8 8 0 0 1 13.5-5.8M20 12a8 8 0 0 1-13.5 5.8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/><path d=\"M17.5 3v4h-4M6.5 21v-4h4\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            ["edit"] = "<path d=\"M4 20h4l10.5-10.5a2 2 0 0 0 0-2.8l-1.2-1.2a2 2 0 0 0-2.8 0L4 16v4z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\"/>",
            ["chart"] = "<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/>",
            ["gear"] = "<circle cx=\"12\" cy=\"12\" r=\"3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/><path d=\"M19.4 13a7.6 7.6 0 0 0 0-2l2-1.5-2-3.4-2.4.6a7.6 7.6 0 0 0-1.7-1L15 3h-6l-.3 2.7a7.6 7.6 0 0 0-1.7 1l-2.4-.6-2 3.4L4.6 11a7.6 7.6 0 0 0 0 2l-2 1.5 2 3.4 2.4-.6a7.6 7.6 0 0 0 1.7 1L9 21h6l.3-2.7a7.6 7.6 0 0 0 1.7-1l2.4.6 2-3.4-2-1.5z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.4\" stroke-linejoin=\"round\"/>",
            ["shield-users"] = "<path d=\"M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6l7-3z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\"/>",
            ["file-text"] = "<path d=\"M6 3h8l5 5v13a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\"/><path d=\"M8 12h8M8 16h8M8 8h3\" stroke=\"currentColor\" stroke-width=\"1.4\" stroke-linecap=\"round\"/>",
            ["bell"] = "<path d=\"M12 3.5a5 5 0 0 0-5 5V12c0 1.5-.6 2.4-1.4 3.3-.6.7-.1 1.7.8 1.7h11.2c.9 0 1.4-1 .8-1.7-.8-.9-1.4-1.8-1.4-3.3V8.5a5 5 0 0 0-5-5z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linejoin=\"round\"/><path d=\"M10 19a2 2 0 0 0 4 0\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/>",
            ["chevron-down"] = "<path d=\"M6 9l6 6 6-6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            ["user"] = "<circle cx=\"12\" cy=\"8\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/><path d=\"M4.5 20c0-4 3.4-7 7.5-7s7.5 3 7.5 7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"/>",
            ["key"] = "<circle cx=\"8\" cy=\"14\" r=\"3.3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/><path d=\"M10.3 11.7L18 4M15.5 6.5L18 9M18 4l2.5 2.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            ["logout"] = "<path d=\"M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M10 8l-4 4 4 4M2 12h13\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
        };

        public static IHtmlContent Get(string name)
        {
            var body = Paths.TryGetValue(name, out var svgBody) ? svgBody : Paths["dashboard"];
            return new HtmlString($"<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\" fill=\"currentColor\" aria-hidden=\"true\">{svgBody}</svg>");
        }
    }
}
