using System;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string rawGame = "Crimson Desert";
        string cleanGame = Regex.Replace(rawGame, "[^a-zA-Z0-9]", "").ToLowerInvariant();
        
        string acronym = "";
        var words = rawGame.Split(new char[] { ' ', ':', '-', '.', '_' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var w in words)
        {
            if (char.IsLetterOrDigit(w[0])) acronym += char.ToLowerInvariant(w[0]);
        }

        Console.WriteLine($"rawGame: '{rawGame}'");
        Console.WriteLine($"cleanGame: '{cleanGame}'");
        Console.WriteLine($"acronym: '{acronym}'");

        string subName = "CD";
        string cleanSub = Regex.Replace(subName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
        
        Console.WriteLine($"subName: '{subName}'");
        Console.WriteLine($"cleanSub: '{cleanSub}'");

        bool matchLevel1 = (cleanSub.Length > 2 && (cleanSub == cleanGame || cleanGame.StartsWith(cleanSub) || cleanSub.StartsWith(cleanGame))) ||
                           (acronym.Length >= 2 && cleanSub == acronym);

        Console.WriteLine($"matchLevel1: {matchLevel1}");
    }
}
