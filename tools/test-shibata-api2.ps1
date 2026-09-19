$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
# A plataforma é "Supermercados Online" — a API deve estar nesse domínio
$base = 'https://supermercadosonline.com.br'
$endpoints = @(
    '/api/products?search=heineken',
    '/api/v1/products?search=heineken',
    '/api/busca?q=heineken',
    '/api/produtos?nome=heineken',
    '/api/catalog_system/pub/products/search?ft=heineken&_from=0&_to=2'
)
foreach ($ep in $endpoints) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $ep) -UserAgent $ua -UseBasicParsing -TimeoutSec 15
        $preview = $r.Content.Substring(0, [Math]::Min(300, $r.Content.Length))
        "OK $($r.StatusCode) [$($r.Content.Length)] $ep"
        "   $preview"
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERR' }
        "FAIL $code $ep"
    }
}
