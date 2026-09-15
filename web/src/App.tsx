import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/contexts/AuthContext'
import { ToastProvider } from '@/components/feedback/toast'
import { AppLayout } from '@/layouts/AppLayout'
import { RequireAuth, RequirePermission } from '@/routes/guards'
import { LoadingState } from '@/components/feedback/states'
import LoginPage from '@/pages/auth/LoginPage'

const DashboardPage = lazy(() => import('@/pages/dashboard/DashboardPage'))
const ProductsPage = lazy(() => import('@/pages/products/ProductsPage'))
const CategoriesPage = lazy(() => import('@/pages/categories/CategoriesPage'))
const StockPage = lazy(() => import('@/pages/stock/StockPage'))
const StockMovementPage = lazy(() => import('@/pages/stock/StockMovementPage'))
const NewSalePage = lazy(() => import('@/pages/sales/NewSalePage'))
const SalesPage = lazy(() => import('@/pages/sales/SalesPage'))
const NewPurchasePage = lazy(() => import('@/pages/purchases/NewPurchasePage'))
const SuppliersPage = lazy(() => import('@/pages/suppliers/SuppliersPage'))
const FinancialPage = lazy(() => import('@/pages/finance/FinancialPage'))
const CashFlowPage = lazy(() => import('@/pages/finance/CashFlowPage'))
const FinancialCategoriesPage = lazy(() => import('@/pages/finance/FinancialCategoriesPage'))
const PaymentMethodsPage = lazy(() => import('@/pages/payment-methods/PaymentMethodsPage'))
const AuditPage = lazy(() => import('@/pages/audit/AuditPage'))

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
})

function Protected({ permission, children }: { permission: string; children: React.ReactElement }) {
  return <RequirePermission permission={permission}>{children}</RequirePermission>
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                element={
                  <RequireAuth>
                    <AppLayout />
                  </RequireAuth>
                }
              >
                <Route
                  index
                  element={
                    <Suspense fallback={<LoadingState />}>
                      <Protected permission="dashboard.read"><DashboardPage /></Protected>
                    </Suspense>
                  }
                />
                <Route path="produtos" element={<Suspense fallback={<LoadingState />}><Protected permission="products.read"><ProductsPage /></Protected></Suspense>} />
                <Route path="categorias" element={<Suspense fallback={<LoadingState />}><Protected permission="products.read"><CategoriesPage /></Protected></Suspense>} />
                <Route path="estoque" element={<Suspense fallback={<LoadingState />}><Protected permission="stock.read"><StockPage /></Protected></Suspense>} />
                <Route path="estoque/movimentar" element={<Suspense fallback={<LoadingState />}><Protected permission="stock.write"><StockMovementPage /></Protected></Suspense>} />
                <Route path="vendas" element={<Suspense fallback={<LoadingState />}><Protected permission="sales.read"><SalesPage /></Protected></Suspense>} />
                <Route path="vendas/nova" element={<Suspense fallback={<LoadingState />}><Protected permission="sales.write"><NewSalePage /></Protected></Suspense>} />
                <Route path="compras/nova" element={<Suspense fallback={<LoadingState />}><Protected permission="purchases.write"><NewPurchasePage /></Protected></Suspense>} />
                <Route path="fornecedores" element={<Suspense fallback={<LoadingState />}><Protected permission="purchases.read"><SuppliersPage /></Protected></Suspense>} />
                <Route path="financeiro" element={<Suspense fallback={<LoadingState />}><Protected permission="financial.read"><FinancialPage /></Protected></Suspense>} />
                <Route path="financeiro/fluxo-de-caixa" element={<Suspense fallback={<LoadingState />}><Protected permission="financial.read"><CashFlowPage /></Protected></Suspense>} />
                <Route path="financeiro/categorias" element={<Suspense fallback={<LoadingState />}><Protected permission="financial.read"><FinancialCategoriesPage /></Protected></Suspense>} />
                <Route path="formas-de-pagamento" element={<Suspense fallback={<LoadingState />}><Protected permission="sales.read"><PaymentMethodsPage /></Protected></Suspense>} />
                <Route path="auditoria" element={<Suspense fallback={<LoadingState />}><Protected permission="audit.read"><AuditPage /></Protected></Suspense>} />
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </BrowserRouter>
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}
