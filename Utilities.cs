
namespace Recipeasy;

public class Utilities 
{
    public static List<String> TokenizeString(String inputString, char[] delimiters) 
    {
        List<String> tokenizedStrings = new List<String>();
        String currentToken = "";
        for (int i = 0; i  < inputString.Length; ++i)
        {
            char currentLetter = inputString[i];
            
            if (delimiters.Any(e => e == currentLetter))
            {
                if (currentToken != "")
                {
                    tokenizedStrings.Add(currentToken);
                    currentToken = "";
                }

            } 
            else 
            {
                currentToken += currentLetter;
            }
        }

        if (currentToken != "")
            tokenizedStrings.Add(currentToken);

        return tokenizedStrings;
    }
}