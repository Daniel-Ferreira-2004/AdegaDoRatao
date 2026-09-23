# Perfil — Shibata

```text
SITE: Shibata Supermercados
DOMÍNIO: loja.shibata.com.br (www)
PLATAFORMA: SPA Angular ("Supermercados Online"); API exige token de sessão
REGIÕES ATENDIDAS: SP (interior/litoral); loja padrão do site
MÉTODO DE BUSCA: AUTOCOMPLETE da home — a URL /busca?q=... NÃO funciona.
                 Digitar (PressSequentially, delay 80ms) no campo
                 #search-term / input[placeholder*='O que você precisa'];
                 resultados no dropdown nav[aria-label='Menu de busca']
MÉTODO DE LOCALIZAÇÃO: NENHUM implementado — loja padrão (REGIÃO NÃO CONFIRMADA)
MÉTODO DE IDENTIFICAÇÃO: somente score de tokens (site NÃO expõe EAN)
MÉTODO DE OBTENÇÃO DO PREÇO: texto "R$ X,XX" dentro do link do autocomplete
MÉTODO DE VERIFICAÇÃO DE ESTOQUE: indireto — sem preço ≈ sem estoque
FALLBACKS: 1) preço no autocomplete; 2) abrir página do produto e ler
           o primeiro "R$ X,XX" do body (RISCO: pode ser preço de
           relacionados — correção pendente)
REGRAS ESPECÍFICAS:
  - FillAsync NÃO dispara o autocomplete — só eventos de teclado reais
  - Ler APENAS links dentro do nav "Menu de busca"; a home tem carrosséis
    com links /produto/ que já causaram leitura de produto errado
  - Modal de cookies/boas-vindas: fechar com button 'Fechar modal'
  - Lento (5-15s): apenas no job diário, nunca em tempo real
  - Playwright: EvaluateAsync deve retornar JSON string (conversor quebra com null)
PROBLEMAS CONHECIDOS:
  - Sem confirmação de EAN (score mínimo 0.5 pode casar variante errada)
  - Fallback de preço na página pega o primeiro "R$" do body
  - Região não confirmada
NÍVEL DE CONFIANÇA: Low — RegiaoConfirmada=false
ÚLTIMA VALIDAÇÃO: 2026-09-22
VERSÃO DO PERFIL: 2
```

Coletor: `src/AdegaDoRatao.Infrastructure/ExternalServices/Coletores/ShibataPrecoCollector.cs`
