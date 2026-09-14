# 13 — Etapa 7: Produtos, categorias e marcas

Endpoints protegidos adicionados:

- `GET/POST /api/products`, `GET/PUT /api/products/{id}`
- `PATCH /api/products/{id}/prices` e `/active`
- `GET/POST /api/categories` e `PATCH /api/categories/{id}/active`
- `GET/POST /api/brands` e `PATCH /api/brands/{id}/active`

Leitura exige `products.read` (ou `products.write`); alterações exigem
`products.write`. ADMIN sempre tem acesso. A listagem de produtos aceita os
parâmetros `page`, `pageSize`, `name`, `sku`, `categoryId` e `isActive`.
