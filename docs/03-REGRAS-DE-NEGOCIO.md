# 03 — Regras de Negócio — Adega do Ratão

## 1. Produtos

- RN01 — SKU é único; código de barras é único quando informado (nulo permitido, mas não duplicado).
- RN02 — Preço de venda e preço de custo devem ser ≥ 0 (`decimal`).
- RN03 — Produto inativo não pode ser vendido nem receber entrada de compra nova.
- RN04 — Produto novo não pode ser vinculado a categoria inativa (RN validada no `ProductService`).
- RN05 — Toda alteração de preço gera registro em `AuditLog` com valor anterior/novo.
- RN06 — Estoque mínimo ≤ estoque máximo, quando ambos informados.

## 2. Categorias e Marcas

- RN07 — Nome de categoria/marca único (case-insensitive).
- RN08 — Desativar categoria não desativa produtos já vinculados; apenas impede novos vínculos (RN04).

## 3. Estoque

- RN09 — Toda alteração de quantidade em estoque **somente** ocorre através da criação de um `StockMovement` (`ENTRADA`, `SAIDA`, `AJUSTE`); não existe update direto do campo de estoque do produto.
- RN10 — Cada movimentação registra: produto, tipo, quantidade, estoque anterior, estoque posterior, motivo, usuário responsável, data/hora.
- RN11 — Estoque não pode ficar negativo, salvo produto/config com `AllowNegativeStock = true`.
- RN12 — Ajuste de estoque (`AJUSTE`) exige motivo obrigatório (texto livre) para fins de auditoria.

## 4. Compras

- RN13 — Compra possui itens com quantidade > 0 e preço de custo ≥ 0.
- RN14 — Total da compra = soma dos subtotais dos itens − desconto + frete.
- RN15 — Ao **confirmar** a compra (status `RECEBIDA`), em uma única transação:
  1. persistir `Purchase` e `PurchaseItem`s;
  2. para cada item, criar `StockMovement` do tipo `ENTRADA` e atualizar o estoque do produto;
  3. criar `FinancialTransaction` do tipo `SAIDA`, categoria "Compras", com referência à compra.
- RN16 — Compra em status `PENDENTE` não afeta estoque nem financeiro; só a confirmação dispara RN15.
- RN17 — Cancelamento de compra já confirmada só é permitido se nenhuma unidade recebida naquela compra já tiver sido vendida (verificação simplificada: estoque atual do produto ≥ quantidade a estornar); caso contrário, retorna erro orientando ajuste manual de estoque.

## 5. Vendas

- RN18 — Item de venda possui quantidade > 0; preço unitário vem do preço de venda vigente do produto no momento (preço "congelado" na venda).
- RN19 — Antes de confirmar a venda, validar que o estoque de cada produto é suficiente (respeitando RN11).
- RN20 — Ao **finalizar** a venda (status `CONCLUIDA`), em uma única transação:
  1. persistir `Sale` e `SaleItem`s;
  2. para cada item, criar `StockMovement` do tipo `SAIDA` e atualizar o estoque;
  3. criar `FinancialTransaction` do tipo `ENTRADA`, categoria "Vendas", com referência à venda.
- RN21 — Cancelamento de venda (`CANCELADA`): estorna o estoque (novo `StockMovement` do tipo `AJUSTE`, motivo "Estorno de venda #N") e cria `FinancialTransaction` do tipo `SAIDA` com referência de estorno; a venda original não é apagada.
- RN22 — Não é permitido cancelar venda já referenciada por outro processo financeiro conciliado (reservado para evolução futura de conciliação bancária — fora do escopo inicial, apenas documentado).

## 6. Formas de Pagamento

- RN23 — Representadas por enum/tabela de referência (`Dinheiro`, `PIX`, `Cartão de Débito`, `Cartão de Crédito`, `Outros`), nunca strings soltas no código.

## 7. Financeiro

- RN24 — Lançamento financeiro sempre possui tipo (`ENTRADA`/`SAIDA`), categoria financeira, valor > 0 e data.
- RN25 — Lançamentos oriundos de venda/compra são criados automaticamente pelo respectivo serviço (RN15/RN20) e não podem ser editados manualmente, apenas estornados via cancelamento da origem.
- RN26 — Lançamentos manuais (despesas, outros recebimentos) podem ser criados, editados e excluídos (soft delete) livremente por GERENTE/ADMIN.

## 8. Fluxo de Caixa e Dashboard

- RN27 — Saldo do período = soma de `FinancialTransaction` tipo `ENTRADA` − soma tipo `SAIDA`, filtradas pelo período solicitado.
- RN28 — "Lucro bruto estimado" = soma (preço de venda − preço de custo) × quantidade vendida no período, apenas para vendas com status `CONCLUIDA`.
- RN29 — Produto "sem estoque" = estoque atual = 0; "estoque baixo" = estoque atual > 0 e ≤ estoque mínimo.

## 9. Usuários, Perfis e Autorização

- RN30 — Um usuário possui exatamente um `Role` (ADMIN, GERENTE ou OPERADOR) na v1; modelo permite evoluir para múltiplos perfis por usuário sem quebrar contrato de API (ver `06-SEGURANCA.md`).
- RN31 — Apenas ADMIN gerencia usuários e perfis.

## 10. Auditoria

- RN32 — As seguintes ações são obrigatoriamente auditadas: alteração de preço, alteração de estoque (via movimentação), cadastro, edição, exclusão lógica, venda, compra, alteração financeira manual.
- RN33 — Todo registro de auditoria é imutável (sem update/delete).

## 11. Regras Transversais

- RN34 — Toda quantidade (estoque, itens de venda/compra) deve ser > 0 quando aplicável a uma operação de movimento; quantidade cadastrada de estoque atual pode ser 0.
- RN35 — Nenhum valor monetário usa `float`/`double`; sempre `decimal(18,2)`.
- RN36 — Exclusões são sempre lógicas (soft delete) para entidades referenciadas por histórico (Produto, Categoria, Marca, Fornecedor, Usuário); `StockMovement`, `FinancialTransaction`, `AuditLog` nunca são excluídos.
