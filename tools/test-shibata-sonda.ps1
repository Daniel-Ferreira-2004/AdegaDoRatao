$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'

"=== SHIBATA ==="
try {
    $r = Invoke-WebRequest -Uri 'https://www.loja.shibata.com.br/busca?q=heineken' -UserAgent $ua -UseBasicParsing -TimeoutSec 25
    "STATUS: $($r.StatusCode) LEN: $($r.Content.Length)"
    if ($r.Content -match 'heineken') { "contem 'heineken'" } else { "NAO contem 'heineken'" }
    if ($r.Content -match 'R\$') { "contem R$" } else { "NAO contem R$" }
    # procura pistas de API
    [regex]::Matches($r.Content, '(https?://[^"'']*api[^"'']*)') | Select-Object -First 5 | ForEach-Object { $_.Value }
} catch { "ERRO: $($_.Exception.Message)" }

"`n=== SONDA ==="
try {
    $r2 = Invoke-WebRequest -Uri 'https://www.sondadelivery.com.br' -UserAgent $ua -UseBasicParsing -TimeoutSec 25
    "STATUS: $($r2.StatusCode) LEN: $($r2.Content.Length)"
    if ($r2.Content -match 'txt-busca-nova') { "tem campo .txt-busca-nova" } else { "NAO tem .txt-busca-nova" }
} catch { "ERRO: $($_.Exception.Message)" }
