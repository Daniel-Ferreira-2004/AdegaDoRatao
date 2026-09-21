namespace AdegaDoRatao.Application.DTOs;

/// <summary>
/// Preço de um produto em uma rede de varejo, retornado pela API externa
/// de comparação de preços (Cnova Tech Data Market).
///
/// Decisão de modelagem: este dado é EFÉMERO (DTO de consulta), não uma
/// entidade persistida. Os preços mudam constantemente, o plano Free da
/// API tem limite de 50 consultas/mês, e o cache de 6h na Infrastructure
/// já cobre a necessidade de reutilização. Persistir criaria uma falsa
/// sensação de histórico confiável. Ver docs/17-INTEGRACAO-PRECOS.md.
/// </summary>
/// <param name="Rede">Nome da rede (ex.: "Veran", "Shibata", "Atacadão", "Semar").</param>
/// <param name="Cidade">Cidade da loja consultada, quando informada pela API.</param>
/// <param name="Preco">Preço encontrado; null quando a rede não tem o produto.</param>
/// <param name="Disponivel">false quando a rede não tem o produto ou a consulta falhou.</param>
/// <param name="Mensagem">Detalhe legível quando indisponível (ex.: "produto não encontrado na rede").</param>
/// <param name="DistanciaKm">Distância estimada (em km) de Ferraz de Vasconcelos até o centro
/// da cidade da loja; null quando a cidade não está no catálogo da região.</param>
/// <param name="UrlProduto">Link da página do produto no site da rede (quando coletado pelo agente).</param>
public sealed record PrecoMercadoRedeResponse(
    string Rede,
    string? Cidade,
    decimal? Preco,
    bool Disponivel,
    string? Mensagem,
    double? DistanciaKm = null,
    string? UrlProduto = null);

/// <summary>
/// Resultado da comparação de preços de mercado de um produto do catálogo.
/// </summary>
/// <param name="OrigemCache">true quando o resultado veio do cache em memória (TTL configurável).</param>
/// <param name="ConsultaFalhou">true quando a API externa falhou/estourou timeout — nesse caso
/// todas as redes vêm com Disponivel=false e Mensagem explicando. Nunca gera erro 500.</param>
public sealed record PrecoMercadoResponse(
    Guid ProdutoId,
    string ProdutoNome,
    string Ean,
    DateTime ConsultadoEm,
    bool OrigemCache,
    bool ConsultaFalhou,
    IReadOnlyList<PrecoMercadoRedeResponse> Precos);
