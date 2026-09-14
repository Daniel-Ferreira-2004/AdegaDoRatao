using System.Net;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.IntegrationTests.Controllers;

/// <summary>
/// Testes de autorização por perfil (RN30/RN31): OPERADOR não acessa o
/// módulo financeiro; GERENTE acessa. Valida o comportamento das policies
/// configuradas no Program.cs.
/// </summary>
public class AuthorizationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Operador_NaoDeveAcessarFinanceiro()
    {
        var client = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.OperadorEmail);

        var response = await client.GetAsync("/api/financial/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Gerente_DeveAcessarFinanceiro()
    {
        var client = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.GerenteEmail);

        var response = await client.GetAsync("/api/financial/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_DeveAcessarAuditoria()
    {
        var client = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.AdminEmail);

        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Gerente_NaoDeveAcessarAuditoria()
    {
        // RN31: auditoria é restrita a ADMIN
        var client = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.GerenteEmail);

        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Operador_DeveAcessarVendas()
    {
        var client = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.OperadorEmail);

        var response = await client.GetAsync("/api/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
