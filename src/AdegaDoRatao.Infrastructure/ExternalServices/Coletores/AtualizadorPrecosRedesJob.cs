using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Job em background que atualiza os snapshots de preços das redes uma vez
/// por dia (por padrão às 06h, configurável em Coletores:HoraDiaria).
///
/// Roda com escopo próprio de DI (os serviços são Scoped) e nunca derruba
/// a aplicação: qualquer falha é logada e a próxima execução ocorre no dia
/// seguinte.
/// </summary>
public sealed class AtualizadorPrecosRedesJob(
    IServiceScopeFactory scopeFactory,
    ILogger<AtualizadorPrecosRedesJob> logger) : BackgroundService
{
    private static readonly TimeSpan HoraDiaria = new(6, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var agora = DateTime.Now;
            var proxima = agora.Date + HoraDiaria;
            if (proxima <= agora)
            {
                proxima = proxima.AddDays(1);
            }

            await Task.Delay(proxima - agora, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var atualizador = scope.ServiceProvider.GetRequiredService<IAtualizadorPrecosRedesService>();
                var total = await atualizador.AtualizarTodosAsync(stoppingToken);
                logger.LogInformation("Job de preços concluído: {Total} EANs processados.", total);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha no job diário de atualização de preços.");
            }
        }
    }
}
