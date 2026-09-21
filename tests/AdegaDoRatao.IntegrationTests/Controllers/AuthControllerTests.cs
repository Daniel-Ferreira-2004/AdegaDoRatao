using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração do fluxo de autenticação (UC01): login com
/// credenciais válidas gera token; inválidas retornam erro genérico
/// (sem detalhar qual campo está errado, por segurança).
/// </summary>
public class AuthControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.SenhaPadrao
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("accessToken");
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornarErro()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = "senha-errada"
        });

        // Erro de caso de uso → 400 (ProblemDetails), sem detalhar o campo errado
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_DeveRetornarErro()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "naoexiste@adega.com",
            password = "qualquer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_DeveRetornar401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
