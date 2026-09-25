# Perfil — D'avó

```text
SITE: D'avó Supermercados
DOMÍNIO: davo.com.br (www)
PLATAFORMA: VipCommerce (SPA Angular; API services.vipcommerce.com.br/api-admin/v1)
REGIÕES ATENDIDAS: SP; loja de SUZANO = centro_distribuicao/4
MÉTODO DE BUSCA: GET /busca?termo={termo} — SPA chama a API interna
  org/399/filial/1/centro_distribuicao/{cd}/loja/buscas/produtos/termo/{termo}
MÉTODO DE LOCALIZAÇÃO: seletor de loja "Retirar na loja" → "D'avó Suzano"
  (SPA chama carrinhos/alterar_centro_distribuicao/omnichannel {"novo_cd_id":4}
  e grava cdSelecionado=4) — REGIÃO CONFIRMADA (Suzano)
MÉTODO DE IDENTIFICAÇÃO: EAN exposto pela API (codigo_barras) + score de tokens
MÉTODO DE OBTENÇÃO DO PREÇO: JSON da API — "preco"; oferta em "oferta.preco_oferta"
  (tem prioridade, é o preço em destaque); "disponivel" indica estoque
ESTRUTURA DOS RESULTADOS: data.produtos[] → descricao, codigo_barras, link,
  preco, oferta.preco_oferta, disponivel
FALLBACKS: nenhum — a API exige Bearer de sessão anônima que só o SPA obtém,
  então o coletor intercepta o JSON da busca renderizada (não chama a API direto)
REGRAS ESPECÍFICAS:
  - Lento (5-15s): apenas no job diário, nunca em tempo real
  - Precisa selecionar a loja de Suzano ANTES de buscar (preço é por CD)
  - Headers da API: authorization Bearer (token de sessão), domainkey=davo.com.br,
    organizationid=399, sessao-id — todos gerenciados pelo SPA
PROBLEMAS CONHECIDOS:
  - Token de sessão anônima não é obtível fora do SPA (por isso Playwright)
NÍVEL DE CONFIANÇA: High com EAN / Medium por nome — RegiaoConfirmada=true
ÚLTIMA VALIDAÇÃO: 2026-09-25
VERSÃO DO PERFIL: 1
```

Coletor: `src/AdegaDoRatao.Infrastructure/ExternalServices/Coletores/DavoPrecoCollector.cs`
