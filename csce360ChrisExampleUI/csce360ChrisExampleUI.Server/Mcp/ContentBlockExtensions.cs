using ModelContextProtocol.Protocol;

namespace csce360ChrisExampleUI.Server.Mcp
{
    // Newer MCP SDK versions replaced ContentBlock.Text with a discriminated
    // hierarchy (text vs image vs other content types), so .Text no longer
    // exists on the base type. This centralizes the "give me the text if
    // there is any" extraction in one place.
    //
    // NOTE: verify the derived type name against your installed version's
    // IntelliSense/source — it's commonly TextContentBlock, but confirm by
    // typing "ContentBlock" and letting autocomplete show you the actual
    // derived types (or check what CallToolAsync's result.Content elements
    // resolve to when you hover in the IDE).
    public static class ContentBlockExtensions
    {
        public static string? AsText(this ContentBlock block)
        {
            return block switch
            {
                TextContentBlock text => text.Text,
                _ => null
            };
        }
    }
}