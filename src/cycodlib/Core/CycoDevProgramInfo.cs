namespace Cycodlib.Core
{
    /// <summary>
    /// Provides program information for the Cycod application
    /// </summary>
    public class CycoDevProgramInfo : ProgramInfo
    {
        public CycoDevProgramInfo() : base(
            () => "cycod",
            () => "AI-powered Developer CLI",
            () => ".cycod",
            () => typeof(CycoDevProgramInfo).Assembly)
        {
        }
    }
}