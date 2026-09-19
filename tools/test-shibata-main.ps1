$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
$js = (Invoke-WebRequest -Uri 'https://www.loja.shibata.com.br/main-4IFY44CD.js' -UserAgent $ua -UseBasicParsing -TimeoutSec 60).Content
"MAIN JS: $($js.Length) bytes"
# URLs de API
[regex]::Matches($js, 'https?://[a-zA-Z0-9./_-]+') | ForEach-Object { $_.Value } | Where-Object { $_ -match 'api|shibata|supermercadosonline' } | Sort-Object -Unique | Select-Object -First 20
"---"
# endpoints relativos
[regex]::Matches($js, '"/[a-z0-9/_-]*(?:produto|busca|search|preco|price)[a-z0-9/_-]*"') | ForEach-Object { $_.Value } | Sort-Object -Unique | Select-Object -First 20
