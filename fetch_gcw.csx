using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
client.DefaultRequestHeaders.Add("Referer", "https://gamecopyworld.com/");

var res = await client.GetAsync("https://gamecopyworld.com/games/gcw_index.shtml?search=Crimson+Desert");
Console.WriteLine("Status: " + res.StatusCode);
var html = await res.Content.ReadAsStringAsync();
Console.WriteLine("Length: " + html.Length);

// Print out any matches
var matches = Regex.Matches(html, @"(?i)<a[^>]+href=""([^""]+)""[^>]*>([^<]*Crimson[^<]*)</a>");
foreach(Match m in matches)
{
    Console.WriteLine($"Link: {m.Groups[1].Value}, Text: {m.Groups[2].Value}");
}
