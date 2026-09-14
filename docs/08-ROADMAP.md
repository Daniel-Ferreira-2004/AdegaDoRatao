# 08 — Roadmap — Adega do Ratão

## 1. Estratégia de Deploy

- **Desenvolvimento**: execução local (`dotnet run`), SQL Server via container Docker local ou instância local/LocalDB.
- **Produção (proposta inicial)**: container Docker da API + SQL Server gerenciado (Azure SQL ou equivalente); pipeline CI/CD (GitHub Actions) executando `dotnet restore/build/test` e, em sucesso, aplicando migrations e publicando a imagem.
- Migrations aplicadas de forma controlada no pipeline (nunca automaticamente a partir da própria API em produção, para evitar migrations acidentais).

## 2. Variáveis de Ambiente (produção)

```text
ConnectionStrings__DefaultConnection
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationMinutes
Serilog__MinimumLevel
AllowedOrigins            # CORS, lista separada por vírgula
AllowNegativeStock        # default global, override por produto
```

## 3. Roadmap de Evoluções Futuras

- Refresh token e revogação de sessão.
- Rate limiting e proteção contra força bruta no login.
- Multi-loja/multi-filial (adicionar `StoreId` transversal).
- Múltiplos perfis por usuário (o modelo `RolePermission` já comporta essa evolução sem quebra de contrato).
- Conciliação financeira/bancária mais robusta (import OFX, status de pagamento com baixa parcial).
- Relatórios exportáveis (PDF/Excel) além do dashboard via API.
- Cache (Redis) para consultas de dashboard de alta frequência.
- Frontend React/TypeScript consumindo a API (Fase futura, fora deste backend).
- Notificações (e-mail/push) para estoque baixo.
- Internacionalização de mensagens de erro/validação, se necessário.
- CQRS/MediatR caso a complexidade de casos de uso cresça a ponto de justificar a separação de comandos e consultas — não adotado na v1 por não haver necessidade comprovada ainda.

## 4. Resumo de Fase 1

```text
ARQUITETURA DEFINIDA
MÓDULOS DEFINIDOS
BANCO DEFINIDO
API DEFINIDA
REGRAS DE NEGÓCIO DEFINIDAS
TESTES PLANEJADOS

PRONTO PARA IMPLEMENTAÇÃO
```

Aguardando confirmação ("APROVADO. PODE IMPLEMENTAR.") para iniciar a Etapa 1 (Solution e estrutura dos projetos) da Fase 2, conforme a ordem de implementação definida.
