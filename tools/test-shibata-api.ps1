$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
$base = 'https://www.loja.shibata.com.br'

# Tenta endpoints comuns de API de e-commerce
$endpoints = @(
    '/api/catalog_system/pub/products/search?ft=heineken&_from=0&_to=2',
    '/io/api/catalog_system/pub/products/search?ft=heineken&_from=0&_to=2',
    '/api/products?search=heineken',
    '/api/v1/products?search=heineken',
    '/busca?q=heineken&format=json'
)

foreach ($ep in $endpoints) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $ep) -UserAgent $ua -UseBasicParsing -TimeoutSec 15
        $len = $r.Content.Length
        $preview = $r.Content.Substring(0, [Math]::Min(200, $len))
        "OK $($r.StatusCode) [$len] $ep"
        "   $preview"
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'ERR' }
        "FAIL $code $ep"
    }
}
