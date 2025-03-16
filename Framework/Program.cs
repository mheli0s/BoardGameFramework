using System;
using System.Text;
using Framework.Core;
namespace Framework;

/* Program entry point to launch the boardgame framework */

public class Program
{
    public static void Main()
    {
        // set Console output encoding to UTF8 to support Unicode and ASCII for better display compatibility
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleUI.DisplayLoadingMessage(15, "Starting Boardgame Framework..."); // 1.5 second loading indicator

        Console.Clear();

        // create the framework Singleton and start it 
        BoardGameFramework _framework = BoardGameFramework.GetBGFInstance();
        _framework.StartFramework();
    }
}
