# Perfil — Tenda Atacado

```text
SITE: Tenda Atacado
DOMÍNIO: tendaatacado.com.br (www)
PLATAFORMA: Next.js (a API VTEX pública NÃO está habilitada nesta conta — 404)
REGIÕES ATENDIDAS: SP e outros; preço da loja padrão da região
MÉTODO DE BUSCA: GET /busca?q={nome} — resultados no <script id="__NEXT_DATA__">
MÉTODO DE LOCALIZAÇÃO: sessão anônima na API pública (api.tendaatacado.com.br):
                       1) POST /api/public/anonymous-client → {user, password}
                       2) POST /api/public/oauth/access-token (grant=password, client_id/secret
                          públicos do JS do site) → access_token (header X-Authorization)
                       3) GET /api/client → id (cookie _Tendaatacado-userInfo)
                       4) POST /api/shopping-cart {"zipcode":8533020} → shoppingCartId +
                          branchId (cookies _Tendaatacado-cartID / _Tendaatacado-branchID)
                       Cookies _Tendaatacado-* (incl. auth-token!) regionalizam o HTML.
                       CEP 08533-020 → branchId 41 (Itaquaquecetuba, entrega) / 53 (Ferraz,
                       retirada) — mesmos preços regionais. Sem sessão: filial padrão
                       GUARULHOS (preços diferentes! ex.: Piracanjuba 395g 6,25/5,75 vs
                       Ferraz 6,79/6,29). Falha na sessão → coleta sem região (não quebra).
MÉTODO DE IDENTIFICAÇÃO: 1) EAN na URL da thumbnail; 2) score de tokens (NomeProdutoMatcher)
MÉTODO DE OBTENÇÃO DO PREÇO: campo "price" (numérico) do objeto produto no __NEXT_DATA__
MÉTODO DE OBTENÇÃO DO PREÇO PROMOCIONAL: campo "wholesalePrices":[{"minQuantity":N,"price":X}]
                             no objeto do produto — quando o tier é menor que "price",
                             devolvido como TipoPreco=CondicionadoQuantidade (destaque
                             "a partir de N unidades" do site). Validado 2026-09-23:
                             Original 350ml → price 4.99, wholesale 4.79 (min 6)
MÉTODO DE VERIFICAÇÃO DE ESTOQUE: campo "isAvailable" (obs.: às vezes false mesmo com preço)
ESTRUTURA DOS RESULTADOS: árvore JSON percorrida em profundidade; objetos com "name"+"price"
URL DO PRODUTO: campo "url" ou "slug" (→ /produto/{slug}); fallback = página de busca
FALLBACKS: 1) EAN na thumbnail; 2) melhor score de tokens ≥ 0.5 + tokens obrigatórios
REGRAS ESPECÍFICAS:
  - User-Agent de navegador obrigatório
  - Busca pelo NOME é mais confiável que por EAN
PROBLEMAS CONHECIDOS:
  - ~~Região não confirmada~~ RESOLVIDO 2026-09-23 (sessão anônima + CEP da adega)
  - Confirmação de EAN depende da thumbnail (frágil — site pode mudar o padrão da URL)
  - Preço "No Cartão Tenda" (fidelidade/Elo) NÃO está no __NEXT_DATA__ — calculado
    no checkout/API autenticada. Quando divergir do wholesale, não é capturável
    sem login (verificado 2026-09-23). NUNCA supor que cartão == wholesale.
NÍVEL DE CONFIANÇA: High (EAN+região) / Medium (nome+região ou EAN sem região) / Low (nome sem região)
ÚLTIMA VALIDAÇÃO: 2026-09-23 (regionalização validada de ponta a ponta: busca retorna
                  preços de Ferraz com a sessão; 91/91 testes OK)
VERSÃO DO PERFIL: 5
```

Coletor: `src/AdegaDoRatao.Infrastructure/ExternalServices/Coletores/TendaPrecoCollector.cs`
