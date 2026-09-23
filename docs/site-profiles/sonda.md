# Perfil — Sonda

```text
SITE: Supermercados Sonda (delivery)
DOMÍNIO: sondadelivery.com.br (www)
PLATAFORMA: ASP.NET WebForms (busca por URL funciona, sem ViewState)
REGIÕES ATENDIDAS: SP; loja padrão do site
MÉTODO DE BUSCA: GET /delivery/busca/{termo} — cartões ".product"
MÉTODO DE LOCALIZAÇÃO: NENHUM implementado — loja padrão (REGIÃO NÃO CONFIRMADA)
MÉTODO DE IDENTIFICAÇÃO: somente score de tokens (site NÃO expõe EAN)
MÉTODO DE OBTENÇÃO DO PREÇO: texto "Por R$ X,XX" no cartão (fallback: "R$ X,XX")
MÉTODO DE VERIFICAÇÃO DE ESTOQUE: indireto — sem preço ≈ sem estoque
ESTRUTURA DOS RESULTADOS: .product → nome em span.tit; link no <a> do cartão
FALLBACKS: 1) regex "Por R$"; 2) regex "R$" genérico no cartão
REGRAS ESPECÍFICAS:
  - Lento (5-15s): apenas no job diário, nunca em tempo real
  - Playwright: EvaluateAsync deve retornar JSON string (conversor quebra com null)
  - ParsePreco compartilhado com Shibata (ShibataPrecoCollector.ParsePreco)
PROBLEMAS CONHECIDOS:
  - Sem confirmação de EAN (score mínimo 0.5 pode casar variante errada)
  - Região não confirmada
NÍVEL DE CONFIANÇA: Low — RegiaoConfirmada=false
ÚLTIMA VALIDAÇÃO: 2026-09-22
VERSÃO DO PERFIL: 2
```

Coletor: `src/AdegaDoRatao.Infrastructure/ExternalServices/Coletores/SondaPrecoCollector.cs`
