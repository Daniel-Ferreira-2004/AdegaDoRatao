# 07 — Testes — Adega do Ratão

## 1. Ferramentas

- **xUnit** para testes unitários e de integração.
- **Moq** para dubles de interfaces (repositórios, serviços externos).
- **FluentAssertions** (adição justificada: legibilidade das asserções — não estava na lista original, mas é padrão de mercado com xUnit; se preferir manter só `Assert` nativo, é trivial remover).
- **WebApplicationFactory** + banco em memória/SQLite ou container de teste (Testcontainers) para testes de integração — decisão final de qual estratégia de banco de teste será tomada na implementação, priorizando SQLite in-memory pela simplicidade de setup local.

## 2. Testes Unitários (Application/Domain) — prioridade

- Cadastro de produto: SKU duplicado deve falhar; preço negativo deve falhar.
- Alteração de preço: deve gerar `AuditLog`.
- Entrada de estoque: deve aumentar `CurrentStock` e criar `StockMovement` tipo `ENTRADA`.
- Saída de estoque: deve reduzir `CurrentStock` e criar `StockMovement` tipo `SAIDA`.
- Venda sem estoque suficiente: deve lançar exceção de domínio/aplicação e não persistir nada.
- Criação de venda: deve baixar estoque de todos os itens e criar `FinancialTransaction` de entrada.
- Cancelamento de venda: deve estornar estoque e criar `FinancialTransaction` de saída de estorno.
- Criação de compra confirmada: deve dar entrada em estoque de todos os itens e criar `FinancialTransaction` de saída.
- Lançamento financeiro manual: deve respeitar tipo/categoria/valor > 0.
- Cálculo de saldo/fluxo de caixa: soma de entradas − saídas por período, casos de borda (período vazio, só entradas, só saídas).
- Autenticação: login com credenciais válidas gera token; credenciais inválidas retornam falha sem detalhar qual campo está errado.
- Autorização: usuário sem permissão recebe 403 ao chamar endpoint restrito (teste de integração).
- Validações: DTOs inválidos (nome vazio, quantidade ≤ 0, SKU duplicado) retornam 400 com mensagens claras.

## 3. Testes de Integração (API)

- Fluxo completo de uma venda via HTTP: login → criar produto → dar entrada de estoque → criar venda → validar estoque final e resposta 201.
- Fluxo de compra: criar fornecedor → criar compra → confirmar → validar estoque e financeiro.
- Autorização por perfil: OPERADOR não consegue acessar `/api/financial/*` (403); GERENTE consegue.
- Regras de unicidade retornando 409 (SKU duplicado, e-mail duplicado).

## 4. Organização

```text
tests/
├── AdegaDoRatao.UnitTests/
│   ├── Application/
│   │   ├── ProductServiceTests.cs
│   │   ├── StockServiceTests.cs
│   │   ├── SaleServiceTests.cs
│   │   ├── PurchaseServiceTests.cs
│   │   └── FinancialServiceTests.cs
│   └── Domain/
│       └── ProductTests.cs
└── AdegaDoRatao.IntegrationTests/
    └── Controllers/
        ├── AuthControllerTests.cs
        ├── SalesControllerTests.cs
        └── PurchasesControllerTests.cs
```

## 5. Critério de Aceite por Etapa

Uma etapa da implementação só é considerada concluída quando: compila sem warnings relevantes, os testes relacionados àquela etapa passam, e nenhum teste de etapas anteriores regrediu (`dotnet test` executado no ciclo completo, não apenas nos testes novos).
