export interface DashboardSummary {
  openAssignedCount: number
  needsActionCount: number
  resolvedLast7DaysCount: number
  slaAtRiskCount: number
}

export interface RecentCustomerEntry {
  id: string
  name: string
  lastInteractionAtUtc: string
}
