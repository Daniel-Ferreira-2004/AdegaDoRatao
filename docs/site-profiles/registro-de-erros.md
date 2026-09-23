# Registro de Erros — Comparação de Preços (regra 12)

Classificação: SEARCH_ERROR, PRODUCT_MATCH_ERROR, PRICE_ERROR,
REGION_ERROR, STOCK_ERROR, PROMOTION_ERROR, UNIT_ERROR, PAGINATION_ERROR,
DUPLICATION_ERROR, DATA_STALENESS, SITE_STRUCTURE_CHANGE, UNKNOWN.

---

## 2026-09-22 — Atacadão — PROMOTION_ERROR ✅ CORRIGIDO

```text
SITE: Atacadão
DATA: 2026-09-22
TIPO DE ERRO: PROMOTION_ERROR (preço condicionado gravado como normal)
ENTRADA UTILIZADA: simulação de checkout VTEX com quantity=6 (SKU 3870)
RESULTADO OBTIDO: R$ 12,29 (tier "a partir de 6 unid.") gravado como preço normal
RESULTADO ESPERADO: R$ 12,49 (preço unitário)
CAUSA CONFIRMADA: sellingPrice da simulação com qtd>1 é o preço CONDICIONADO;
                  priceDefinition.sellingPrices só traz o tier aplicável
CORREÇÃO: simular qtd=1 primeiro; quando houver tiers, usar o tier
          quantity<=1 (AtacadaoPrecoCollector.TentarPrecoNaSimulacao)
TESTE REALIZADO: API real — qtd=1 → 1249; qtd=6 → 1229; build + 67 testes OK
RESULTADO APÓS CORREÇÃO: preço unitário correto gravado
```

## 2026-09-22 — Todas as redes — modelo de confiança ✅ IMPLEMENTADO

```text
TIPO: melhoria estrutural (regras 6, 9 e 16 do protocolo)
ALTERAÇÃO: ColetaPrecoRede e MarketPriceSnapshot ganharam TipoPreco,
           Confianca e RegiaoConfirmada; DTO da API expõe os campos;
           migration AddConfiancaTipoPrecoSnapshot
TESTE: build da solução OK; 67/67 testes unitários OK
```

## 2026-09-23 — Todos (matcher) — UNIT_ERROR ✅ CORRIGIDO

```text
SITE: todos (NomeProdutoMatcher compartilhado)
DATA: 2026-09-23
TIPO DE ERRO: UNIT_ERROR
ENTRADA UTILIZADA: busca "Arroz 5kg" vs. candidato "Arroz Branco Tipo 1 5 Kg"
RESULTADO OBTIDO: não casava (produto existente era dado como ausente)
RESULTADO ESPERADO: casar — mesma medida, grafada com espaço
CAUSA CONFIRMADA: ContemTokensObrigatorios exigia o token COMPOSTO ("5kg")
                  no candidato via .All(), anulando a separação em partes
                  ("5", "kg") que existia justamente para esse caso
CORREÇÃO: tokens obrigatórios com dígito passam a ser os PURAMENTE
          numéricos quando existem; compostos só como fallback
TESTE REALIZADO: suíte nova de 21 testes (matcher + Atacadão + Tenda) — 88/88 OK
RESULTADO APÓS CORREÇÃO: "5kg" casa com "5 Kg"; "2L" NÃO casa com "350ml"
```

## 2026-09-23 — Atacadão — PROMOTION_ERROR (decisão de negócio) ✅ IMPLEMENTADO

```text
SITE: Atacadão
DATA: 2026-09-23
TIPO DE ERRO: PROMOTION_ERROR (preço exibido desatualizado vs. destaque do site)
ENTRADA UTILIZADA: Cerveja Original 350ml (SKU 12864, EAN 7891991015493)
RESULTADO OBTIDO: R$ 5,19 (preço unitário) exibido na comparação
RESULTADO ESPERADO: R$ 4,99 — preço em destaque no site ("a partir de 12 unid.", -4%)
CAUSA CONFIRMADA: a correção de 2026-09-22 passou a gravar SÓ o preço unitário;
                  o desconto por quantidade (tier qtd=12 na simulação VTEX) era descartado
CORREÇÃO: o coletor simula 1 unid. (preço normal) E 12 unid.; quando o preço
          em 12 é menor, devolve o preço condicionado marcado como
          TipoPreco=CondicionadoQuantidade (nunca misturado com o normal).
          O modal exibe o selo "Preço atacado" nesses casos.
TESTE REALIZADO: API real — qtd=1 → 519; qtd=12 → 499. Teste unitário
                 Coletar_ComDescontoPorQuantidade_DevolvePrecoCondicionado_MarcadoComoTal. 88/88 OK
RESULTADO APÓS CORREÇÃO: comparação exibe R$ 4,99 com selo "Preço atacado"
```

---

## Problemas abertos

| Data | Site | Tipo | Descrição |
|------|------|------|-----------|
| 2026-09-22 | Shibata, Sonda | REGION_ERROR | Preço da loja padrão; região não confirmada (sinalizado no dado, falta coleta regionalizada) |
| ~~2026-09-22~~ | ~~Tenda~~ | ~~REGION_ERROR~~ | ✅ RESOLVIDO 2026-09-23: sessão anônima + CEP 08533-020 → filial Ferraz; divergência Piracanjuba 395g (5,75 Guarulhos vs 6,29 Ferraz) eliminada |
| 2026-09-22 | Shibata, Sonda | PRODUCT_MATCH_ERROR | Sem confirmação de EAN; score mínimo 0.5 pode casar variante errada |
| 2026-09-22 | Shibata | PRICE_ERROR | Fallback lê o primeiro "R$" do body — pode ser preço de relacionados |
| 2026-09-22 | Atacadão | REGION_ERROR | Simulação usa sc=1 fixo com seller regional (possível inconsistência canal/seller) |
| 2026-09-22 | Todos | UNKNOWN | Sem detecção automática de SITE_STRUCTURE_CHANGE |
| 2026-09-23 | Todos (matcher) | PRODUCT_MATCH_ERROR | Variantes com a mesma medida (ex.: "Coca 2L" vs "Coca Zero 2L") passam no matcher — proteção real é o EAN; risco residual em Shibata/Sonda |
