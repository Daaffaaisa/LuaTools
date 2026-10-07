using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LuaToolsGui.Services;
using LuaToolsGui.Models;

namespace LuaToolsGui.Tests;

public class GcwScraperServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(string body)
        {
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) });
        }
    }

    [Fact]
    public async Task SearchFixesAsync_ParsesGameCopyWorldHtmlAndReturnsFixItems()
    {
        // Arrange: Fake HTML that mimics a GCW search result page
        var fakeHtml = """
        <html>
        <body>
            <table class="gcw-list">
                <tr>
                    <td class="item-title"><a href="/games/cyberpunk_2077.shtml#v1.12">Cyberpunk 2077 v1.12 [MULTI] Fixed EXE</a></td>
                    <td class="item-date">01-Jan-2023</td>
                    <td class="item-size">14 MB</td>
                </tr>
            </table>
        </body>
        </html>
        """;
        var handler = new StubHandler(fakeHtml);
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        // Act
        var results = await service.SearchFixesAsync("Cyberpunk 2077");

        // Assert
        Assert.Single(results);
        Assert.Equal("Cyberpunk 2077 v1.12 [MULTI] Fixed EXE", results[0].Title);
        Assert.Equal("01-Jan-2023", results[0].DateLabel);
        Assert.Equal("14 MB", results[0].Size);
        Assert.Equal("https://gamecopyworld.com/games/cyberpunk_2077.shtml#v1.12", results[0].MirrorPageUrl);
        
        // Ensure proper User-Agent was sent to bypass basic blocks
        Assert.Contains("Windows NT 10.0", handler.LastRequest!.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task GetDirectDownloadUrlAsync_ResolvesMirrorAndReturnsArchiveUrl()
    {
        var fakeMirrorHtml = "<html><body><div id=\"download_link\"><a href=\"http://fake-mirror.com/fix.rar\">Click Here</a></div></body></html>";
        var handler = new StubHandler(fakeMirrorHtml);
        var client = new HttpClient(handler);
        var service = new GcwScraperService(client);

        var url = await service.GetDirectDownloadUrlAsync("https://gamecopyworld.com/games/mirror.shtml");
        
        Assert.Equal("http://fake-mirror.com/fix.rar", url);
    }
}

