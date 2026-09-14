# 05 — API — Adega do Ratão

API RESTful, JSON, documentada via Swagger/OpenAPI, protegida por JWT (Bearer).

## 1. Convenções

- Recursos no plural, kebab/lowercase: `/api/products`.
- Paginação: `?page=1&pageSize=20` com resposta `PagedResult<T>` (`items`, `totalCount`, `page`, `pageSize`).
- Filtros via query string (`?name=`, `?sku=`, `?categoryId=`, `?status=`).
- Datas em ISO 8601 UTC.
- Erros no formato `ProblemDetails` (RFC 7807).

## 2. Endpoints

```text
POST   /api/auth/login
POST   /api/auth/refresh                (evolução futura)

GET    /api/products
GET    /api/products/{id}
GET    /api/products/search?query=
POST   /api/products
PUT    /api/products/{id}
PATCH  /api/products/{id}/status        (ativar/desativar)
PATCH  /api/products/{id}/price
GET    /api/products/{id}/history

GET    /api/categories
GET    /api/categories/{id}
POST   /api/categories
PUT    /api/categories/{id}
PATCH  /api/categories/{id}/status

GET    /api/brands
GET    /api/brands/{id}
POST   /api/brands
PUT    /api/brands/{id}
PATCH  /api/brands/{id}/status

GET    /api/stock/movements?productId=&from=&to=
POST   /api/stock/movements              (entrada/saída/ajuste manual)
GET    /api/stock/low                    (abaixo do mínimo)
GET    /api/stock/out-of-stock

GET    /api/suppliers
GET    /api/suppliers/{id}
POST   /api/suppliers
PUT    /api/suppliers/{id}
PATCH  /api/suppliers/{id}/status

GET    /api/purchases
GET    /api/purchases/{id}
POST   /api/purchases
POST   /api/purchases/{id}/confirm
POST   /api/purchases/{id}/cancel

GET    /api/sales
GET    /api/sales/{id}
POST   /api/sales
POST   /api/sales/{id}/cancel

GET    /api/payment-methods
POST   /api/payment-methods

GET    /api/financial/categories
POST   /api/financial/categories
GET    /api/financial/transactions?from=&to=&type=
POST   /api/financial/transactions
GET    /api/financial/cash-flow?period=

GET    /api/dashboard/summary
GET    /api/dashboard/top-products
GET    /api/dashboard/payment-methods-usage

GET    /api/users
POST   /api/users
PUT    /api/users/{id}
PATCH  /api/users/{id}/status

GET    /api/roles
GET    /api/roles/{id}/permissions
PUT    /api/roles/{id}/permissions

GET    /api/audit-logs?entityName=&entityId=
```

## 3. Autenticação e Autorização na API

- `POST /api/auth/login` recebe `{ email, password }`, retorna `{ token, expiresAt, user: { id, name, role } }`.
- Todos os demais endpoints exigem header `Authorization: Bearer {token}`.
- Autorização por policy baseada em `Permission` (ex.: `[Authorize(Policy = "products.write")]`), resolvida a partir das claims do token (RoleId + permissões do Role carregadas no momento do login).

## 4. Exemplo de DTOs

```json
// POST /api/products (request)
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
