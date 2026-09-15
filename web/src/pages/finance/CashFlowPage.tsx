import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ArrowDownCircle, ArrowUpCircle, Scale } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getCashFlow } from '@/services/financial/financialService'
import { formatCurrency, toDateInputValue } from '@/utils/format'
import { PageHeader } from '@/components/common/page-header'
import { ErrorState, LoadingState } from '@/components/feedback/states'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export default function CashFlowPage() {
  const today = toDateInputValue(new Date())
  const monthStart = toDateInputValue(new Date(new Date().getFullYear(), new Date().getMonth(), 1))

  const [from, setFrom] = useState(monthStart)
  const [to, setTo] = useState(today)

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['cashflow', { from, to }],
    queryFn: () =>
      getCashFlow(new Date(from + 'T00:00:00').toISOString(), new Date(to + 'T23:59:59').toISOString()),
    enabled: Boolean(from && to),
  })

  const chartData = data
    ? [
        { name: 'Entradas', valor: data.totalEntradas, fill: '#10b981' },
        { name: 'Saídas', valor: data.totalSaidas, fill: '#ef4444' },
        { name: 'Saldo', valor: data.saldo, fill: data.saldo >= 0 ? '#f59e0b' : '#ef4444' },
      ]
    : []

  return (
    <>
      <PageHeader title="Fluxo de caixa" description="Entradas, saídas e saldo do período" />

      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-3 pt-6">
          <div className="flex items-center gap-2">
            <label htmlFor="from" className="text-sm text-muted-foreground">De</label>
            <Input id="from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="flex items-center gap-2">
            <label htmlFor="to" className="text-sm text-muted-foreground">Até</label>
            <Input id="to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
        </CardContent>
      </Card>

      {isLoading ? (
        <LoadingState message="Carregando fluxo de caixa..." />
      ) : isError ? (
        <ErrorState title="Não foi possível carregar o fluxo de caixa." onRetry={() => refetch()} />
      ) : data ? (
        <>
          <div className="grid gap-4 sm:grid-cols-3">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Entradas</CardTitle>
                <ArrowUpCircle className="size-4 text-emerald-500" />
              </CardHeader>
              <CardContent>
                <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400">
                  {formatCurrency(data.totalEntradas)}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Saídas</CardTitle>
                <ArrowDownCircle className="size-4 text-red-500" />
              </CardHeader>
              <CardContent>
                <p className="text-2xl font-bold text-red-600 dark:text-red-400">
                  {formatCurrency(data.totalSaidas)}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Saldo</CardTitle>
                <Scale className="size-4 text-primary" />
              </CardHeader>
              <CardContent>
                <p className={`text-2xl font-bold ${data.saldo >= 0 ? 'text-primary' : 'text-red-600 dark:text-red-400'}`}>
                  {formatCurrency(data.saldo)}
                </p>
              </CardContent>
            </Card>
          </div>

          <Card className="mt-4">
            <CardHeader>
              <CardTitle>Resumo do período</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="name" />
                  <YAxis tickFormatter={(v) => formatCurrency(Number(v))} width={110} />
                  <Tooltip formatter={(value) => formatCurrency(Number(value))} />
                  <Bar dataKey="valor" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </>
      ) : null}
    </>
  )
}
