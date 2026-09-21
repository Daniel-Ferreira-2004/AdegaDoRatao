using System.Net;
using System.Net.Http.Json;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdegaDoRatao.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração do endpoint GET /api/products/{id}/precos-mercado.
/// A API externa (Cnova Tech Data Market) é substituída por um fake que
/// simula os cenários de borda: timeout/falha total e rede sem o produto.
/// Em ambos os casos a resposta deve ser 200 com a indisponibilidade
/// sinalizada no corpo — nunca 500.
/// </summary>
public class PrecosMercadoTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<Guid> CriarProdutoComEanAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Category("Bebidas Teste " + Guid.NewGuid().ToString("N")[..8]);
        var marca = new Brand("Marca Teste " + Guid.NewGuid().ToString("N")[..8]);
        context.Categories.Add(categoria);
        context.Brands.Add(marca);
        // EAN único por teste: a factory compartilha o mesmo banco in-memory
        // entre os testes da classe e Products.Barcode tem constraint UNIQUE.
        var ean = "789" + Guid.NewGuid().ToString("N")[..10];
        var produto = new Product("Coca-Cola 2L", null, "SKU-" + Guid.NewGuid().ToString("N")[..8],
            ean, categoria.Id, marca.Id, "UN", 5.00m, 8.50m, 6, null);
        context.Products.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private WebApplicationFactory<Program> ComServicoExterno(IPrecoMercadoExternoService fake)
        => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPrecoMercadoExternoService));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddSingleton(fake);
        }));

    [Fact]
    public async Task PrecosMercado_QuandoApiExternaFalha_DeveRetornar200ComConsultaFalhou()
    {
        // Simula timeout da API externa: o serviço devolve Result.Failure
        // (mesmo contrato que o DataMarketPrecoService usa em timeout).
        var fake = new FakePrecoExterno(Result<ConsultaExternaPrecos>.Failure(
            "A consulta à API de preços de mercado excedeu o tempo limite."));
        var produtoId = await CriarProdutoComEanAsync();
        var autenticado = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.GerenteEmail);
        var client = ComServicoExterno(fake).CreateClient();
        client.DefaultRequestHeaders.Authorization = autenticado.DefaultRequestHeaders.Authorization;

        var response = await client.GetAsync($"/api/v1/products/{produtoId}/precos-mercado");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PrecoMercadoResponse>();
        body!.ConsultaFalhou.Should().BeTrue();
        body.Precos.Should().BeEmpty();
    }

    [Fact]
    public async Task PrecosMercado_RedeSemProduto_DeveRetornar200ComRedeIndisponivel()
    {
        IReadOnlyList<PrecoMercadoRedeResponse> precos = new PrecoMercadoRedeResponse[]
        {
            new("Veran", "Suzano", 7.99m, true, null),
            new("Shibata", "Suzano", null, false, "Preço indisponível nesta rede."),
            new("Atacadão", "Suzano", 7.49m, true, null),
            new("Semar", "Suzano", null, false, "Preço indisponível nesta rede."),
        };
        var fake = new FakePrecoExterno(Result<ConsultaExternaPrecos>.Success(
            new ConsultaExternaPrecos(precos, OrigemCache: false)));
        var produtoId = await CriarProdutoComEanAsync();
        var client = ComServicoExterno(fake).CreateClient();
        var autenticado = await factory.CreateAuthenticatedClientAsync(CustomWebApplicationFactory.GerenteEmail);
        client.DefaultRequestHeaders.Authorization = autenticado.DefaultRequestHeaders.Authorization;

        var response = await client.GetAsync($"/api/v1/products/{produtoId}/precos-mercado");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PrecoMercadoResponse>();
        body!.ConsultaFalhou.Should().BeFalse();
        body.Precos.Single(p => p.Rede == "Veran").Preco.Should().Be(7.99m);
        body.Precos.Single(p => p.Rede == "Shibata").Disponivel.Should().BeFalse();
    }

    private sealed class FakePrecoExterno(Result<ConsultaExternaPrecos> resultado) : IPrecoMercadoExternoService
    {
        public Task<Result<ConsultaExternaPrecos>> ConsultarPrecosAsync(
            string ean, string? cidade, string? estado,
            CancellationToken cancellationToken = default)
            => Task.FromResult(resultado);
    }
}
