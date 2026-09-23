# Perfis por Site (regra 11 do protocolo de auditoria)

Cada rede de mercado possui um perfil **independente** nesta pasta.
Conhecimento adquirido sobre um site NUNCA é aplicado a outro sem
validação (regra 19 — isolamento).

| Perfil | Domínio | Técnica | Confiança atual |
|--------|---------|---------|-----------------|
| [atacadao.md](atacadao.md) | atacadao.com.br | HTTP VTEX | MEDIUM-HIGH |
| [tenda.md](tenda.md) | tendaatacado.com.br | HTTP `__NEXT_DATA__` | MEDIUM-LOW |
| [shibata.md](shibata.md) | loja.shibata.com.br | Playwright | LOW |
| [sonda.md](sonda.md) | sondadelivery.com.br | Playwright | LOW |

Erros e correções ficam no [registro de erros](registro-de-erros.md)
(regra 12). Ao detectar mudança de estrutura em um site, marcar
`SITE_STRUCTURE_CHANGED` no registro e reanalisar o perfil (regra 15).
