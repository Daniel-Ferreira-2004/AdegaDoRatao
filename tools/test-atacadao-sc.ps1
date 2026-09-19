foreach ($sc in 1..5) {
    try {
        $r = Invoke-WebRequest -Uri "https://www.atacadao.com.br/io/api/catalog_system/pub/products/search?ft=Heineken%20269ml&_from=0&_to=2&sc=$sc" -UserAgent 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36' -UseBasicParsing -TimeoutSec 20
        $j = $r.Content | ConvertFrom-Json
        $offer = $j[0].items[0].sellers[0].commertialOffer
        "sc=$sc -> Price: $($offer.Price) | Qtd: $($offer.AvailableQuantity) | Sellers: $($j[0].items[0].sellers.Count)"
    } catch { "sc=$sc -> ERRO" }
}
