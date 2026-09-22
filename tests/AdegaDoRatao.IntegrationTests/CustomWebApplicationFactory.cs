using System.Net.Http.Json;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Infrastructure.Auth;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdegaDoRatao.IntegrationTests;

/// <summary>
/// Factory que sobe a API inteira em memória para os testes de integração,
/// substituindo o SQL Server por um SQLite in-memory (banco relacional real,
/// que respeita as constraints e transações — diferente do provider InMemory).
///
/// Também faz o seed mínimo: perfis (ADMIN/GERENTE/OPERADOR), permissões e
/// um usuário de cada perfil, para os testes de autenticação/autorização.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Conexão SQLite mantida aberta durante toda a vida da factory: se fechar,
    // o banco in-memory é destruído.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public const string AdminEmail = "admin@adega.com";
    public const string GerenteEmail = "gerente@adega.com";
    public const string OperadorEmail = "operador@adega.com";
    public const string SenhaPadrao = "Senha@123";

    static CustomWebApplicationFactory()
    {
        // Em CI não há User Secrets: fornece uma connection string fictícia via
        // variável de ambiente só para o AddInfrastructure não falhar na validação.
        // O DbContext real é substituído pelo SQLite in-memory no ConfigureWebHost
        // — essa string nunca é usada de fato.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=testes;Username=teste;Password=teste");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove o DbContext configurado para SQL Server (se houver) e
            // substitui pelo SQLite in-memory.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        await SeedAsync(context);
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Seed mínimo: perfis, permissões e um usuário por perfil.
    /// A senha é hasheada com o mesmo hasher da produção (BCrypt).
    /// </summary>
    private static async Task SeedAsync(AppDbContext context)
    {
        var hasher = new BcryptPasswordHasher();
        var passwordHash = hasher.Hash(SenhaPadrao);

        // Permissões usadas nos testes
        var permissions = new[]
        {
            "products.read", "products.write",
            "stock.read", "stock.write",
            "sales.read", "sales.write",
            "purchases.read", "purchases.write",
            "financial.read", "financial.write",
            "dashboard.read"
        }.Select(code => new Permission(code, code)).ToArray();
        context.Permissions.AddRange(permissions);

        // Perfis
        var admin = new Role("ADMIN");
        var gerente = new Role("GERENTE");
        var operador = new Role("OPERADOR");
        context.Roles.AddRange(admin, gerente, operador);
        await context.SaveChangesAsync();

        // GERENTE recebe todas as permissões; OPERADOR só vendas/leitura
        foreach (var p in permissions)
        {
            gerente.ConcederPermissao(p.Id);
        }
        foreach (var code in new[] { "products.read", "stock.read", "sales.read", "sales.write" })
        {
            operador.ConcederPermissao(permissions.First(p => p.Code == code).Id);
        }

        // Usuários (um por perfil)
        context.Users.AddRange(
            new User("Admin Teste", AdminEmail, passwordHash, admin.Id),
            new User("Gerente Teste", GerenteEmail, passwordHash, gerente.Id),
            new User("Operador Teste", OperadorEmail, passwordHash, operador.Id));

        await context.SaveChangesAsync();
    }

    /// <summary>Atalho: autentica um usuário e devolve um HttpClient com o Bearer token.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password = SenhaPadrao)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    // DTO local para desserializar a resposta de login (evita depender do
    // projeto Application aqui só para isso).
    private sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAt, Guid UserId,
        string Name, string Role, IReadOnlyCollection<string> Permissions);
}
