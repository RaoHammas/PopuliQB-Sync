using System.Xml;

namespace PopuliQB_Tool.Helpers;

public static class PQExtensions
{
    public static List<string> DivideIntoEqualParts(this string input, int maxLength)
    {
        var dividedStrings = new List<string>();

        var length = input.Length;

        // Calculate the number of parts needed
        var numParts = (int)Math.Ceiling((double)length / maxLength);

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

    public static string GetXmlNodeValue(string xmlString, string nodePath)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        var statusMessageNode = xmlDoc.SelectSingleNode(nodePath);
        if (statusMessageNode is { Value: not null })
        {
            return statusMessageNode.Value;
        }
        
        return "";
    }
}