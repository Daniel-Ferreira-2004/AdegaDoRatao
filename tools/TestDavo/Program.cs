// Ferramenta única: testa o DavoPrecoCollector contra o site real.
// Uso: dotnet run -- "<ean>" "<nome>"
using AdegaDoRatao.Infrastructure.ExternalServices.Coletores;
using Microsoft.Extensions.Logging;

var ean = args.Length > 0 ? args[0] : "7894900027013";
var nome = args.Length > 1 ? args[1] : "Coca-Cola Original 2L";

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<DavoPrecoCollector>();

var collector = new DavoPrecoCollector(logger);
var resultado = await collector.ColetarAsync(ean, nome);

Console.WriteLine("=== RESULTADO ===");
Console.WriteLine($"Sucesso: {resultado.Succeeded}");
if (resultado.Value is not null)
{
    var v = resultado.Value;
    Console.WriteLine($"Rede: {v.Rede}");
    Console.WriteLine($"Nome: {v.NomeProdutoNaRede}");
    Console.WriteLine($"Preço: {v.Preco}");
    Console.WriteLine($"Disponível: {v.Disponivel}");
    Console.WriteLine($"Url: {v.UrlProduto}");
    Console.WriteLine($"Confiança: {v.Confianca}");
    Console.WriteLine($"Região confirmada: {v.RegiaoConfirmada}");
}
else
{
    Console.WriteLine($"Erro: {resultado.Error}");
}
