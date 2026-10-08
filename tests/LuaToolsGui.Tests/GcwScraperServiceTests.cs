using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LuaToolsGui.Services;
using System;

namespace LuaToolsGui.Tests;

public class GcwScraperServiceTests
{
    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _responder;
        public int Calls { get; private set; }

        public StubHandler(Func<HttpRequestMessage, string> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var html = _responder(request);
            if (html == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(html)
            });
        }
    }

    [Fact]
    public async Task SearchFixesAsync_ReturnsEmptyWhenIndexDoesNotContainGame()
    {
        // Arrange
        var indexHtml = "<html><body>Some other game</body></html>";
        var handler = new StubHandler(_ => indexHtml);
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        // Act
        var results = await service.SearchFixesAsync("Ghost of Tsushima");

        // Assert
        Assert.Empty(results);
        Assert.Equal(4, handler.Calls); // Checks all 4 index files
    }

    [Fact]
    public async Task SearchFixesAsync_ParsesGameCopyWorldHtmlAndReturnsFixItems()
    {
        // Arrange
        var indexHtml = """
        <html><body>
            <a href="pc_crimson_desert.shtml">Crimson Desert</a>
            <a href="pc_other_game.shtml">Other Game</a>
        </body></html>
        """;

        var gameHtml = """
        <html><body>
            >Index<
            <table class="t8">
                <tr><td><b>Game Trainers &amp; Unlockers:</b></td></tr>
                <tr><td><a href="#Crimson Desert Trainer">Crimson Desert Trainer</a></td></tr>
                <tr><td><b>Game Fixes:</b></td></tr>
                <tr><td><a href="#Crimson Desert Fix">Crimson Desert Fix</a></td></tr>
            </table>

            <a name="Crimson Desert Trainer"></a>
            <a href='enable_javascript.shtml' onMouseDown="cbox('https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.v1.0.Trainer-FLiNG!rar'); return false;">MIRROR #01</a>
            
            <a name="Crimson Desert Fix"></a>
            <a href='enable_javascript.shtml' onMouseDown="cbox('https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.Fix!rar'); return false;">MIRROR #02</a>
        </body></html>
        """;

        var handler = new StubHandler(req => 
        {
            if (req.RequestUri!.ToString().Contains("gcw_index.shtml")) return indexHtml;
            if (req.RequestUri!.ToString().Contains("pc_crimson_desert.shtml")) return gameHtml;
            return null!;
        });
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        // Act
        var results = await service.SearchFixesAsync("Crimson Desert");

        // Assert
        Assert.Equal(2, results.Count);

        Assert.Equal("[Game Trainers & Unlockers] Crimson Desert Trainer", results[0].Title);
        Assert.Equal("https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.v1.0.Trainer-FLiNG!rar", results[0].MirrorPageUrl);

        Assert.Equal("[Game Fixes] Crimson Desert Fix", results[1].Title);
        Assert.Equal("https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.Fix!rar", results[1].MirrorPageUrl);
        
        Assert.Equal(2, handler.Calls); // Index + Game page
    }

    [Fact]
    public async Task GetDirectDownloadUrlAsync_ResolvesMirrorAndReturnsArchiveUrl()
    {
        var mirrorHtml = """
        <html><body>
            <a href="https://g1.gamecopyworld.com/?y=encoded_payload" rel="nofollow">MIRROR #01</a>
        </body></html>
        """;
        var handler = new StubHandler(_ => mirrorHtml);
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        var directUrl = await service.GetDirectDownloadUrlAsync("https://dl.gamecopyworld.com/?c=12345");

        Assert.Equal("https://g1.gamecopyworld.com/?y=encoded_payload", directUrl);
        Assert.Equal(1, handler.Calls);
    }
}

