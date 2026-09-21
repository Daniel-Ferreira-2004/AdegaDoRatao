$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
# Cookies de regionalização VTEX com o CEP de Ferraz de Vasconcelos
$session.Cookies.Add([System.Net.Cookie]::new('VTEXSC', 'sc=2', '/', 'www.atacadao.com.br'))
$session.Cookies.Add([System.Net.Cookie]::new('postalCode', '08503-000', '/', 'www.atacadao.com.br'))
$session.Cookies.Add([System.Net.Cookie]::new('region', '08503000', '/', 'www.atacadao.com.br'))

# 1) Simula o checkout de região (endpoint que a VTEX usa para trocar o CEP)
try {
    $r0 = Invoke-WebRequest -Uri 'https://www.atacadao.com.br/api/checkout/pub/regions?postalCode=08503000' -UserAgent $ua -WebSession $session -UseBasicParsing -TimeoutSec 15
    "REGIAO: $($r0.StatusCode) $($r0.Content.Substring(0, [Math]::Min(300, $r0.Content.Length)))"
} catch { "REGIAO ERRO: $($_.Exception.Message)" }

# 2) Busca o Doritos com a sessão regionalizada
foreach ($sc in 1..2) {
    try {
        $r = Invoke-WebRequest -Uri "https://www.atacadao.com.br/io/api/catalog_system/pub/products/search?ft=Doritos%20120g&_from=0&_to=5&sc=$sc" -UserAgent $ua -WebSession $session -UseBasicParsing -TimeoutSec 15
        $j = $r.Content | ConvertFrom-Json
        foreach ($p in $j) {
            foreach ($i in $p.items) {
                $o = $i.sellers[0].commertialOffer
                "sc=$sc | $($p.productName) | EAN: $($i.ean) | Price: $($o.Price) | Qtd: $($o.AvailableQuantity)"
            }
        }
    } catch { "sc=$sc ERRO" }
}
