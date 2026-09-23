// Ferramenta única: corrige linhas legadas de MarketPriceSnapshots que
// ficaram com string vazia nas colunas de enum (TipoPreco/Confianca) após
// a migration AddConfiancaTipoPrecoSnapshot.
// Uso: dotnet run -- "<connection string>"
using Npgsql;

var cs = args[0];
await using var cn = new NpgsqlConnection(cs);
await cn.OpenAsync();

foreach (var sql in new[]
{
    "UPDATE \"MarketPriceSnapshots\" SET \"TipoPreco\"='Normal' WHERE \"TipoPreco\"=''",
    "UPDATE \"MarketPriceSnapshots\" SET \"Confianca\"='Unverified' WHERE \"Confianca\"=''"
})
{
    await using var cmd = new NpgsqlCommand(sql, cn);
    var n = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"{sql} => {n} linha(s)");
}

await using var count = new NpgsqlCommand("SELECT COUNT(*) FROM \"MarketPriceSnapshots\"", cn);
Console.WriteLine($"Total de snapshots: {await count.ExecuteScalarAsync()}");
