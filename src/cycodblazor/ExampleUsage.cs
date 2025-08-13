using Cycodlib.Core;
using Cycodlib.Core.Chat;
using Cycodlib.Core.Formatting;
using Cycodlib.Functions;

namespace Cycodblazor
{
    /// <summary>
    /// Example of how to use the components from cycodlib in Blazor
    /// </summary>
    public class ExampleUsage
    {
        public void DemonstrateUsage()
        {
            // Using CycoDevProgramInfo
            var programInfo = new CycoDevProgramInfo();
            
            // Using ChatHistoryDefaults
            bool useOpenAIFormat = ChatHistoryDefaults.UseOpenAIFormat;
            
            // Using TrajectoryFormatter
            string formattedUser = TrajectoryFormatter.FormatUserInput("Hello, AI!");
            string formattedAssistant = TrajectoryFormatter.FormatAssistantOutput("Hello! How can I help?");
            
            // Using DateAndTimeHelperFunctions
            var dateTimeHelper = new DateAndTimeHelperFunctions();
            string currentDate = dateTimeHelper.GetCurrentDate();
            string currentTime = dateTimeHelper.GetCurrentTime();
        }
    }
}