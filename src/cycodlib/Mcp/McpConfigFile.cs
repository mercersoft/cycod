namespace Cycodlib.Mcp
{
    /// <summary>
    /// Simplified MCP configuration file representation
    /// </summary>
    public class McpConfigFile
    {
        /// <summary>
        /// The file name of this configuration
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The servers defined in this configuration file
        /// </summary>
        public Dictionary<string, IMcpServerConfigItem> Servers { get; set; } = new Dictionary<string, IMcpServerConfigItem>();
    }
}