import {
  FinancialTransactionStatus,
  FinancialTransactionType,
  PurchaseStatus,
  SaleStatus,
  StockMovementType,
} from '@/types/api'

type BadgeVariant = 'default' | 'success' | 'warning' | 'destructive' | 'secondary'

export const stockMovementTypeLabel: Record<number, string> = {
  [StockMovementType.Entrada]: 'Entrada',
  [StockMovementType.Saida]: 'Saída',
  [StockMovementType.Ajuste]: 'Ajuste',
}

export const stockMovementTypeVariant: Record<number, BadgeVariant> = {
  [StockMovementType.Entrada]: 'success',
  [StockMovementType.Saida]: 'destructive',
  [StockMovementType.Ajuste]: 'warning',
}

export const saleStatusLabel: Record<number, string> = {
  [SaleStatus.Concluida]: 'Concluída',
  [SaleStatus.Cancelada]: 'Cancelada',
}

export const saleStatusVariant: Record<number, BadgeVariant> = {
  [SaleStatus.Concluida]: 'success',
  [SaleStatus.Cancelada]: 'destructive',
}

export const purchaseStatusLabel: Record<number, string> = {
  [PurchaseStatus.Pendente]: 'Pendente',
  [PurchaseStatus.Recebida]: 'Recebida',
  [PurchaseStatus.Cancelada]: 'Cancelada',
}

export const purchaseStatusVariant: Record<number, BadgeVariant> = {
  [PurchaseStatus.Pendente]: 'warning',
  [PurchaseStatus.Recebida]: 'success',
  [PurchaseStatus.Cancelada]: 'destructive',
}

export const financialTypeLabel: Record<number, string> = {
  [FinancialTransactionType.Entrada]: 'Entrada',
  [FinancialTransactionType.Saida]: 'Saída',
}

export const financialTypeVariant: Record<number, BadgeVariant> = {
  [FinancialTransactionType.Entrada]: 'success',
  [FinancialTransactionType.Saida]: 'destructive',
}

export const financialStatusLabel: Record<number, string> = {
  [FinancialTransactionStatus.Pendente]: 'Pendente',
  [FinancialTransactionStatus.Pago]: 'Pago',
  [FinancialTransactionStatus.Estornado]: 'Estornado',
}

export const financialStatusVariant: Record<number, BadgeVariant> = {
  [FinancialTransactionStatus.Pendente]: 'warning',
  [FinancialTransactionStatus.Pago]: 'success',
  [FinancialTransactionStatus.Estornado]: 'secondary',
}
