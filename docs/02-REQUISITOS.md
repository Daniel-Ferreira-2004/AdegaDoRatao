# 02 — Requisitos — Adega do Ratão

## 1. Perfis de Usuário

| Perfil    | Descrição                                             |
|-----------|--------------------------------------------------------|
| ADMIN     | Acesso total ao sistema, incluindo usuários e permissões |
| GERENTE   | Produtos, estoque, vendas, compras, financeiro, relatórios |
| OPERADOR  | Vendas, consulta de produtos, consulta de estoque       |

Modelo de permissões extensível: `Role` possui uma coleção de `Permission` (string por recurso/ação, ex.: `products.write`, `financial.read`), permitindo criar novos perfis sem alterar código, apenas dados.

## 2. Requisitos Funcionais (RF)

### Produtos
- RF01 — Cadastrar, editar, consultar e listar produtos (com paginação e filtros).
- RF02 — Pesquisar produto por nome, SKU e código de barras.
- RF03 — Ativar/desativar produto.
- RF04 — Alterar preços com registro em auditoria.
- RF05 — Consultar histórico de alterações relevantes do produto.

### Categorias e Marcas
- RF06 — CRUD de categorias (cadastrar, editar, listar, consultar, ativar/desativar).
- RF07 — CRUD de marcas.

### Estoque
- RF08 — Registrar entrada, saída e ajuste de estoque, sempre via movimentação.
- RF09 — Consultar estoque atual por produto.
- RF10 — Listar produtos abaixo do estoque mínimo.
- RF11 — Consultar histórico de movimentações por produto/período.

### Compras
- RF12 — Registrar compra com fornecedor, itens, custos, frete e desconto.
- RF13 — Confirmar compra: atualizar estoque, gerar movimentações e lançamento financeiro.

### Vendas
- RF14 — Registrar venda com itens, descontos e forma de pagamento.
- RF15 — Validar estoque disponível antes de concluir a venda.
- RF16 — Finalizar venda: baixar estoque, gerar movimentação e lançamento financeiro.
- RF17 — Cancelar venda, com avaliação de estorno de estoque/financeiro.

### Financeiro
- RF18 — Registrar lançamentos financeiros (entrada/saída) manuais e automáticos.
- RF19 — CRUD de categorias financeiras.
- RF20 — Consultar fluxo de caixa por período (hoje, ontem, 7 dias, mês atual, mês anterior, personalizado).

### Dashboard
- RF21 — Expor endpoint(s) agregando vendas do dia/mês, faturamento, despesas, saldo, estoque baixo/zerado, últimas vendas/movimentações, produtos mais vendidos, formas de pagamento mais usadas.

### Usuários e Autenticação
- RF22 — Login com emissão de JWT.
- RF23 — CRUD de usuários e atribuição de perfil (ADMIN).
- RF24 — Autorização por perfil/permissão em todos os endpoints protegidos.

### Auditoria
- RF25 — Registrar automaticamente operações sensíveis (preço, estoque, cadastro/edição/exclusão lógica, venda, compra, financeiro).

## 3. Requisitos Não Funcionais (RNF)

- RNF01 — API RESTful, stateless, versionável (`/api/v1/...` reservado para evolução futura).
- RNF02 — Autenticação via JWT com expiração configurável; sem sessão em servidor.
- RNF03 — Senhas armazenadas apenas com hash forte (BCrypt/Argon2 via `IPasswordHasher`), nunca em texto puro.
- RNF04 — Consistência transacional obrigatória em operações que afetam estoque + financeiro simultaneamente (venda, compra).
- RNF05 — Logging estruturado (Serilog) com correlação de requisição, sem dados sensíveis em log.
- RNF06 — Tratamento de erro centralizado, respostas no formato `ProblemDetails`, sem stack trace em produção.
- RNF07 — Documentação de API via Swagger/OpenAPI sempre atualizada.
- RNF08 — Testes automatizados cobrindo as regras de negócio críticas (unitários) e os principais fluxos de API (integração).
- RNF09 — Valores monetários e de preço em `decimal`, nunca `float`/`double`.
- RNF10 — Configuração sensível fora do código-fonte (User Secrets em dev, variáveis de ambiente em produção).
- RNF11 — Sistema preparado para múltiplos usuários concorrentes sem corrupção de estoque (uso de transações/lock otimista via `RowVersion` em `Product`/`Stock`).
- RNF12 — API desacoplada de qualquer frontend específico; CORS configurável para permitir consumo por um app React separado.

## 4. Casos de Uso Principais

- UC01 — Efetuar login.
- UC02 — Cadastrar produto.
- UC03 — Alterar preço de um produto.
- UC04 — Registrar entrada de estoque (compra ou ajuste).
- UC05 — Registrar venda de balcão.
- UC06 — Cancelar uma venda.
- UC07 — Registrar uma compra de fornecedor.
- UC08 — Consultar fluxo de caixa do mês atual.
- UC09 — Consultar dashboard operacional do dia.
- UC10 — Consultar produtos com estoque abaixo do mínimo.
- UC11 — Consultar trilha de auditoria de uma entidade.
- UC12 — Gerenciar usuários e perfis (ADMIN).

Cada caso de uso acima tem seu fluxo detalhado descrito junto ao módulo correspondente em `03-REGRAS-DE-NEGOCIO.md`.

## 5. Pontos Ambíguos Identificados e Decisão Adotada

| Ponto ambíguo | Decisão adotada | Justificativa |
|---|---|---|
| "Categoria inativa não deve receber novos produtos, conforme regra definida" | Bloquear a associação de produto novo a categoria inativa; produtos já associados continuam válidos até serem editados | Evita inconsistência sem quebrar dados históricos |
| Estoque negativo | Bloqueado por padrão; liberado apenas via flag de configuração por produto ou global (`AllowNegativeStock`) | Atende ao pedido explícito de regra configurável |
| Estorno de estoque no cancelamento de venda | Estorna automaticamente o estoque (volta a quantidade) e cria uma `FinancialTransaction` de estorno; a venda muda para status `CANCELADA`, não é excluída | Preserva rastreabilidade/auditoria |
| Estorno de compra | Mesma lógica: se estoque já foi usado (venda) não é permitido cancelar diretamente — exige tratamento manual/ajuste; caso contrário, estorna estoque e financeiro | Evita estoque negativo por cancelamento indevido |
| Exclusão de produto/categoria/marca | Sempre soft delete (`IsActive = false` / `DeletedAt`), nunca exclusão física | Preserva histórico de vendas/compras que referenciam a entidade |
