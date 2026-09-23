using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Caso de uso: atualização dos snapshots de preço de mercado.
///
/// Para cada produto com EAN cadastrado, consulta todos os coletores de
/// rede registrados (Tenda, Atacadão, Shibata, Sonda) e grava/atualiza o
/// snapshot (EAN + rede) no banco. Falhas individuais de uma rede não
/// interrompem as demais — o coletor devolve Result.Failure e o EAN é
/// pulado naquela rede.
/// </summary>
public sealed class AtualizadorPrecosRedesService(
    IProductRepository products,
    IMarketPriceSnapshotRepository snapshots,
    IEnumerable<IPrecoRedeCollector> coletores,
    IUnitOfWork unitOfWork) : IAtualizadorPrecosRedesService
{
    public async Task<int> AtualizarTodosAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await products.ListarAtivosComEanAsync(cancellationToken);
        foreach (var (ean, nome) in produtos)
        {
            await ColetarEmTodasAsRedesAsync(ean, nome, cancellationToken);
        }

        return produtos.Count;
    }

    public async Task AtualizarPorEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        var produto = await products.ObterPorCodigoDeBarrasAsync(ean, cancellationToken);
        await ColetarEmTodasAsRedesAsync(ean, produto?.Name, cancellationToken);
    }

    private async Task ColetarEmTodasAsRedesAsync(string ean, string? nomeProduto, CancellationToken cancellationToken)
    {
        foreach (var coletor in coletores)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resultado = await coletor.ColetarAsync(ean, nomeProduto, cancellationToken);
            if (!resultado.Succeeded || resultado.Value is null)
            {
                continue; // falha na rede: pula sem interromper as demais
            }

            var coleta = resultado.Value;
            var existente = await snapshots.ObterPorEanERedeAsync(ean, coletor.Rede, cancellationToken);
            if (existente is null)
            {
                await snapshots.AdicionarAsync(new MarketPriceSnapshot(
                    ean, coletor.Rede, coleta.NomeProdutoNaRede, coleta.Preco, coleta.Disponivel,
                    coleta.UrlProduto, coleta.TipoPreco, coleta.Confianca, coleta.RegiaoConfirmada), cancellationToken);
            }
            else
            {
                existente.Atualizar(coleta.Preco, coleta.Disponivel, coleta.NomeProdutoNaRede, coleta.UrlProduto,
                    coleta.TipoPreco, coleta.Confianca, coleta.RegiaoConfirmada);
                snapshots.Atualizar(existente);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
