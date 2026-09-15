# Adega do Ratão — Frontend

Frontend do sistema de gestão (ERP/PDV) da **Adega do Ratão**, consumindo a API REST em ASP.NET Core (`src/AdegaDoRatao.API`).

## Tecnologias

- React 19 + TypeScript + Vite
- React Router 7 (rotas protegidas por autenticação e permissão)
- TanStack Query (cache, mutations, invalidação)
- Axios (camada HTTP centralizada com interceptors)
- Tailwind CSS 3 (tema claro/escuro, identidade âmbar)
- Radix UI (dialog, dropdown, tabs) + Lucide (ícones)
- React Hook Form + Zod (formulários e validação)
- Recharts (gráficos do dashboard e fluxo de caixa)
- Vitest (testes) e oxlint (lint)

## Requisitos

- Node.js 20+
- API .NET em execução (por padrão `http://localhost:55961` / `https://localhost:55960`)

## Configuração

```bash
cp .env.example .env
```

| Variável | Descrição | Padrão |
|---|---|---|
| `VITE_API_URL` | URL base da API (sem barra final) | `http://localhost:55961/api` |

## Execução

```bash
npm install
npm run dev      # desenvolvimento
npm run build    # build de produção (tsc + vite)
npm run lint     # oxlint
npm run test     # testes unitários (vitest)
```

## Estrutura

```
src/
├── components/   # ui (primitivos), layout, forms, feedback, common
├── config/       # variáveis de ambiente
├── contexts/     # AuthContext (sessão + permissões)
├── hooks/        # useDebounce
├── layouts/      # AppLayout (sidebar + header)
├── pages/        # uma pasta por módulo
├── routes/       # guards (RequireAuth, RequirePermission)
├── services/     # api/apiClient + um serviço por módulo
├── types/        # DTOs da API (camelCase, enums como inteiros)
└── utils/        # formatadores e mapas de enums
```

## Integração com a API

- Toda comunicação HTTP passa por `src/services/api/apiClient.ts` (baseURL via env, Bearer token, timeout, normalização de erros ProblemDetails → `ApiError`).
- Login: `POST /api/auth/login` → sessão (token, expiração, role, permissões) persistida em `localStorage`. Não há refresh token: em 401 a sessão é encerrada e o usuário volta ao login.
- Autorização: a UI esconde rotas/ações conforme `permissions` do login (`products.read`, `sales.write`, etc.); a segurança real é sempre validada pela API.
- Enums são inteiros no JSON (ex.: `SaleStatus`: 1=Concluída, 2=Cancelada) — mapeados em `src/utils/enums.ts`.
- Paginação: `page`/`pageSize` → `{ items, page, pageSize, totalCount, totalPages, hasNextPage }`.

## Rotas principais

| Rota | Módulo | Permissão |
|---|---|---|
| `/` | Dashboard | `dashboard.read` |
| `/produtos` | Produtos | `products.read` |
| `/categorias` | Categorias e marcas | `products.read` |
| `/estoque`, `/estoque/movimentar` | Estoque | `stock.read` / `stock.write` |
| `/vendas`, `/vendas/nova` | Vendas (PDV) | `sales.read` / `sales.write` |
| `/compras/nova` | Compras | `purchases.write` |
| `/fornecedores` | Fornecedores | `purchases.read` |
| `/financeiro`, `/financeiro/fluxo-de-caixa`, `/financeiro/categorias` | Financeiro | `financial.read` |
| `/formas-de-pagamento` | Formas de pagamento | `sales.read` |
| `/auditoria` | Auditoria | `audit.read` (ADMIN) |

## Pendências (aguardando backend)

- **Clientes**: não há endpoints/entidade de clientes na API.
- **Gestão de usuários**: não há endpoints de usuários.
- **Listagem de compras**: a API só permite criar/confirmar/cancelar.
- **Contas a pagar/receber com vencimento**: o financeiro atual usa transações Entrada/Saída sem data de vencimento.
- **Relatórios/exportação**: não há endpoints de exportação.
