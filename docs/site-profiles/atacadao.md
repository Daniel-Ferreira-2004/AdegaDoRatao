# Perfil — Atacadão

```text
SITE: Atacadão
DOMÍNIO: atacadao.com.br (www)
PLATAFORMA: VTEX IO (FastStore) + Cloudflare
REGIÕES ATENDIDAS: nacional; preço regionalizado por loja (seller)
MÉTODO DE BUSCA: GET /io/api/catalog_system/pub/products/search?ft={nome}&_from=0&_to=5&sc={1,2}
MÉTODO DE LOCALIZAÇÃO: CEP 08533-020, nº 126 (Ferraz de Vasconcelos) — SEMPRE esta
                       localização. No site: botão "CEP" → "Entrega em casa" → CEP →
                       número 126 → confirmar.
                       Na API: /api/checkout/pub/regions?postalCode=08533020 → sellers
                       (prefere nome contendo "Ferraz"; fallback atacadaobr733);
                       cookies postalCode=08533-020; region=08533020 no HttpClient
MÉTODO DE IDENTIFICAÇÃO: 1) EAN exato em items[].ean; 2) score de tokens (NomeProdutoMatcher)
MÉTODO DE OBTENÇÃO DO PREÇO: POST /api/checkout/pub/orderForms/simulation?sc=1
                             com { id=skuId, quantity=1, seller } + postalCode da adega;
                             preço em CENTAVOS (sellingPrice)
MÉTODO DE OBTENÇÃO DO PREÇO PROMOCIONAL: simulação com 12 unidades — quando o
                             sellingPrice cai vs. 1 unid., é o preço "a partir de N unid."
                             (destaque do site): devolvido como TipoPreco=CondicionadoQuantidade.
                             Preço normal = simulação com 1 unidade (tier quantity=1).
MÉTODO DE VERIFICAÇÃO DE ESTOQUE: availability == "available" na simulação
PAGINAÇÃO: _from/_to (coletor lê só os 6 primeiros)
FALLBACKS: 1) simulação qtd=1 → qtd=6; 2) HTML da página → JSON-LD offers.price;
           3) primeiro "R$ X,XX" do HTML (último recurso, risco de preço de relacionados)
REGRAS ESPECÍFICAS:
  - Cloudflare bloqueia sem User-Agent de navegador
  - API de catálogo NÃO tem o preço real (zerado/outra loja) — só simulação/página
  - Busca textual nem sempre indexa EAN → buscar pelo nome, confirmar pelo ean
  - link do produto pode vir relativo ou absoluto (secure.atacadao.com.br)
PROBLEMAS CONHECIDOS:
  - simulação usa sc=1 fixo com seller regional (possível inconsistência canal/seller)
NÍVEL DE CONFIANÇA: High (EAN) / Medium (nome) — RegiaoConfirmada=true
ÚLTIMA VALIDAÇÃO: 2026-09-23 (SKU 12864: qtd=1 → R$ 5,19; qtd=12 → R$ 4,99 condicionado)
VERSÃO DO PERFIL: 4
```

Coletor: `src/AdegaDoRatao.Infrastructure/ExternalServices/Coletores/AtacadaoPrecoCollector.cs`
