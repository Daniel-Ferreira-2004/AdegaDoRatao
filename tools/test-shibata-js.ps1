$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
$r = Invoke-WebRequest -Uri 'https://www.loja.shibata.com.br' -UserAgent $ua -UseBasicParsing -TimeoutSec 20
# Extrai os arquivos JS referenciados
$scripts = [regex]::Matches($r.Content, 'src="([^"]+\.js[^"]*)"') | ForEach-Object { $_.Groups[1].Value }
"SCRIPTS:"
$scripts | ForEach-Object { "  $_" }
# Baixa o primeiro JS e procura URLs de API
foreach ($s in $scripts | Select-Object -First 3) {
    $url = if ($s.StartsWith('http')) { $s } else { 'https://www.loja.shibata.com.br' + $s }
    try {
        $js = (Invoke-WebRequest -Uri $url -UserAgent $ua -UseBasicParsing -TimeoutSec 30).Content
        "JS $url [$($js.Length)]"
        [regex]::Matches($js, 'https?://[a-zA-Z0-9./_-]*api[a-zA-Z0-9./_-]*') | Select-Object -First 10 | ForEach-Object { "  API: $($_.Value)" }
        [regex]::Matches($js, '"/api/[^"]*"') | Select-Object -First 10 | ForEach-Object { "  PATH: $($_.Value)" }
    } catch { "  ERRO ao baixar $url" }
}
