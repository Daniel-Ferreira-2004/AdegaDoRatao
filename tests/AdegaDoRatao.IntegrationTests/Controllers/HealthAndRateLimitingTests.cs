using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração do hardening da API:
/// - GET /health deve responder 200 com a aplicação e o banco saudáveis;
/// - POST /api/v1/auth/login é limitado a 5 tentativas/minuto por IP
///   (a 6ª deve retornar 429 Too Many Requests).
///
/// Estes testes usam uma factory própria (IClassFixture por classe), então o
/// contador do rate limiter começa zerado e não interfere nos demais testes.
/// </summary>
public class HealthAndRateLimitingTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Health_ComBancoDisponivel_DeveRetornar200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Login_AposLimiteDeTentativas_DeveRetornar429()
    {
        var client = factory.CreateClient();
        var payload = new { email = "forca-bruta@adega.com", password = "qualquer" };

        // Política "login": 5 requisições permitidas por janela de 1 minuto.
        for (var i = 0; i < 5; i++)
        {
            var tentativa = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
            tentativa.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "as 5 primeiras tentativas passam pelo rate limiter e falham na credencial");
        }

        var bloqueada = await client.PostAsJsonAsync("/api/v1/auth/login", payload);

        bloqueada.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "a 6ª tentativa na mesma janela deve ser bloqueada pelo rate limiter");
    }
}
