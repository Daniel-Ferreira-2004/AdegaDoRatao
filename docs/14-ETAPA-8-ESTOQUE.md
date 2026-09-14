# 14 — Etapa 8: Estoque

- `POST /api/stock/movements`: registra entrada, saída ou ajuste.
- `GET /api/stock/products/{productId}/movements`: histórico por período.
- `GET /api/stock/low?includeOutOfStock=true`: estoque baixo e, opcionalmente, zerado.

Um ajuste recebe o estoque final contado em `quantity`; entrada e saída recebem
a quantidade a movimentar. Toda alteração gera movimento e auditoria.
