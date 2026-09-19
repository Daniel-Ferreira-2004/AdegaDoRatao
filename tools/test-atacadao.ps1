$r = Invoke-WebRequest -Uri 'https://www.atacadao.com.br/io/api/catalog_system/pub/products/search?ft=Heineken%20269ml&_from=0&_to=5' -UserAgent 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36' -UseBasicParsing -TimeoutSec 20
"STATUS: $($r.StatusCode) LEN: $($r.Content.Length)"
$j = $r.Content | ConvertFrom-Json
"PRODUTOS: $($j.Count)"
foreach ($p in $j) {
    "NOME: $($p.productName)"
    foreach ($i in $p.items) {
        $offer = $i.sellers[0].commertialOffer
        "  EAN: $($i.ean) | Price: $($offer.Price) | ListPrice: $($offer.ListPrice) | Qtd: $($offer.AvailableQuantity)"
    }
}
