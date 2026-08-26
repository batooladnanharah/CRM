import type { TicketStatus } from '@/types/tickets'

export interface TicketVolume {
  total: number
  open: number
  resolved: number
}

export interface StatusCount {
  status: TicketStatus
  count: number
}

export interface AgentPerformance {
  agentId: string
  displayName: string
  ticketCount: number
}

export interface SlaPerformance {
  totalEvaluated: number
  withinSla: number
  atRisk: number
  breached: number
  withinSlaPercent: number
  atRiskPercent: number
  breachedPercent: number
}

export interface ResolutionMetrics {
  resolvedTicketCount: number
  averageResolutionMinutes: number | null
}

export interface ReportsSummary {
  ticketVolume: TicketVolume
  statusDistribution: StatusCount[]
  agentPerformance: AgentPerformance[]
  slaPerformance: SlaPerformance
  resolution: ResolutionMetrics
}
