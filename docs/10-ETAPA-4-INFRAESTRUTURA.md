# 10 — Etapa 4: Infraestrutura

Esta etapa adiciona as implementações técnicas que não pertencem ao domínio:

- **BCrypt:** gera e confere hashes de senha. Senhas em texto puro não são
  persistidas nem expostas.
- **JWT:** gera tokens HMAC-SHA256 com identificador, e-mail, perfil e
  permissões. A chave deve ter ao menos 32 caracteres e ficar em User Secrets
  ou variável de ambiente.
- **Usuário atual:** adapta `HttpContext` para `ICurrentUserService`, mantendo
  os services da Application independentes de ASP.NET.
- **Injeção de dependência:** concentra o registro desses adaptadores no método
  `AddInfrastructure`.

Repositórios EF Core, contexto, mapeamentos e transações dependem da modelagem
do banco e serão adicionados na Etapa 5. Essa separação evita criar repositórios
antes de definir índices, relações, concorrência e filtros de exclusão lógica.
