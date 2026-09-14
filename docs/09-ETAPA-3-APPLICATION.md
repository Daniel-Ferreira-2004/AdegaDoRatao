# 09 — Etapa 3: Application

## O que esta etapa entrega

A camada `AdegaDoRatao.Application` passou a concentrar os casos de uso, sem
conhecer Entity Framework, HTTP ou SQL Server. Ela conhece somente as entidades
e os contratos de repositório do Domínio. Assim, a API e a persistência podem
evoluir sem levar regras de negócio para controllers ou repositories.

## Componentes criados

- `DTOs`: modelos de entrada e saída. Entidades do Domínio nunca devem ser
  retornadas diretamente pela API.
- `Validators`: regras de formato e campos obrigatórios com FluentValidation.
  As regras que consultam o banco permanecem nos services.
- `Services`: fluxos de produto, categoria, marca, estoque, compra e venda.
- `Interfaces`: contratos de service e as abstrações de infraestrutura
  (`IUnitOfWork`, usuário atual, auditoria, hash e JWT).
- `Common`: resultado paginado e exceção previsível de aplicação.

## Decisões importantes

1. **Estoque:** `StockService` é o caminho para uma movimentação manual.
   Ele atualiza produto e movimento em uma única transação. Em um ajuste, a
   quantidade solicitada é o estoque final contado, não uma diferença.
2. **Venda:** `SaleService` cria a venda, baixa cada item e registra a entrada
   financeira na mesma transação. O preço é copiado do produto naquele momento.
3. **Compra:** criar uma compra deixa-a pendente. Confirmá-la é que cria as
   entradas de estoque e a saída financeira.
4. **Cancelamentos:** não excluem históricos. Criam movimentos e lançamentos
   de estorno, preservando a rastreabilidade.
5. **Mobile:** buscas de produtos aceitam página e tamanho de página (limitado
   a 100 itens), evitando respostas excessivas em redes móveis.

## Configurações necessárias na etapa de infraestrutura

Para venda e compra, a inicialização do banco deve criar as categorias
financeiras ativas `Vendas` e `Compras`; os services usam esses nomes para os
lançamentos automáticos. A Infrastructure também deve carregar os itens ao
consultar uma venda ou compra por identificador.

## Como validar localmente

Com o .NET SDK 8 instalado, na pasta raiz do projeto:

```bash
dotnet restore
dotnet build
dotnet test
```

Nesta etapa ainda não há endpoints; eles serão adicionados após a Infrastructure
e a configuração do banco. A validação de requests será ligada à API na etapa
de controllers.
