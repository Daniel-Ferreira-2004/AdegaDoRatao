using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração do fluxo de refresh token:
/// - login retorna um refresh token junto com o JWT;
/// - o refresh troca o par de tokens (rotação: o antigo é revogado);
/// - reusar um refresh token já rotacionado falha (anti-replay);
/// - revogar invalida o token (logout).
/// </summary>
public class RefreshTokenTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<string> LoginEObterRefreshTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.SenhaPadrao
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        return body!.RefreshToken;
    }

    [Fact]
    public async Task Login_DeveRetornarRefreshToken()
    {
        var client = factory.CreateClient();

        var refreshToken = await LoginEObterRefreshTokenAsync(client);

        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_ComTokenValido_DeveRetornarNovoParDeTokens()
    {
        var client = factory.CreateClient();
        var refreshToken = await LoginEObterRefreshTokenAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace()
            .And.NotBe(refreshToken, "a rotação emite um refresh token novo a cada uso");
    }

    [Fact]
    public async Task Refresh_ReusandoTokenRotacionado_DeveRetornarErro()
    {
        var client = factory.CreateClient();
        var refreshToken = await LoginEObterRefreshTokenAsync(client);
        await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        // Replay: o mesmo token já foi consumido e revogado na chamada acima.
        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_DeveInvalidarORefreshToken()
    {
        var client = factory.CreateClient();
        var refreshToken = await LoginEObterRefreshTokenAsync(client);

        var revoke = await client.PostAsJsonAsync("/api/v1/auth/revoke", new { refreshToken });
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ComTokenInexistente_DeveRetornarErro()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "token-inventado" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record LoginBody(string AccessToken, DateTime ExpiresAt, string RefreshToken,
        Guid UserId, string Name, string Role, IReadOnlyCollection<string> Permissions);
}
