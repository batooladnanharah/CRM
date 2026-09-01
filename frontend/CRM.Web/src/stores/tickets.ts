import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  assignTicket,
  changeTicketPriority,
  changeTicketStatus,
  createTicket,
  escalateTicket,
  fetchEligibleAgents,
  fetchTicketHistory,
  getTicket,
  listTickets,
} from '@/api/tickets'
import { ApiError } from '@/api/http'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type {
  CreateTicketPayload,
  EligibleAgent,
  Ticket,
  TicketHistoryEntry,
  TicketListItem,
  TicketPriority,
  TicketStatus,
} from '@/types/tickets'

const SEARCH_DEBOUNCE_MS = 300
const t = i18n.global.t

export const useTicketsStore = defineStore('tickets', () => {
  const items = ref<TicketListItem[]>([])
  const current = ref<Ticket | null>(null)
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const search = ref('')
  const status = ref<TicketStatus | ''>('')
  const priority = ref<TicketPriority | ''>('')
  const loading = ref(false)
  const error = ref<string | null>(null)
  const creating = ref(false)
  const createError = ref<string | null>(null)
  const loadingCurrent = ref(false)
  const notFound = ref(false)
  const loadError = ref<string | null>(null)

  const isAssigning = ref(false)
  const isChangingStatus = ref(false)
  const isChangingPriority = ref(false)
  const isLoadingHistory = ref(false)
  const isLoadingAgents = ref(false)
  const history = ref<TicketHistoryEntry[]>([])
  const eligibleAgents = ref<EligibleAgent[]>([])
  const actionError = ref<string | null>(null)
  const escalating = ref(false)
  const escalateError = ref<string | null>(null)

  let searchDebounceHandle: ReturnType<typeof setTimeout> | null = null
  let requestSequence = 0

  async function fetchList(overrides?: { page?: number }) {
    if (overrides?.page !== undefined) page.value = overrides.page

    loading.value = true
    error.value = null

    const sequence = ++requestSequence

    try {
      const result = await listTickets({
        search: search.value || undefined,
        status: status.value || undefined,
        priority: priority.value || undefined,
        page: page.value,
        pageSize: pageSize.value,
      })

      if (sequence !== requestSequence) {
        return
      }

      items.value = result.items
      page.value = result.page
      pageSize.value = result.pageSize
      total.value = result.totalCount
    } catch {
      if (sequence !== requestSequence) {
        return
      }
      error.value = 'errorLoad'
    } finally {
      if (sequence === requestSequence) {
        loading.value = false
      }
    }
  }

  function retry() {
    return fetchList()
  }

  function setSearch(term: string) {
    search.value = term

    if (searchDebounceHandle) {
      clearTimeout(searchDebounceHandle)
    }

    searchDebounceHandle = setTimeout(() => {
      searchDebounceHandle = null
      void fetchList({ page: 1 })
    }, SEARCH_DEBOUNCE_MS)
  }

  function setFilters(partial: { status?: TicketStatus | ''; priority?: TicketPriority | '' }) {
    if (partial.status !== undefined) status.value = partial.status
    if (partial.priority !== undefined) priority.value = partial.priority
    void fetchList({ page: 1 })
  }

  function setPage(nextPage: number) {
    void fetchList({ page: nextPage })
  }

  async function fetchOne(id: string) {
    loadingCurrent.value = true
    notFound.value = false
    loadError.value = null
    current.value = null

    try {
      current.value = await getTicket(id)
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        notFound.value = true
      } else {
        loadError.value = 'errorLoad'
      }
    } finally {
      loadingCurrent.value = false
    }
  }

  async function create(payload: CreateTicketPayload): Promise<Ticket> {
    creating.value = true
    createError.value = null

    try {
      const created = await createTicket(payload)
      // CRM-62 — surface the automatically-assigned agent in the success
      // toast; there's no separate "assign" UI step for auto-assignment,
      // so this is the only place the user learns who picked it up.
      if (created.autoAssigned && created.assigneeDisplayName) {
        useToast().success(t('notifications.tickets.createdAutoAssigned', { agent: created.assigneeDisplayName }))
      } else {
        useToast().success(t('notifications.tickets.created'))
      }
      return created
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.message === 'customer_not_found') {
        createError.value = 'customerNotFound'
      } else {
        createError.value = 'generic'
      }
      useToast().error(t('notifications.tickets.loadFailed'))
      throw err
    } finally {
      creating.value = false
    }
  }

  function resetError() {
    error.value = null
    createError.value = null
    loadError.value = null
    actionError.value = null
    escalateError.value = null
  }

  async function assign(id: string, agentUserId: string | null) {
    isAssigning.value = true
    actionError.value = null

    try {
      const updated = await assignTicket(id, agentUserId)
      current.value = updated
      useToast().success(t('notifications.tickets.assigned'))
      return updated
    } catch (err) {
      actionError.value = mapActionError(err)
      useToast().error(t('notifications.tickets.loadFailed'))
      throw err
    } finally {
      isAssigning.value = false
    }
  }

  async function changeStatus(id: string, nextStatus: TicketStatus) {
    isChangingStatus.value = true
    actionError.value = null

    try {
      const updated = await changeTicketStatus(id, nextStatus)
      current.value = updated
      useToast().success(t('notifications.tickets.updated'))
      return updated
    } catch (err) {
      actionError.value = mapActionError(err)
      useToast().error(t('notifications.tickets.loadFailed'))
      throw err
    } finally {
      isChangingStatus.value = false
    }
  }

  async function changePriority(id: string, nextPriority: TicketPriority) {
    isChangingPriority.value = true
    actionError.value = null

    try {
      const updated = await changeTicketPriority(id, nextPriority)
      current.value = updated
      return updated
    } catch (err) {
      actionError.value = mapActionError(err)
      throw err
    } finally {
      isChangingPriority.value = false
    }
  }

  async function loadHistory(id: string) {
    isLoadingHistory.value = true
    actionError.value = null

    try {
      history.value = await fetchTicketHistory(id)
    } catch {
      actionError.value = 'errorLoadHistory'
    } finally {
      isLoadingHistory.value = false
    }
  }

  async function loadEligibleAgents() {
    isLoadingAgents.value = true
    actionError.value = null

    try {
      eligibleAgents.value = await fetchEligibleAgents()
    } catch {
      actionError.value = 'errorLoadAgents'
    } finally {
      isLoadingAgents.value = false
    }
  }

  async function escalate(id: string, reason: string) {
    escalating.value = true
    escalateError.value = null

    try {
      const updated = await escalateTicket(id, { reason })
      current.value = updated
      // Refetch history so the new Escalated entry (and its reason) shows up
      // without requiring the user to re-open the history panel.
      await loadHistory(id)
      return updated
    } catch (err) {
      escalateError.value = mapActionError(err)
      throw err
    } finally {
      escalating.value = false
    }
  }

  function mapActionError(err: unknown): string {
    if (err instanceof ApiError && err.status === 400) {
      return err.message
    }
    return 'errorAction'
  }

  return {
    items,
    current,
    total,
    page,
    pageSize,
    search,
    status,
    priority,
    loading,
    error,
    creating,
    createError,
    loadingCurrent,
    notFound,
    loadError,
    isAssigning,
    isChangingStatus,
    isChangingPriority,
    isLoadingHistory,
    isLoadingAgents,
    history,
    eligibleAgents,
    actionError,
    escalating,
    escalateError,
    fetchList,
    retry,
    fetchOne,
    create,
    setSearch,
    setFilters,
    setPage,
    resetError,
    assign,
    changeStatus,
    changePriority,
    loadHistory,
    loadEligibleAgents,
    escalate,
  }
})
