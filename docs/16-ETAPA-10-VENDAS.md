# 16 — Etapa 10: Vendas

Esta etapa expõe o PDV pela API. Registrar uma venda **já conclui** o
fluxo: baixa estoque, grava o cupom e lança a entrada financeira na mesma
transação (RN20). Não existe status “pendente” de venda, ao contrário da
compra.

## Endpoints

| Método | Rota | Permissão | Efeito |
| --- | --- | --- | --- |
| GET | `/api/sales` | `sales.read` | Lista paginada (resumo, sem itens) |
| GET | `/api/sales/{id}` | `sales.read` | Detalhe com itens e preços congelados |
| POST | `/api/sales` | `sales.write` | Cria e finaliza a venda |
| POST | `/api/sales/{id}/cancel` | `sales.write` | Estorna estoque e caixa; **não apaga** o registro |
| GET | `/api/payment-methods` | `sales.read` | Lista formas de pagamento |
| POST | `/api/payment-methods` | `sales.write` | Cadastra uma forma (PIX, dinheiro, etc.) |
| PATCH | `/api/payment-methods/{id}/active` | `sales.write` | Ativa/desativa |

Filtros da listagem: `from`, `to`, `status` (`1` = Concluida, `2` = Cancelada),
`page` (mínimo 1) e `pageSize` (1 a 100, padrão 20). Pensado para rede móvel:
a lista não devolve os itens; o app pede o detalhe só quando o usuário abre o
cupom.

## Corpo do POST `/api/sales`

```json
{
  "items": [ { "productId": "guid", "quantity": 2 } ],
  "discount": 0,
  "paymentMethodId": "guid"
}
```

O preço unitário **não** vem do cliente: o serviço copia `Product.SalePrice`
na hora (RN18). O total é `soma(quantidade × preço) − desconto`.

## Pré-requisitos no banco

1. Forma de pagamento ativa.
2. Categoria financeira ativa chamada **Vendas** (o serviço busca pelo nome,
   sem diferenciar maiúsculas). Sem ela, a venda recusa com mensagem clara.
3. Produtos ativos e estoque suficiente (a menos que o produto permita
   estoque negativo).

## Cancelamento (RN21)

- Só vendas `Concluida` podem ser canceladas.
- Cada item volta ao estoque com movimento do tipo ajuste e motivo
  `Estorno de venda #N`.
- É criado um lançamento financeiro de **saída** de estorno. O lançamento
  original da venda permanece.
- RN22 (conciliação bancária bloqueando cancelamento) fica para evolução
  futura, como já documentado nas regras de negócio.

## Onde ler o código

- Regras do aggregate: `src/AdegaDoRatao.Domain/Entities/Sale.cs`
- Orquestração: `src/AdegaDoRatao.Application/Services/SaleService.cs`
- HTTP: `src/AdegaDoRatao.API/Controllers/SalesController.cs`
