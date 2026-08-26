import { ref } from 'vue'
import { defineStore } from 'pinia'
import { listTickets } from '@/api/tickets'
import { useAuthStore } from '@/stores/auth'
import type { TicketListItem, TicketPriority } from '@/types/tickets'
import type { DashboardSummary, RecentCustomerEntry } from '@/types/dashboard'

const MY_TICKETS_PAGE_SIZE = 25
const RECENT_CUSTOMERS_LIMIT = 8
const SLA_AT_RISK_HOURS = 24
const RESOLVED_WINDOW_DAYS = 7

const PRIORITY_ORDER: Record<TicketPriority, number> = { Urgent: 3, High: 2, Normal: 1, Low: 0 }

function isSlaAtRisk(ticket: TicketListItem): boolean {
  const isHighPriority = ticket.priority === 'High' || ticket.priority === 'Urgent'
  const ageMs = Date.now() - new Date(ticket.createdAtUtc).getTime()
  return isHighPriority && ageMs > SLA_AT_RISK_HOURS * 60 * 60 * 1000
}

function dedupeRecentCustomers(tickets: TicketListItem[]): RecentCustomerEntry[] {
  const byCustomerId = new Map<string, RecentCustomerEntry>()

  for (const ticket of tickets) {
    const existing = byCustomerId.get(ticket.customerId)
    if (!existing || ticket.updatedAtUtc > existing.lastInteractionAtUtc) {
      byCustomerId.set(ticket.customerId, {
        id: ticket.customerId,
        name: ticket.customerName,
        lastInteractionAtUtc: ticket.updatedAtUtc,
      })
    }
  }

  return [...byCustomerId.values()]
    .sort((a, b) => b.lastInteractionAtUtc.localeCompare(a.lastInteractionAtUtc))
    .slice(0, RECENT_CUSTOMERS_LIMIT)
}

export const useDashboardStore = defineStore('dashboard', () => {
  const summary = ref<DashboardSummary | null>(null)
  const myOpenTickets = ref<TicketListItem[]>([])
  const myTasks = ref<TicketListItem[]>([])
  const recentCustomers = ref<RecentCustomerEntry[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function load() {
    error.value = null

    const authStore = useAuthStore()
    const agentId = authStore.user?.id
    if (!agentId) {
      error.value = 'noUser'
      return
    }

    const sevenDaysAgo = new Date(Date.now() - RESOLVED_WINDOW_DAYS * 24 * 60 * 60 * 1000).toISOString()

    const [assignedResult, resolvedResult] = await Promise.allSettled([
      listTickets({ assigneeId: agentId, pageSize: MY_TICKETS_PAGE_SIZE }),
      listTickets({
        assigneeId: agentId,
        status: 'Resolved',
        updatedSince: sevenDaysAgo,
        pageSize: MY_TICKETS_PAGE_SIZE,
      }),
    ])

    if (assignedResult.status === 'rejected' || resolvedResult.status === 'rejected') {
      error.value = 'loadFailed'
    }

    const assignedTickets = assignedResult.status === 'fulfilled' ? assignedResult.value.items : []
    const resolvedTotal = resolvedResult.status === 'fulfilled' ? resolvedResult.value.totalCount : 0
    const resolvedTickets = resolvedResult.status === 'fulfilled' ? resolvedResult.value.items : []

    if (assignedResult.status === 'fulfilled') {
      myOpenTickets.value = assignedTickets.filter(
        (t) => t.status === 'Open' || t.status === 'InProgress',
      )
      myTasks.value = assignedTickets
        .filter((t) => t.status === 'Open')
        .sort((a, b) => {
          const priorityDiff = PRIORITY_ORDER[b.priority] - PRIORITY_ORDER[a.priority]
          return priorityDiff !== 0 ? priorityDiff : a.createdAtUtc.localeCompare(b.createdAtUtc)
        })
      recentCustomers.value = dedupeRecentCustomers([...assignedTickets, ...resolvedTickets])

      summary.value = {
        openAssignedCount: myOpenTickets.value.length,
        needsActionCount: myTasks.value.length,
        resolvedLast7DaysCount: resolvedTotal,
        slaAtRiskCount: myOpenTickets.value.filter(isSlaAtRisk).length,
      }
    }
  }

  async function loadAll() {
    loading.value = true
    try {
      await load()
    } finally {
      loading.value = false
    }
  }

  async function refresh() {
    await load()
  }

  return { summary, myOpenTickets, myTasks, recentCustomers, loading, error, loadAll, refresh }
})
