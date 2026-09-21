$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
$termos = @('Coca-Cola 2L', 'Doritos 120g', 'Heineken 269ml')
foreach ($t in $termos) {
    $url = "https://www.atacadao.com.br/io/api/catalog_system/pub/products/search?ft=$([uri]::EscapeDataString($t))&_from=0&_to=5&sc=2"
    try {
        $r = Invoke-WebRequest -Uri $url -UserAgent $ua -UseBasicParsing -TimeoutSec 20
        $j = $r.Content | ConvertFrom-Json
        "=== $t -> $($j.Count) produtos ==="
        foreach ($p in $j) {
            foreach ($i in $p.items) {
                $o = $i.sellers[0].commertialOffer
                "  $($p.productName) | EAN: $($i.ean) | Price: $($o.Price) | Qtd: $($o.AvailableQuantity)"
            }
        }
    } catch { "=== $t -> ERRO: $($_.Exception.Message)" }
}
