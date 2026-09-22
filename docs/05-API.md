# 05 — API — Adega do Ratão

API RESTful, JSON, documentada via Swagger/OpenAPI, protegida por JWT (Bearer).

## 1. Convenções

- Versionamento na URL: todas as rotas sob `/api/v1/`.
- Recursos no plural, kebab/lowercase: `/api/v1/products`.
- Paginação: `?page=1&pageSize=20` com resposta `PagedResult<T>` (`items`, `totalCount`, `page`, `pageSize`).
- Filtros via query string (`?name=`, `?sku=`, `?categoryId=`, `?status=`).
- Datas em ISO 8601 UTC.
- Erros no formato `ProblemDetails` (RFC 7807).

## 2. Endpoints

```text
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh             (rotaciona o refresh token)
POST   /api/v1/auth/revoke              (logout: revoga o refresh token)

GET    /api/v1/products
GET    /api/v1/products/{id}
GET    /api/v1/products/search?query=
POST   /api/v1/products
PUT    /api/v1/products/{id}
PATCH  /api/v1/products/{id}/status     (ativar/desativar)
PATCH  /api/v1/products/{id}/price
GET    /api/v1/products/{id}/history

GET    /api/v1/categories
GET    /api/v1/categories/{id}
POST   /api/v1/categories
PUT    /api/v1/categories/{id}
PATCH  /api/v1/categories/{id}/status

GET    /api/v1/brands
GET    /api/v1/brands/{id}
POST   /api/v1/brands
PUT    /api/v1/brands/{id}
PATCH  /api/v1/brands/{id}/status

GET    /api/v1/stock/movements?productId=&from=&to=
POST   /api/v1/stock/movements           (entrada/saída/ajuste manual)
GET    /api/v1/stock/low                 (abaixo do mínimo)
GET    /api/v1/stock/out-of-stock

GET    /api/v1/suppliers
GET    /api/v1/suppliers/{id}
POST   /api/v1/suppliers
PUT    /api/v1/suppliers/{id}
PATCH  /api/v1/suppliers/{id}/status

GET    /api/v1/purchases
GET    /api/v1/purchases/{id}
POST   /api/v1/purchases
POST   /api/v1/purchases/{id}/confirm
POST   /api/v1/purchases/{id}/cancel

GET    /api/v1/sales
GET    /api/v1/sales/{id}
POST   /api/v1/sales
POST   /api/v1/sales/{id}/cancel

GET    /api/v1/payment-methods
POST   /api/v1/payment-methods

GET    /api/v1/financial/categories
POST   /api/v1/financial/categories
GET    /api/v1/financial/transactions?from=&to=&type=
POST   /api/v1/financial/transactions
GET    /api/v1/financial/cash-flow?period=

GET    /api/v1/dashboard/summary
GET    /api/v1/dashboard/top-products
GET    /api/v1/dashboard/payment-methods-usage

GET    /api/v1/users
POST   /api/v1/users
PUT    /api/v1/users/{id}
PATCH  /api/v1/users/{id}/status

GET    /api/v1/roles
GET    /api/v1/roles/{id}/permissions
PUT    /api/v1/roles/{id}/permissions

GET    /api/v1/audit-logs?entityName=&entityId=

GET    /health                          (health check: 200 = API + banco OK)
```

## 3. Autenticação e Autorização na API

- `POST /api/v1/auth/login` recebe `{ email, password }`, retorna `{ accessToken, expiresAt, refreshToken, userId, name, role, permissions }`.
- O `refreshToken` (vida longa, 30 dias) permite obter um novo JWT sem nova senha via `POST /api/v1/auth/refresh`. A cada uso o refresh token é rotacionado (o anterior é revogado); reuso de token revogado é recusado (anti-replay). Apenas o hash SHA-256 do token é persistido.
- `POST /api/v1/auth/revoke` invalida o refresh token (logout).
- Todos os demais endpoints exigem header `Authorization: Bearer {token}`.
- Autorização por policy baseada em `Permission` (ex.: `[Authorize(Policy = "products.write")]`), resolvida a partir das claims do token (RoleId + permissões do Role carregadas no momento do login).

## 4. Exemplo de DTOs

```json
// POST /api/v1/products (request)
{
  "name": "Cerveja Heineken Long Neck 330ml",
  "description": "Cerveja lager premium",
  "sku": "CERV-HEIN-330",
  "barcode": "7891149101015",
  "categoryId": "guid",
  "brandId": "guid",
  "unitOfMeasure": "UN",
  "costPrice": 4.50,
  "salePrice": 8.90,
  "minStock": 24,
  "maxStock": 200
}
```

```json
// Response de erro (ProblemDetails)
{
  "type": "https://httpstatuses.com/409",
  "title": "SKU já cadastrado",
  "status": 409,
  "detail": "Já existe um produto ativo com o SKU 'CERV-HEIN-330'.",
  "traceId": "00-abc123..."
}
```

## 5. Códigos HTTP

`200 OK` (leitura/atualização), `201 Created` (criação, com header `Location`), `204 No Content` (ações sem corpo de retorno), `400 Bad Request` (validação), `401 Unauthorized` (token ausente/inválido), `403 Forbidden` (sem permissão), `404 Not Found`, `409 Conflict` (regra de unicidade/estado), `500 Internal Server Error` (não tratado — logado e convertido em `ProblemDetails` genérico).

## 6. Consumo pelo Frontend (React/TypeScript)

- CORS liberado por origem configurável via `appsettings`/variável de ambiente.
- Contrato estável de DTOs (Response) documentado no Swagger, servindo de base para geração de tipos TypeScript (ex.: via `openapi-typescript`).
- Endpoints de dashboard desenhados para alimentar diretamente componentes de gráfico/cartão sem processamento adicional no frontend.
