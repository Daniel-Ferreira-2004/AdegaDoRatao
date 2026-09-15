import { describe, expect, it } from 'vitest'
import { formatCurrency, toDateInputValue } from './format'

// Intl.NumberFormat usa espaço não-quebrável (U+00A0) antes do símbolo
const normalize = (s: string) => s.replace(/ /g, ' ')

describe('formatCurrency', () => {
  it('formata valores em BRL', () => {
    expect(normalize(formatCurrency(1234.5))).toBe('R$ 1.234,50')
  })

  it('formata zero', () => {
    expect(normalize(formatCurrency(0))).toBe('R$ 0,00')
  })
})

describe('toDateInputValue', () => {
  it('gera formato yyyy-mm-dd', () => {
    expect(toDateInputValue(new Date(2026, 0, 5))).toBe('2026-01-05')
  })

  it('preenche mês e dia com zero à esquerda', () => {
    expect(toDateInputValue(new Date(2026, 8, 15))).toBe('2026-09-15')
  })
})
