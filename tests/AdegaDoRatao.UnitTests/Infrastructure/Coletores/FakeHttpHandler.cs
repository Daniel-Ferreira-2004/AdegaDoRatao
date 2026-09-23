using System.Net;
using System.Text;

namespace AdegaDoRatao.UnitTests.Infrastructure.Coletores;

/// <summary>
/// HttpMessageHandler falso para testar os coletores SEM rede: roteia
/// cada requisição para uma resposta pré-configurada conforme a URL.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly List<Func<HttpRequestMessage, HttpResponseMessage?>> _rotas = [];

    public IList<HttpRequestMessage> Requisicoes { get; } = new List<HttpRequestMessage>();

    public FakeHttpHandler Quando(string trechoUrl, string json,
        HttpStatusCode status = HttpStatusCode.OK, string contentType = "application/json")
    {
        _rotas.Add(req => req.RequestUri?.ToString().Contains(trechoUrl, StringComparison.OrdinalIgnoreCase) == true
            ? new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, contentType)
            }
            : null);
        return this;
    }

    public FakeHttpHandler QuandoHtml(string trechoUrl, string html)
        => Quando(trechoUrl, html, HttpStatusCode.OK, "text/html");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requisicoes.Add(request);
        foreach (var rota in _rotas)
        {
            var resposta = rota(request);
            if (resposta is not null)
            {
                return Task.FromResult(resposta);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    public HttpClient CriarClient(string baseAddress)
        => new(this) { BaseAddress = new Uri(baseAddress) };
}
