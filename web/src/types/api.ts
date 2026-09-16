// Tipos espelhando os DTOs da API .NET (JSON camelCase, enums como inteiros)

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
}

// ---- Auth ----
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  userId: string
  name: string
  role: string
  permissions: string[]
}

// ---- Catálogo (categorias, marcas, formas de pagamento) ----
export interface NamedEntityRequest {
  name: string
}

export interface NamedEntityResponse {
  id: string
  name: string
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface ActiveStatusRequest {
  isActive: boolean
}

// ---- Produtos ----
export interface ProductQuery {
  name?: string
  sku?: string
  categoryId?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

export interface ProductResponse {
  id: string
  name: string
  description: string | null
  sku: string
  barcode: string | null
  categoryId: string
  brandId: string
  unitOfMeasure: string
  costPrice: number
  salePrice: number
  currentStock: number
  minStock: number
  maxStock: number | null
  allowNegativeStock: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateProductRequest {
  name: string
  description?: string | null
  sku: string
  barcode?: string | null
  categoryId: string
  brandId: string
  unitOfMeasure: string
  costPrice: number
  salePrice: number
  minStock: number
  maxStock?: number | null
  allowNegativeStock: boolean
}

export interface UpdateProductRequest {
  name: string
  description?: string | null
  barcode?: string | null
  categoryId: string
  brandId: string
  unitOfMeasure: string
  minStock: number
  maxStock?: number | null
  allowNegativeStock: boolean
}

export interface ChangeProductPricesRequest {
  costPrice: number
  salePrice: number
}

// ---- Estoque ----
export const StockMovementType = { Entrada: 1, Saida: 2, Ajuste: 3 } as const
export type StockMovementTypeValue = (typeof StockMovementType)[keyof typeof StockMovementType]

export interface RegisterStockMovementRequest {
  productId: string
  type: StockMovementTypeValue
  quantity: number
  reason: string
}

export interface StockMovementResponse {
  id: string
  productId: string
  type: StockMovementTypeValue
  quantity: number
  previousStock: number
  newStock: number
  reason: string
  userId: string
  createdAt: string
  referenceType: string | null
  referenceId: string | null
}

// ---- Vendas ----
export const SaleStatus = { Concluida: 1, Cancelada: 2 } as const
export type SaleStatusValue = (typeof SaleStatus)[keyof typeof SaleStatus]

export interface SaleItemRequest {
  productId: string
  quantity: number
}

export interface CreateSaleRequest {
  items: SaleItemRequest[]
  discount: number
  paymentMethodId: string
}

export interface SaleItemResponse {
  productId: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface SaleResponse {
  id: string
  saleNumber: number
  date: string
  discount: number
  total: number
  paymentMethodId: string
  status: SaleStatusValue
  items: SaleItemResponse[]
}

export interface SaleListItemResponse {
  id: string
  saleNumber: number
  date: string
  discount: number
  total: number
  paymentMethodId: string
  status: SaleStatusValue
  itemCount: number
}

export interface SaleQuery {
  from?: string
  to?: string
  status?: SaleStatusValue
  page?: number
  pageSize?: number
}

// ---- Compras ----
export const PurchaseStatus = { Pendente: 1, Recebida: 2, Cancelada: 3 } as const
export type PurchaseStatusValue = (typeof PurchaseStatus)[keyof typeof PurchaseStatus]

export interface PurchaseItemRequest {
  productId: string
  quantity: number
  unitCost: number
}

export interface CreatePurchaseRequest {
  supplierId: string
  date: string
  discount: number
  freight: number
  paymentMethodId: string
  items: PurchaseItemRequest[]
}

export interface PurchaseResponse {
  id: string
  supplierId: string
  date: string
  discount: number
  freight: number
  total: number
  status: PurchaseStatusValue
}

// ---- Fornecedores ----
export interface SupplierRequest {
  name: string
  document?: string | null
  phone?: string | null
  email?: string | null
}

export interface SupplierResponse {
  id: string
  name: string
  document: string | null
  phone: string | null
  email: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

// ---- Financeiro ----
export const FinancialTransactionType = { Entrada: 1, Saida: 2 } as const
export type FinancialTransactionTypeValue =
  (typeof FinancialTransactionType)[keyof typeof FinancialTransactionType]

export const FinancialTransactionStatus = { Pendente: 1, Pago: 2, Estornado: 3 } as const
export type FinancialTransactionStatusValue =
  (typeof FinancialTransactionStatus)[keyof typeof FinancialTransactionStatus]

export interface CreateFinancialCategoryRequest {
  name: string
  type: FinancialTransactionTypeValue
}

export interface FinancialCategoryResponse {
  id: string
  name: string
  type: FinancialTransactionTypeValue
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateFinancialTransactionRequest {
  description: string
  type: FinancialTransactionTypeValue
  financialCategoryId: string
  amount: number
  date: string
  paymentMethodId?: string | null
  notes?: string | null
}

export interface FinancialTransactionResponse {
  id: string
  description: string
  type: FinancialTransactionTypeValue
  financialCategoryId: string
  financialCategoryName: string
  amount: number
  date: string
  paymentMethodId: string | null
  status: FinancialTransactionStatusValue
  userId: string
  referenceType: string | null
  referenceId: string | null
  notes: string | null
  createdAt: string
}

export interface FinancialTransactionQuery {
  from?: string
  to?: string
  type?: FinancialTransactionTypeValue
  page?: number
  pageSize?: number
}

export interface CashFlowResponse {
  from: string
  to: string
  totalEntradas: number
  totalSaidas: number
  saldo: number
}

// ---- Dashboard ----
export interface DashboardSummary {
  vendasHoje: number
  vendasMes: number
  faturamentoHoje: number
  faturamentoMes: number
  despesasMes: number
  saldoMes: number
  produtosEstoqueBaixo: number
  produtosSemEstoque: number
  lucroBrutoEstimadoMes: number
}

export interface RecentSale {
  id: string
  saleNumber: number
  date: string
  total: number
  paymentMethodName: string
  itemCount: number
}

export interface RecentStockMovement {
  id: string
  productName: string
  type: string
  quantity: number
  date: string
  reason: string | null
}

export interface TopSellingProduct {
  productId: string
  productName: string
  sku: string
  totalQuantitySold: number
  totalRevenue: number
}

export interface TopPaymentMethod {
  paymentMethodId: string
  paymentMethodName: string
  totalSales: number
  totalAmount: number
}

export interface DashboardResponse {
  summary: DashboardSummary
  recentSales: RecentSale[]
  recentMovements: RecentStockMovement[]
  topProducts: TopSellingProduct[]
  topPaymentMethods: TopPaymentMethod[]
}

// ---- Auditoria ----
export interface AuditLogResponse {
  id: string
  userId: string | null
  userEmail: string | null
  action: string
  entityName: string
  entityId: string
  oldValues: string | null
  newValues: string | null
  createdAt: string
}

export interface AuditLogQuery {
  entityName?: string
  entityId?: string
  userId?: string
  action?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}
