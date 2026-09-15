import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Undo2 } from 'lucide-react'
import {
  createTransaction,
  getFinancialCategories,
  getTransactions,
  reverseTransaction,
} from '@/services/financial/financialService'
import { paymentMethodsService } from '@/services/catalog/catalogService'
import {
  FinancialTransactionStatus,
  FinancialTransactionType,
} from '@/types/api'
import { formatCurrency, formatDate, toDateInputValue } from '@/utils/format'
import { financialStatusLabel, financialStatusVariant, financialTypeLabel, financialTypeVariant } from '@/utils/enums'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { Pagination } from '@/components/common/pagination'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { ConfirmDialog } from '@/components/feedback/confirm-dialog'
import { Button } from '@/components/ui/button'
import { Input, Textarea } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
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
import { FormField } from '@/components/forms/form-field'

const transactionSchema = z.object({
  description: z.string().min(1, 'Informe a descrição').max(200),
  type: z.union([z.literal(1), z.literal(2)], 'Selecione o tipo'),
  financialCategoryId: z.string().min(1, 'Selecione a categoria'),
  amount: z.number('Informe o valor').positive('Valor deve ser maior que zero'),
  date: z.string().min(1, 'Informe a data'),
  paymentMethodId: z.string().optional().or(z.literal('')),
  notes: z.string().max(500).optional().or(z.literal('')),
})

type TransactionFormData = z.infer<typeof transactionSchema>

export default function FinancialPage() {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission('financial.write')
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const today = toDateInputValue(new Date())
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1))

  const [from, setFrom] = useState(monthStart)
  const [to, setTo] = useState(today)
  const [type, setType] = useState('')
  const [page, setPage] = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const [reversing, setReversing] = useState<string | null>(null)

  const { data: categories } = useQuery({ queryKey: ['financial-categories'], queryFn: getFinancialCategories })
  const { data: paymentMethods } = useQuery({ queryKey: ['payment-methods'], queryFn: paymentMethodsService.list })

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['financial-transactions', { from, to, type, page }],
    queryFn: () =>
      getTransactions({
        from: from ? new Date(from + 'T00:00:00').toISOString() : undefined,
        to: to ? new Date(to + 'T23:59:59').toISOString() : undefined,
        type: type ? (Number(type) as 1 | 2) : undefined,
        page,
        pageSize: 20,
      }),
    placeholderData: keepPreviousData,
  })

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: { date: today },
  })

  const selectedType = watch('type')

  useEffect(() => {
    if (formOpen) reset({ date: today, description: '', type: 0 as never, financialCategoryId: '', amount: 0, paymentMethodId: '', notes: '' })
  }, [formOpen, reset, today])

  const createMutation = useMutation({
    mutationFn: createTransaction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['financial-transactions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast({ variant: 'success', title: 'Transação registrada com sucesso.' })
      setFormOpen(false)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível registrar a transação.', description: error.message })
    },
  })

  const reverseMutation = useMutation({
    mutationFn: reverseTransaction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['financial-transactions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast({ variant: 'success', title: 'Transação estornada.' })
      setReversing(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível estornar.', description: error.message })
      setReversing(null)
    },
  })

  function onSubmit(form: TransactionFormData) {
    createMutation.mutate({
      description: form.description,
      type: form.type as 1 | 2,
      financialCategoryId: form.financialCategoryId,
      amount: form.amount,
      date: new Date(form.date + 'T12:00:00').toISOString(),
      paymentMethodId: form.paymentMethodId || null,
      notes: form.notes || null,
    })
  }

  return (
    <>
      <PageHeader
        title="Financeiro"
        description="Contas a pagar e a receber"
        actions={
          canWrite && (
            <Button onClick={() => setFormOpen(true)}>
              <Plus /> Nova transação
            </Button>
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
              value={type}
              onChange={(e) => { setType(e.target.value); setPage(1) }}
              className="max-w-[160px]"
              aria-label="Filtrar por tipo"
            >
              <option value="">Entradas e saídas</option>
              <option value={FinancialTransactionType.Entrada}>Entradas</option>
              <option value={FinancialTransactionType.Saida}>Saídas</option>
            </Select>
          </div>

          {isLoading ? (
            <LoadingState message="Carregando transações..." />
          ) : isError ? (
            <ErrorState title="Não foi possível carregar as transações." onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState title="Nenhuma transação encontrada" description="Ajuste o período ou registre uma nova transação." />
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Data</TableHead>
                    <TableHead>Descrição</TableHead>
                    <TableHead>Categoria</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead className="text-right">Valor</TableHead>
                    <TableHead>Status</TableHead>
                    {canWrite && <TableHead className="w-12" />}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((t) => (
                    <TableRow key={t.id}>
                      <TableCell>{formatDate(t.date)}</TableCell>
                      <TableCell className="font-medium">{t.description}</TableCell>
                      <TableCell>{t.financialCategoryName}</TableCell>
                      <TableCell>
                        <Badge variant={financialTypeVariant[t.type]}>{financialTypeLabel[t.type]}</Badge>
                      </TableCell>
                      <TableCell className="text-right font-medium">{formatCurrency(t.amount)}</TableCell>
                      <TableCell>
                        <Badge variant={financialStatusVariant[t.status]}>{financialStatusLabel[t.status]}</Badge>
                      </TableCell>
                      {canWrite && (
                        <TableCell>
                          {t.status !== FinancialTransactionStatus.Estornado && (
                            <Button variant="ghost" size="icon" onClick={() => setReversing(t.id)} aria-label="Estornar transação">
                              <Undo2 className="text-destructive" />
                            </Button>
                          )}
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pagination result={data} onPageChange={setPage} />
            </>
          )}
        </CardContent>
      </Card>

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nova transação</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <FormField label="Descrição" htmlFor="description" error={errors.description?.message} required>
              <Input id="description" {...register('description')} />
            </FormField>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label="Tipo" htmlFor="type" error={errors.type?.message} required>
                <Select id="type" {...register('type', { valueAsNumber: true })}>
                  <option value="">Selecione...</option>
                  <option value={FinancialTransactionType.Entrada}>Entrada (a receber)</option>
                  <option value={FinancialTransactionType.Saida}>Saída (a pagar)</option>
                </Select>
              </FormField>
              <FormField label="Categoria" htmlFor="financialCategoryId" error={errors.financialCategoryId?.message} required>
                <Select id="financialCategoryId" {...register('financialCategoryId')}>
                  <option value="">Selecione...</option>
                  {categories
                    ?.filter((c) => c.isActive && Number(selectedType) === c.type)
                    .map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                </Select>
              </FormField>
              <FormField label="Valor (R$)" htmlFor="amount" error={errors.amount?.message} required>
                <Input id="amount" type="number" min="0" step="0.01" {...register('amount', { valueAsNumber: true })} />
              </FormField>
              <FormField label="Data" htmlFor="date" error={errors.date?.message} required>
                <Input id="date" type="date" {...register('date')} />
              </FormField>
            </div>
            <FormField label="Forma de pagamento" htmlFor="paymentMethodId" error={errors.paymentMethodId?.message}>
              <Select id="paymentMethodId" {...register('paymentMethodId')}>
                <option value="">Nenhuma</option>
                {paymentMethods?.filter((m) => m.isActive).map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </Select>
            </FormField>
            <FormField label="Observações" htmlFor="notes" error={errors.notes?.message}>
              <Textarea id="notes" {...register('notes')} />
            </FormField>
            <DialogFooter>
              <Button variant="outline" onClick={() => setFormOpen(false)} disabled={isSubmitting}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Salvando...' : 'Salvar'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={reversing !== null}
        onOpenChange={(open) => !open && setReversing(null)}
        title="Estornar transação"
        description="Tem certeza que deseja estornar esta transação? Essa ação não poderá ser desfeita."
        confirmLabel="Estornar"
        loading={reverseMutation.isPending}
        onConfirm={() => reversing && reverseMutation.mutate(reversing)}
      />
    </>
  )
}
