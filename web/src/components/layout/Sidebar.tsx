import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Package,
  Tags,
  Boxes,
  ArrowDownUp,
  ShoppingCart,
  Receipt,
  Truck,
  Wallet,
  CreditCard,
  ScrollText,
  Wine,
  X,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  permission: string
}

interface NavSection {
  title: string
  items: NavItem[]
}

const navSections: NavSection[] = [
  {
    title: 'Geral',
    items: [{ to: '/', label: 'Dashboard', icon: LayoutDashboard, permission: 'dashboard.read' }],
  },
  {
    title: 'Catálogo',
    items: [
      { to: '/produtos', label: 'Produtos', icon: Package, permission: 'products.read' },
      { to: '/categorias', label: 'Categorias e Marcas', icon: Tags, permission: 'products.read' },
    ],
  },
  {
    title: 'Estoque',
    items: [
      { to: '/estoque', label: 'Estoque', icon: Boxes, permission: 'stock.read' },
      { to: '/estoque/movimentar', label: 'Movimentações', icon: ArrowDownUp, permission: 'stock.write' },
    ],
  },
  {
    title: 'Vendas',
    items: [
      { to: '/vendas/nova', label: 'Nova venda', icon: ShoppingCart, permission: 'sales.write' },
      { to: '/vendas', label: 'Vendas realizadas', icon: Receipt, permission: 'sales.read' },
    ],
  },
  {
    title: 'Compras',
    items: [
      { to: '/compras/nova', label: 'Nova compra', icon: Truck, permission: 'purchases.write' },
      { to: '/fornecedores', label: 'Fornecedores', icon: Truck, permission: 'purchases.read' },
    ],
  },
  {
    title: 'Financeiro',
    items: [
      { to: '/financeiro', label: 'Transações', icon: Wallet, permission: 'financial.read' },
      { to: '/financeiro/fluxo-de-caixa', label: 'Fluxo de caixa', icon: Wallet, permission: 'financial.read' },
      { to: '/financeiro/categorias', label: 'Categorias', icon: Tags, permission: 'financial.read' },
      { to: '/formas-de-pagamento', label: 'Formas de pagamento', icon: CreditCard, permission: 'sales.read' },
    ],
  },
  {
    title: 'Administração',
    items: [{ to: '/auditoria', label: 'Auditoria', icon: ScrollText, permission: 'audit.read' }],
  },
]

interface SidebarProps {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const { hasPermission } = useAuth()

  const visibleSections = navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => hasPermission(item.permission)),
    }))
    .filter((section) => section.items.length > 0)

  return (
    <>
      {open && (
        <div className="fixed inset-0 z-40 bg-black/50 lg:hidden" onClick={onClose} aria-hidden="true" />
      )}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-64 flex-col border-r bg-card transition-transform lg:static lg:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-16 items-center gap-2 border-b px-4">
          <Wine className="size-7 text-primary" />
          <span className="text-lg font-bold">Adega do Ratão</span>
          <Button variant="ghost" size="icon" className="ml-auto lg:hidden" onClick={onClose} aria-label="Fechar menu">
            <X />
          </Button>
        </div>
        <nav className="flex-1 overflow-y-auto p-3" aria-label="Navegação principal">
          {visibleSections.map((section) => (
            <div key={section.title} className="mb-4">
              <p className="mb-1 px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {section.title}
              </p>
              {section.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  onClick={onClose}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-primary/10 text-primary'
                        : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                    )
                  }
                >
                  <item.icon className="size-4" />
                  {item.label}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
      </aside>
    </>
  )
}
