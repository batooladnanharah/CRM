import { apiRequest } from './http'
import type { ReportsSummary } from '@/types/reports'

export function getReportsSummary(): Promise<ReportsSummary> {
  return apiRequest<ReportsSummary>('/reports/summary', { method: 'GET' })
}
