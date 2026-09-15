import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Eye, Plus, XCircle } from 'lucide-react'
import { cancelSale, getSale, getSales } from '@/services/sales/salesService'
import { paymentMethodsService } from '@/services/catalog/catalogService'
import { SaleStatus, type SaleListItemResponse } from '@/types/api'
import { formatCurrency, formatDateTime, toDateInputValue } from '@/utils/format'
import { saleStatusLabel, saleStatusVariant } from '@/utils/enums'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { Pagination } from '@/components/common/pagination'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { ConfirmDialog } from '@/components/feedback/confirm-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

export default function SalesPage() {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission('sales.write')
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const today = toDateInputValue(new Date())
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1))

  const [from, setFrom] = useState(monthStart)
  const [to, setTo] = useState(today)
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [cancelling, setCancelling] = useState<SaleListItemResponse | null>(null)

  const { data: paymentMethods } = useQuery({
    queryKey: ['payment-methods'],
    queryFn: paymentMethodsService.list,
  })

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['sales', { from, to, status, page }],
    queryFn: () =>
      getSales({
        from: from ? new Date(from + 'T00:00:00').toISOString() : undefined,
        to: to ? new Date(to + 'T23:59:59').toISOString() : undefined,
        status: status ? (Number(status) as 1 | 2) : undefined,
        page,
        pageSize: 20,
      }),
    placeholderData: keepPreviousData,
  })

  const detail = useQuery({
    queryKey: ['sales', 'detail', detailId],
    queryFn: () => getSale(detailId!),
    enabled: detailId !== null,
  })

  const cancelMutation = useMutation({
    mutationFn: (id: string) => cancelSale(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast({ variant: 'success', title: 'Venda cancelada.' })
      setCancelling(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível cancelar a venda.', description: error.message })
      setCancelling(null)
    },
  })

  const paymentMethodName = (id: string) =>
    paymentMethods?.find((m) => m.id === id)?.name ?? '—'

  return (
    <>
      <PageHeader
        title="Vendas"
        description="Histórico de vendas realizadas"
        actions={
          canWrite && (
            <Link
              to="/vendas/nova"
              className="inline-flex h-9 items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 [&_svg]:size-4"
            >
              <Plus /> Nova venda
            </Link>
          )
        }
      />

      <Card>
        <CardContent className="pt-6">
          <div className="mb-4 flex flex-wrap gap-3">
            <div className="flex items-center gap-2">
              <label htmlFor="from" className="text-sm text-muted-foreground">De</label>
              <Input id="from" type="date" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1) }} />
            </div>
            <div className="flex items-center gap-2">
              <label htmlFor="to" className="text-sm text-muted-foreground">Até</label>
              <Input id="to" type="date" value={to} onChange={(e) => { setTo(e.target.value); setPage(1) }} />
            </div>
            <Select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1) }}
              className="max-w-[160px]"
              aria-label="Filtrar por status"
            >
              <option value="">Todos os status</option>
              <option value={SaleStatus.Concluida}>Concluídas</option>
              <option value={SaleStatus.Cancelada}>Canceladas</option>
            </Select>
          </div>

          {isLoading ? (
            <LoadingState message="Carregando vendas..." />
          ) : isError ? (
            <ErrorState title="Não foi possível carregar as vendas." onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState title="Nenhuma venda encontrada" description="Ajuste o período ou registre uma nova venda." />
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nº</TableHead>
                    <TableHead>Data</TableHead>
                    <TableHead>Pagamento</TableHead>
                    <TableHead className="text-right">Itens</TableHead>
                    <TableHead className="text-right">Desconto</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="w-24">Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((sale) => (
                    <TableRow key={sale.id}>
                      <TableCell className="font-medium">#{sale.saleNumber}</TableCell>
                      <TableCell>{formatDateTime(sale.date)}</TableCell>
                      <TableCell>{paymentMethodName(sale.paymentMethodId)}</TableCell>
                      <TableCell className="text-right">{sale.itemCount}</TableCell>
                      <TableCell className="text-right">{formatCurrency(sale.discount)}</TableCell>
                      <TableCell className="text-right font-medium">{formatCurrency(sale.total)}</TableCell>
                      <TableCell>
                        <Badge variant={saleStatusVariant[sale.status]}>{saleStatusLabel[sale.status]}</Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button variant="ghost" size="icon" onClick={() => setDetailId(sale.id)} aria-label="Ver detalhes">
                            <Eye />
                          </Button>
                          {canWrite && sale.status === SaleStatus.Concluida && (
                            <Button variant="ghost" size="icon" onClick={() => setCancelling(sale)} aria-label="Cancelar venda">
                              <XCircle className="text-destructive" />
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pagination result={data} onPageChange={setPage} />
            </>
          )}
        </CardContent>
      </Card>

      <Dialog open={detailId !== null} onOpenChange={(open) => !open && setDetailId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {detail.data ? `Venda #${detail.data.saleNumber}` : 'Detalhes da venda'}
            </DialogTitle>
          </DialogHeader>
          {detail.isLoading ? (
            <LoadingState />
          ) : detail.data ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-2 text-sm">
                <span className="text-muted-foreground">Data</span>
                <span>{formatDateTime(detail.data.date)}</span>
                <span className="text-muted-foreground">Pagamento</span>
                <span>{paymentMethodName(detail.data.paymentMethodId)}</span>
                <span className="text-muted-foreground">Status</span>
                <span>
                  <Badge variant={saleStatusVariant[detail.data.status]}>
                    {saleStatusLabel[detail.data.status]}
                  </Badge>
                </span>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Produto</TableHead>
                    <TableHead className="text-right">Qtd.</TableHead>
                    <TableHead className="text-right">Unitário</TableHead>
                    <TableHead className="text-right">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.data.items.map((item) => (
                    <TableRow key={item.productId}>
                      <TableCell className="font-mono text-xs">{item.productId.slice(0, 8)}…</TableCell>
                      <TableCell className="text-right">{item.quantity}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.unitPrice)}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.subtotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <div className="space-y-1 border-t pt-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Desconto</span>
                  <span>- {formatCurrency(detail.data.discount)}</span>
                </div>
                <div className="flex justify-between text-base font-bold">
                  <span>Total</span>
                  <span>{formatCurrency(detail.data.total)}</span>
                </div>
              </div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={cancelling !== null}
        onOpenChange={(open) => !open && setCancelling(null)}
        title="Cancelar venda"
        description={`Tem certeza que deseja cancelar a venda #${cancelling?.saleNumber}? O estoque será estornado. Essa ação não poderá ser desfeita.`}
        confirmLabel="Cancelar venda"
        loading={cancelMutation.isPending}
        onConfirm={() => cancelling && cancelMutation.mutate(cancelling.id)}
      />
    </>
  )
}
