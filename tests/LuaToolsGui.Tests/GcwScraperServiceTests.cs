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
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _route;
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(Func<HttpRequestMessage, string> route)
        {
            _route = route;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastRequest = request;
            var body = _route(request);
            if (body == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public async Task SearchFixesAsync_ParsesGameCopyWorldHtmlAndReturnsFixItems()
    {
        // Arrange: Fake HTML that mimics real GCW structure (Index -> Game Page -> Mirrors)
        var indexHtml = """
        <html><body>
            <a href="pc_crimson_desert.shtml">Crimson Desert</a>
            <a href="pc_other_game.shtml">Other Game</a>
        </body></html>
        """;

        var gameHtml = """
        <html><body>
            <a href='enable_javascript.shtml' onMouseDown="cbox('https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.v1.0.Trainer-FLiNG!rar'); return false;">MIRROR #01</a>
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
        Assert.Equal(1, results.Count);

        Assert.Equal("Crimson Desert Fix", results[0].Title);
        Assert.Equal("https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.Fix!rar", results[0].MirrorPageUrl);
        
        Assert.Equal(2, handler.Calls); // Index + Game page
    }

    [Fact]
    public async Task GetDirectDownloadUrlAsync_ResolvesMirrorAndReturnsArchiveUrl()
    {
        // The mirror page on dl.gamecopyworld.com contains the actual g1.gamecopyworld.com mirror link
        var mirrorHtml = """
        <html><body>
            <a href="https://g1.gamecopyworld.com/?y=encoded_payload" rel="nofollow">MIRROR #01</a>
        </body></html>
        """;
        var handler = new StubHandler(_ => mirrorHtml);
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        // Act
        var result = await service.GetDirectDownloadUrlAsync("https://dl.gamecopyworld.com/?c=123&f=test!rar");

        // Assert
        Assert.Equal("https://g1.gamecopyworld.com/?y=encoded_payload", result);
        Assert.Equal("https://dl.gamecopyworld.com/?c=123&f=test!rar", handler.LastRequest!.Headers.Referrer?.ToString());
    }
}
