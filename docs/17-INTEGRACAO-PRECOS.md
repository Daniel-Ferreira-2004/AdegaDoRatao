# 17 — Integração: comparação de preços de mercado (Cnova Tech Data Market)

Endpoint adicionado:

- `GET /api/products/{id}/precos-mercado` — compara o preço do produto nos
  supermercados do **estado de SP** (filtro `state=SP`, sem filtro de
  cidade — a Data Market não tem cobertura de lojas em Suzano, e o filtro
  por cidade sempre retornava vazio), usando o EAN (`Barcode`) cadastrado
  no produto. Cada loja retorna rede, cidade, preço e disponibilidade,
  ordenados do menor para o maior preço.
- **Filtro de redes**: apenas as redes abaixo aparecem na resposta (a
  comparação ignora acentos e aceita variações como "Assaí Atacadista" ou
  "D'Avó Supermercados"):
  Sonda, Shibata, Nagumo, Atacadão, Tenda Atacado, Rossi, Extra, Semar,
  Veran, D'avó, Assaí e Soni. A lista fica em `RedesPermitidas` no
  `PrecoMercadoService`.

Leitura exige `products.read` (ou `products.write`); ADMIN sempre tem acesso.

## API externa

- Base: `https://datamarket.cnovatech.com.br`
- Endpoint consumido: `GET /api/v1/market/products/ean/{ean}?state={UF}&city={cidade}`
- Autenticação: header `X-API-Key`
- Plano Free: 50 consultas/mês.

**Schema confirmado em 16/09/2026** (teste real com EAN 7894900011517):
a resposta tem `ean`, `product`, `brand`, `summary` (min/max/avg de preços)
e `stores[]` com `store_name`, `city`, `state`, `price`, `in_stock`, `url`.
Os filtros `state` e `city` funcionam como query params. O cruzamento com
as redes desejadas é feito por correspondência parcial de `store_name`
(case-insensitive); quando uma rede tem mais de uma loja, usa-se o menor
preço.

**Pendente (TODO no código):** na consulta de teste, nenhuma das redes
Veran, Shibata, Atacadão e Semar apareceu para Suzano/SP — validar com a
Cnova se essas redes têm cobertura na base deles.

## Decisões de design

- **Preço externo é DTO efêmero, não entidade persistida.** Os preços
  mudam constantemente, a cota do plano Free é baixa e o cache de 6h já
  cobre a reutilização. Persistir criaria falsa sensação de histórico.
- **Falhas da API externa nunca viram 500.** Timeout, erro de rede, 5xx e
  "produto não encontrado" (404) são representados no corpo da resposta:
  `consultaFalhou=true` (falha total) ou itens com `disponivel=false` e
  `mensagem` explicativa (rede sem o produto).
- **Cache em memória (`IMemoryCache`)** com TTL configurável
  (`DataMarket:CacheHours`, padrão 6h), chaveado por EAN + cidade + redes,
  para proteger a cota mensal. A resposta indica `origemCache=true` quando
  veio do cache.
- O contrato com a API externa segue o padrão `Result<T>` já usado no
  Application; o caso de uso (`PrecoMercadoService`) converte falhas em
  resposta tipada.

## Configuração

A API Key **nunca** fica no `appsettings.json` versionado — mesmo padrão
do `Jwt:Secret`:

```bash
# Desenvolvimento (User Secrets)
dotnet user-secrets set "DataMarket:ApiKey" "sua-chave" --project src/AdegaDoRatao.API

# Produção (variável de ambiente)
DataMarket__ApiKey=sua-chave
```

Seção `DataMarket` (valores não-secretos podem ficar no appsettings):

| Chave           | Padrão                                  | Descrição                          |
| --------------- | --------------------------------------- | ---------------------------------- |
| `BaseUrl`       | `https://datamarket.cnovatech.com.br`   | URL base da API                    |
| `ApiKey`        | _(vazio)_                               | Chave — apenas via secrets/ambiente |
| `TimeoutSeconds`| `10`                                    | Timeout de cada chamada HTTP       |
| `CacheHours`    | `6`                                     | TTL do cache em memória            |

## Onde cada parte vive

- Application: `Interfaces/MarketPriceContracts.cs` (`IPrecoMercadoExternoService`,
  `IPrecoMercadoService`), `DTOs/MarketPriceDtos.cs`,
  `Services/PrecoMercadoService.cs` (redes padrão e cidade).
- Infrastructure: `ExternalServices/DataMarketOptions.cs`,
  `ExternalServices/DataMarketPrecoService.cs` (HttpClient via
  `IHttpClientFactory` + `IMemoryCache`), registro em `DependencyInjection.cs`.
- API: endpoint em `Controllers/ProductsController.cs`.
- Testes: `tests/AdegaDoRatao.UnitTests/Application/PrecoMercadoServiceTests.cs`
  e `tests/AdegaDoRatao.IntegrationTests/Controllers/PrecosMercadoTests.cs`
  (timeout e rede sem produto, ambos respondendo 200 sem exceção).
