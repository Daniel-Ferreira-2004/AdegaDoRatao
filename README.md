# Adega do Ratão

Sistema de gestão para uma adega/loja de bebidas: produtos, estoque, compras, vendas, financeiro, dashboard e usuários — construído como uma API REST em .NET (Clean Architecture), pronta para ser consumida por qualquer cliente (web ou mobile).

## Tecnologias

- C# / .NET 8 (LTS)
- ASP.NET Core Web API
- Entity Framework Core + PostgreSQL (Npgsql) — funciona com Supabase, Neon, Railway, Azure ou Postgres local/Docker
- JWT (autenticação) + BCrypt (hash de senha)
- FluentValidation
- Serilog (logging)
- Swagger / OpenAPI
- xUnit + Moq + FluentAssertions (testes)

## Arquitetura

Clean Architecture em 4 camadas — ver `docs/01-ARQUITETURA.md` para o detalhamento completo.

```text
src/
├── AdegaDoRatao.Domain          # Entidades, enums, regras de domínio (sem dependências)
├── AdegaDoRatao.Application     # Casos de uso, DTOs, validações
├── AdegaDoRatao.Infrastructure  # EF Core, JWT, hashing, logging
└── AdegaDoRatao.API             # Controllers, middlewares, composição da aplicação

tests/
├── AdegaDoRatao.UnitTests
└── AdegaDoRatao.IntegrationTests
```

## Pré-requisitos

- .NET SDK 8.0+
- Um banco PostgreSQL — recomendado: [Supabase](https://supabase.com) (plano grátis, sem cartão, acessível de qualquer lugar)

## Configuração (desenvolvimento)

Os segredos (connection string, chave JWT) **não** ficam no `appsettings.json`. Configure via User Secrets a partir da pasta `src/AdegaDoRatao.API`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=db.SEU-PROJETO.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SUA-SENHA;SSL Mode=Require"
dotnet user-secrets set "Jwt:Secret" "uma-chave-bem-grande-e-aleatoria-aqui"
```

### Criando o banco grátis no Supabase

1. Crie uma conta em [supabase.com](https://supabase.com) (pode usar login do GitHub);
2. Clique em **New project**, escolha nome, senha do banco e região (ex.: `South America (São Paulo)`);
3. No painel do projeto, vá em **Project Settings → Database → Connection string** e copie a string no formato **URI/Npgsql** (ou monte com Host `db.SEU-PROJETO.supabase.co`, porta `5432`, usuário `postgres` e a senha que você definiu);
4. Use essa string no comando `user-secrets` acima.

## Executando

```bash
dotnet restore
dotnet build
dotnet ef database update --project src/AdegaDoRatao.Infrastructure --startup-project src/AdegaDoRatao.API
dotnet run --project src/AdegaDoRatao.API
```

Swagger disponível em `https://localhost:{porta}/swagger` após subir a API.

## Testes

```bash
dotnet test
```

## Status do desenvolvimento

- [x] Etapa 1 — Solution e estrutura dos projetos
- [x] Etapa 2 — Domain
- [x] Etapa 3 — Application
- [x] Etapa 4 — Infrastructure
- [x] Etapa 5 — Banco de dados e Entity Framework
- [x] Etapa 6 — Autenticação e autorização
- [x] Etapa 7 — Produtos, categorias e marcas
- [x] Etapa 8 — Estoque
- [x] Etapa 9 — Fornecedores e compras
- [x] Etapa 10 — Vendas
- [x] Etapa 11 — Financeiro
- [x] Etapa 12 — Dashboard e relatórios
- [x] Etapa 13 — Auditoria
- [x] Etapa 14 — Tratamento global de erros e logging
- [x] Etapa 15 — Testes unitários
- [x] Etapa 16 — Testes de integração
- [x] Etapa 17 — Swagger/OpenAPI
- [x] Etapa 18 — README e documentação final

## Documentação

Toda a documentação do projeto está na pasta [`docs/`](docs/):

| Arquivo | Conteúdo |
|---|---|
| [01-ARQUITETURA.md](docs/01-ARQUITETURA.md) | Visão geral da Clean Architecture adotada |
| [02-REQUISITOS.md](docs/02-REQUISITOS.md) | Requisitos funcionais e não funcionais |
| [03-REGRAS-DE-NEGOCIO.md](docs/03-REGRAS-DE-NEGOCIO.md) | Regras de negócio (RN01–RN36) |
| [04-BANCO-DE-DADOS.md](docs/04-BANCO-DE-DADOS.md) | Modelo de dados e mapeamentos |
| [05-API.md](docs/05-API.md) | Contratos dos endpoints |
| [06-SEGURANCA.md](docs/06-SEGURANCA.md) | Autenticação JWT, perfis e permissões |
| [07-TESTES.md](docs/07-TESTES.md) | Estratégia de testes |
| [08-ROADMAP.md](docs/08-ROADMAP.md) | Roadmap e etapas do projeto |
| 09–16 | Detalhamento das etapas 3 a 10 |

## Suporte a mobile

A API é REST/JSON stateless com JWT, então qualquer app mobile (React Native, Flutter, nativo) consome diretamente. Decisões pensadas para mobile:

- **CORS configurável** por origem via `AllowedOrigins` no `appsettings.json` (ou variável de ambiente);
- **Paginação** em todas as listagens (`page`/`pageSize`), essencial em rede móvel;
- **Dashboard agregado** (`GET /api/dashboard`) que retorna vendas, financeiro e estoque em uma única chamada;
- **Payloads enxutos** com DTOs de resposta separados dos de requisição.

## Autenticação

1. `POST /api/auth/login` com e-mail e senha retorna um token JWT;
2. Envie o token no header `Authorization: Bearer {token}` nas demais chamadas;
3. No Swagger, clique em **Authorize** e informe `Bearer {token}` para testar os endpoints protegidos.

Perfis e permissões são dados (tabelas `Roles`/`Permissions`), não enum fixo — ver [06-SEGURANCA.md](docs/06-SEGURANCA.md).
