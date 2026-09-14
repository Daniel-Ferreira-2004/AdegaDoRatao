# 15 — Etapa 9: Fornecedores e compras

Fornecedores: `GET/POST /api/suppliers`, `GET/PUT /api/suppliers/{id}` e
`PATCH /api/suppliers/{id}/active`.

Compras: `POST /api/purchases` cria uma compra **pendente**; `POST
/api/purchases/{id}/confirm` confirma o recebimento e, na mesma transação,
entrada no estoque e saída financeira. `POST /api/purchases/{id}/cancel`
preserva o histórico e estorna somente quando o estoque ainda é suficiente.
