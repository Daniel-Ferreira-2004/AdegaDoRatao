import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  DollarSign,
  ShoppingCart,
  TrendingUp,
  PackageX,
  AlertTriangle,
  Wallet,
  PiggyBank,
  Receipt,
} from 'lucide-react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getDashboard } from '@/services/dashboard/dashboardService'
import { formatCurrency, formatDateTime } from '@/utils/format'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { PageHeader } from '@/components/common/page-header'
import { ErrorState } from '@/components/feedback/states'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const CHART_COLORS = ['#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ef4444', '#14b8a6']

export default function DashboardPage() {
  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
  })

  if (isError) {
    return (
      <>
        <PageHeader title="Dashboard" description="Visão geral da operação" />
        <ErrorState title="Não foi possível carregar o dashboard." onRetry={() => refetch()} />
      </>
    )
  }

  const summary = data?.summary

  const cards = [
    { title: 'Vendas hoje', value: summary?.vendasHoje, icon: ShoppingCart, format: (v: number) => String(v), to: '/vendas' },
    { title: 'Faturamento hoje', value: summary?.faturamentoHoje, icon: DollarSign, format: formatCurrency, to: '/vendas' },
    { title: 'Faturamento no mês', value: summary?.faturamentoMes, icon: TrendingUp, format: formatCurrency, to: '/vendas' },
    { title: 'Despesas no mês', value: summary?.despesasMes, icon: Receipt, format: formatCurrency, to: '/financeiro' },
    { title: 'Saldo do mês', value: summary?.saldoMes, icon: Wallet, format: formatCurrency, to: '/financeiro/fluxo-de-caixa' },
    { title: 'Lucro bruto estimado (mês)', value: summary?.lucroBrutoEstimadoMes, icon: PiggyBank, format: formatCurrency, to: '/financeiro/fluxo-de-caixa' },
    { title: 'Produtos com estoque baixo', value: summary?.produtosEstoqueBaixo, icon: AlertTriangle, format: (v: number) => String(v), to: '/estoque' },
    { title: 'Produtos sem estoque', value: summary?.produtosSemEstoque, icon: PackageX, format: (v: number) => String(v), to: '/estoque' },
  ]

  return (
    <>
      <PageHeader title="Dashboard" description="Visão geral da operação" />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((card) => (
          <Link key={card.title} to={card.to} className="block">
            <Card className="h-full transition-colors hover:border-primary/50 hover:bg-accent/50">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
                <card.icon className="size-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <Skeleton className="h-8 w-24" />
                ) : (
                  <p className="text-2xl font-bold">{card.format(card.value ?? 0)}</p>
                )}
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Produtos mais vendidos</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : data && data.topProducts.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={data.topProducts} layout="vertical" margin={{ left: 8, right: 16 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                  <XAxis type="number" />
                  <YAxis type="category" dataKey="productName" width={120} tick={{ fontSize: 12 }} />
                  <Tooltip formatter={(value) => [value, 'Qtd. vendida']} />
                  <Bar dataKey="totalQuantitySold" fill="#f59e0b" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma venda registrada no período.</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Formas de pagamento</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : data && data.topPaymentMethods.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={data.topPaymentMethods}
                    dataKey="totalAmount"
                    nameKey="paymentMethodName"
                    innerRadius={60}
                    outerRadius={100}
                    paddingAngle={2}
                    label={({ name, percent }) => `${name} (${((percent ?? 0) * 100).toFixed(0)}%)`}
                  >
                    {data.topPaymentMethods.map((_, i) => (
                      <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value) => formatCurrency(Number(value))} />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma venda registrada no período.</p>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Vendas recentes</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : data && data.recentSales.length > 0 ? (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nº</TableHead>
                    <TableHead>Data</TableHead>
                    <TableHead>Pagamento</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.recentSales.map((sale) => (
                    <TableRow key={sale.id}>
                      <TableCell>#{sale.saleNumber}</TableCell>
                      <TableCell>{formatDateTime(sale.date)}</TableCell>
                      <TableCell>{sale.paymentMethodName}</TableCell>
                      <TableCell className="text-right font-medium">{formatCurrency(sale.total)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma venda recente.</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Movimentações de estoque recentes</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : data && data.recentMovements.length > 0 ? (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Produto</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead className="text-right">Qtd.</TableHead>
                    <TableHead>Data</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.recentMovements.map((mov) => (
                    <TableRow key={mov.id}>
                      <TableCell>{mov.productName}</TableCell>
                      <TableCell>{mov.type}</TableCell>
                      <TableCell className="text-right">{mov.quantity}</TableCell>
                      <TableCell>{formatDateTime(mov.date)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma movimentação recente.</p>
            )}
          </CardContent>
        </Card>
      </div>
    </>
  )
}
