$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
foreach ($sc in 1..10) {
    try {
        $r = Invoke-WebRequest -Uri "https://www.atacadao.com.br/io/api/catalog_system/pub/products/search?ft=Doritos%20120g&_from=0&_to=5&sc=$sc" -UserAgent $ua -UseBasicParsing -TimeoutSec 15
        $j = $r.Content | ConvertFrom-Json
        $doritos = $j | ForEach-Object { $_.items } | Where-Object { $_.ean -eq '7892840822446' } | Select-Object -First 1
        if ($doritos) {
            $o = $doritos.sellers[0].commertialOffer
            "sc=$sc -> Price: $($o.Price) | Qtd: $($o.AvailableQuantity) | Seller: $($doritos.sellers[0].sellerName)"
        } else { "sc=$sc -> EAN nao encontrado ($($j.Count) produtos)" }
    } catch { "sc=$sc -> ERRO" }
}
