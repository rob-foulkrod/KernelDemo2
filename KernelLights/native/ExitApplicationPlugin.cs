using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;
using System.ComponentModel;
using System.Threading.Tasks;

public class ExitApplicationPlugin
{

    [KernelFunction("exit_Plugin")]
    [Description("Exits the application. Can be used when alfred is dismissed")]
    public Task ExitApplicationAsync(string finalMessage)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(finalMessage);
        Console.ResetColor();
        Console.ReadKey();

        Environment.Exit(0);
        return Task.CompletedTask;
    }
}
