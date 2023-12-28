using System.Text.RegularExpressions;

namespace PopuliQB_Tool.Helpers;

public static class PqExtensions
{
    public static string RemoveInvalidUnicodeCharacters(this string input)
    {
        // Define a regular expression pattern to match valid characters
        var pattern = @"[^\p{L}\p{N}\s]";

        // Use Regex to replace invalid characters with an empty string
        var result = Regex.Replace(input, pattern, string.Empty);

        return result;
    }

    public static List<string> DivideIntoEqualParts(this string input, int maxLength)
    {
        var dividedStrings = new List<string>();

        var length = input.Length;

        // Calculate the number of parts needed
        var numParts = (int)Math.Ceiling((double)length / --maxLength);

        // Calculate the length for each part
        var partLength = length / numParts;

        // Divide the string into equal parts
        for (var i = 0; i < numParts; i++)
        {
            var startIndex = i * partLength;
            var endIndex = Math.Min(startIndex + partLength, length);
            var part = input.Substring(startIndex, endIndex - startIndex);
            dividedStrings.Add(part);
        }

        return dividedStrings;
    }

    public static string GetXmlNodeValue(string xmlString)
    {
        var pattern = @"statusMessage\s*=\s*""([^""]*)""";
        var match = Regex.Match(xmlString, pattern);

        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        return "";
    }

}