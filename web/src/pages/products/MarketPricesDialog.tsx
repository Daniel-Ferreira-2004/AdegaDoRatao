import { useEffect, useMemo, useRef } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { TrendingDown, MapPin, RefreshCw, ExternalLink } from 'lucide-react'
import { atualizarPrecosMercado, getPrecosMercado } from '@/services/products/productsService'
import { useToast } from '@/components/feedback/toast'
import type { PrecoMercadoRedeResponse, ProductResponse } from '@/types/api'
import { formatCurrency } from '@/utils/format'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'

interface MarketPricesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  product: ProductResponse | null
}

/**
 * Ordenação "inteligente": disponíveis primeiro, do menor para o maior
 * preço; em empate de preço, o mais perto de Ferraz de Vasconcelos ganha;
 * indisponíveis (sem preço) sempre no final, do mais perto ao mais longe.
 * (A API já devolve nessa ordem — este sort é apenas uma garantia local.)
 */
function ordenar(precos: PrecoMercadoRedeResponse[]): PrecoMercadoRedeResponse[] {
  return [...precos].sort((a, b) => {
    if (a.disponivel !== b.disponivel) return a.disponivel ? -1 : 1
    const precoA = a.preco ?? Number.MAX_VALUE
    const precoB = b.preco ?? Number.MAX_VALUE
    if (precoA !== precoB) return precoA - precoB
    return (a.distanciaKm ?? Number.MAX_VALUE) - (b.distanciaKm ?? Number.MAX_VALUE)
  })
}

export function MarketPricesDialog({ open, onOpenChange, product }: MarketPricesDialogProps) {
  const { toast } = useToast()
  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['precos-mercado', product?.id],
    queryFn: () => getPrecosMercado(product!.id),
    enabled: open && product !== null,
    staleTime: 5 * 60 * 1000, // evita reconsulta ao reabrir (o backend já cacheia 6h)
  })

  // Dispara a coleta do EAN nas redes (POST /precos/atualizar/{ean}) e
  // depois relê os preços já atualizados.
  const ean = data?.ean ?? product?.barcode ?? null
  const atualizar = useMutation({
    mutationFn: () => atualizarPrecosMercado(ean!),
    onSuccess: async () => {
      await refetch()
      toast({ variant: 'success', title: 'Preços atualizados nas redes.' })
    },
    onError: (error: Error) =>
      toast({ variant: 'error', title: 'Não foi possível atualizar os preços.', description: error.message }),
  })

  // Coleta sob demanda: produto recém-cadastrado ainda não tem snapshot
  // local (origemCache=false) — pesquisa nas redes na hora, uma única vez
  // por abertura do modal (o ref evita loop quando a coleta não encontra
  // o produto em nenhuma rede). Também dispara quando a API externa falhou
  // (consultaFalhou=true), pois nesse caso não há dado local nem externo.
  const coletaDisparada = useRef<string | null>(null)
  useEffect(() => {
    if (!open) {
      coletaDisparada.current = null
      return
    }
    if (
      data &&
      !data.origemCache &&
      ean &&
      !atualizar.isPending &&
      coletaDisparada.current !== product?.id
    ) {
      coletaDisparada.current = product?.id ?? null
      atualizar.mutate()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, data, ean, product?.id])

  const precosOrdenados = useMemo(() => (data ? ordenar(data.precos) : []), [data])
  const menorPreco = precosOrdenados.find((p) => p.disponivel && p.preco !== null)?.preco ?? null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Preços nos mercados</DialogTitle>
          <DialogDescription>
            {product?.name}
            {data?.ean ? ` · EAN ${data.ean}` : ''}
          </DialogDescription>
        </DialogHeader>

        {isLoading || atualizar.isPending ? (
          <LoadingState
            message={
              atualizar.isPending
                ? 'Pesquisando nas redes (Tenda, Atacadão, Shibata, Sonda, D\'avó)...'
                : 'Consultando preços nos mercados...'
            }
          />
        ) : isError ? (
          <ErrorState
            title="Não foi possível consultar os preços."
            onRetry={() => refetch()}
          />
        ) : data?.consultaFalhou ? (
          <EmptyState
            title="Consulta indisponível no momento"
            description="O serviço de preços de mercado não respondeu. Tente novamente em instantes."
          />
        ) : precosOrdenados.length === 0 ? (
          <EmptyState
            title="Nenhum mercado encontrado"
            description="Nenhum mercado da região tem este produto na base de comparação."
          />
        ) : (
          <>
            <ul className="divide-y rounded-md border">
              {precosOrdenados.map((item) => {
                const isMenor = item.disponivel && item.preco !== null && item.preco === menorPreco
                return (
                  <li
                    key={`${item.rede}-${item.cidade ?? ''}`}
                    className="flex items-center justify-between gap-3 px-3 py-2"
                  >
                    <div className="min-w-0">
                      <p className="flex items-center gap-2 font-medium">
                        <span className="truncate">{item.rede}</span>
                        {isMenor && (
                          <Badge variant="success" className="gap-1">
                            <TrendingDown className="h-3 w-3" /> Menor preço
                          </Badge>
                        )}
                        {item.tipoPreco === 'CondicionadoQuantidade' && (
                          <Badge variant="secondary" title="Preço com desconto por quantidade (ex.: a partir de N unidades)">
                            Preço atacado
                          </Badge>
                        )}
                      </p>
                      {item.cidade && (
                        <p className="flex items-center gap-1 text-xs text-muted-foreground">
                          <MapPin className="h-3 w-3" /> {item.cidade}
                          {item.distanciaKm !== null && (
                            <span>· {item.distanciaKm.toLocaleString('pt-BR')} km</span>
                          )}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-2 text-right">
                      {item.disponivel && item.preco !== null ? (
                        <span className={`font-semibold ${isMenor ? 'text-green-600' : ''}`}>
                          {formatCurrency(item.preco)}
                        </span>
                      ) : (
                        <span className="text-sm text-muted-foreground">
                          {item.mensagem ?? 'Indisponível'}
                        </span>
                      )}
                      {item.urlProduto && (
                        <a
                          href={item.urlProduto}
                          target="_blank"
                          rel="noopener noreferrer"
                          title={`Ver produto no site do ${item.rede}`}
                          className="text-muted-foreground transition-colors hover:text-primary"
                        >
                          <ExternalLink className="h-4 w-4" />
                        </a>
                      )}
                    </div>
                  </li>
                )
              })}
            </ul>

            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span>
                {data?.origemCache ? 'Resultado em cache' : 'Consulta ao vivo'}
                {data?.consultadoEm &&
                  ` · ${new Date(data.consultadoEm).toLocaleString('pt-BR')}`}
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => atualizar.mutate()}
                disabled={isFetching || atualizar.isPending || !ean}
                aria-label="Atualizar preços"
              >
                <RefreshCw className={isFetching || atualizar.isPending ? 'animate-spin' : ''} />
                {atualizar.isPending ? 'Coletando...' : 'Atualizar'}
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
