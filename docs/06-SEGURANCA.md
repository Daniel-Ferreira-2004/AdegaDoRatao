# 06 — Segurança — Adega do Ratão

## 1. Autenticação

- JWT assinado (HMAC-SHA256) emitido em `POST /api/auth/login`.
- Claims: `sub` (UserId), `name`, `role`, `permissions` (lista resolvida no login).
- Expiração configurável (`Jwt:ExpirationMinutes`, padrão 60 min). Renovação futura via refresh token (ver roadmap).
- Senhas com hash via BCrypt (custo configurável), nunca texto puro, nunca reversível.

## 2. Autorização

- Baseada em **policies** derivadas de `Permission` (não apenas `[Authorize(Roles = ...)]`), permitindo granularidade por recurso/ação sem acoplar o código ao nome do perfil.
- Mapeamento inicial de permissões por perfil:
  - **ADMIN**: todas as permissões.
  - **GERENTE**: `products.*`, `stock.*`, `sales.*`, `purchases.*`, `financial.*`, `reports.read`.
  - **OPERADOR**: `sales.write`, `sales.read`, `products.read`, `stock.read`.
- Novos perfis/permissões são dados (tabela `Permission`/`RolePermission`), não exigem deploy de código.

## 3. Proteção de Secrets

- Desenvolvimento: **User Secrets** (`dotnet user-secrets`) para connection string, `Jwt:Secret`.
- Produção: **variáveis de ambiente** (ou um cofre de secrets do provedor de nuvem, se disponível).
- Nenhum secret entra no Git; `appsettings.json` versionado contém apenas placeholders/estrutura, nunca valores reais; `.env.example` documenta as chaves esperadas sem valores.

## 4. Tratamento de Erros e Exposição de Dados

- `ExceptionHandlingMiddleware` global captura exceções e converte em `ProblemDetails`.
- Em produção, mensagens genéricas para erros 500 (sem stack trace); em desenvolvimento, detalhe completo liberado via `IHostEnvironment.IsDevelopment()`.
- Nenhum dado sensível (senha, token, secrets) é logado.

## 5. Auditoria

- `AuditLog` gravado automaticamente (via interceptor do EF Core ou serviço explícito nos Services) para: alteração de preço, movimentação de estoque, criação/edição/exclusão lógica de produto/categoria/marca/fornecedor/usuário, venda, compra, lançamento financeiro manual.
- Registro imutável: sem endpoint de update/delete para `AuditLog`.
- Consulta restrita a ADMIN/GERENTE (`GET /api/audit-logs`).

## 6. Outras Práticas

- Validação de entrada em toda borda da API (FluentValidation) — nunca confiar apenas em validação client-side.
- Rate limiting básico no login (mitigar força bruta) — reservado para roadmap se não crítico na v1.
- HTTPS obrigatório (`UseHttpsRedirection`), HSTS em produção.
- Cabeçalhos de segurança básicos (`X-Content-Type-Options`, etc.) via middleware.
