using System;
using Framework.Enums;
namespace Framework.Core;

/* Handles console-based validation for framework menus and generic empty inputs */

public class InputValidator
{

    // returns true if input entered wasn't null or whitespace chars
    internal static bool IsEmptyInput(string input)
    {
        return string.IsNullOrWhiteSpace(input);
    }

    // returns true if a valid option from FrameworkMenuOption enum is selected
    internal static bool IsValidFrameworkMenuOption(string input, out string menuChoice)
    {
        if (Enum.TryParse(input, out FrameworkMenuOption option))
        {
            menuChoice = option.ToString();
            return Enum.IsDefined(option);
        }
        else
        {
            menuChoice = string.Empty; // default to empty if invalid input entered 
            return false;
        }
    }

    // returns true if the game menu input entered is in range
    internal static bool IsValidGameMenuOption(string input, out int menuChoice)
    {
        return int.TryParse(input, out menuChoice)
                    && menuChoice >= 0 && menuChoice <= 3;
    }

    // returns true if a valid menu choice was input and save choice in an out variable
    internal static bool IsValidGameOverMenuOption(string input, out char menuChoice)
    {
        return char.TryParse(input, out menuChoice)
                    && menuChoice == 'r' && menuChoice == 'q' && menuChoice == 'e';
    }
}
