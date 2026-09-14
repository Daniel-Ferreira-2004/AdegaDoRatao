# 11 — Etapa 5: Banco de dados

O projeto agora possui `AppDbContext`, mapeamentos EF Core para as entidades,
repositórios e `UnitOfWork` com transações para compra, venda e estoque.

## Antes de criar o banco

Configure a conexão fora do código, na pasta da API:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AdegaDoRatao;Trusted_Connection=True;TrustServerCertificate=True;"
```

Depois, crie e aplique a primeira migration:

```bash
dotnet ef migrations add InitialCreate --project src/AdegaDoRatao.Infrastructure --startup-project src/AdegaDoRatao.API
dotnet ef database update --project src/AdegaDoRatao.Infrastructure --startup-project src/AdegaDoRatao.API
```

O banco nunca deve ser criado com `EnsureCreated` em produção; migrations são
versionadas e aplicadas de modo controlado.
