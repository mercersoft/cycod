using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Cycodlib.Core;

public class CycoDevProgramRunner : ProgramRunner
{
    private readonly IServiceProvider? _serviceProvider;

    public CycoDevProgramRunner(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var program = new CycoDevProgramRunner();
            return await program.RunProgramAsync(args);
        }
        finally
        {
            ShellSession.ShutdownAll();
        }
    }

    public static async Task<int> RunAsync(string[] args, IServiceProvider serviceProvider)
    {
        try
        {
            var program = new CycoDevProgramRunner(serviceProvider);
            return await program.RunProgramAsync(args);
        }
        finally
        {
            ShellSession.ShutdownAll();
        }
    }

    override protected bool ParseCommandLine(string[] args, out CommandLineOptions? commandLineOptions, out CommandLineException? ex)
    {
        return CycoDevCommandLineOptions.Parse(args, out commandLineOptions, out ex);
    }

    private CycoDevProgramInfo _programInfo = new();
}
