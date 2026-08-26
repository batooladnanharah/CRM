import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAiStore } from '@/stores/ai'
import { ApiError } from '@/api/http'
import type { getAiStatus, summariseTicket } from '@/api/ai'
import type { AiResponse, AiStatus } from '@/types/ai'

const { getStatusMock, summariseMock } = vi.hoisted(() => ({
  getStatusMock: vi.fn<typeof getAiStatus>(),
  summariseMock: vi.fn<typeof summariseTicket>(),
}))

vi.mock('@/api/ai', () => ({
  getAiStatus: getStatusMock,
  summariseTicket: summariseMock,
}))

function makeStatus(overrides: Partial<AiStatus> = {}): AiStatus {
  return { enabled: true, provider: 'Development', available: true, ...overrides }
}

function makeResponse(overrides: Partial<AiResponse> = {}): AiResponse {
  return {
    success: true,
    content: 'Development summary: test',
    provider: 'Development',
    model: 'development-mock',
    errorCode: null,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  getStatusMock.mockReset()
  summariseMock.mockReset()
})

describe('ai store', () => {
  it('has the expected initial state', () => {
    const store = useAiStore()

    expect(store.status).toBeNull()
    expect(store.loadingStatus).toBe(false)
    expect(store.statusError).toBeNull()
    expect(store.summaries).toEqual({})
  })

  it('loadStatus() populates status on success', async () => {
    const status = makeStatus()
    getStatusMock.mockResolvedValue(status)

    const store = useAiStore()
    await store.loadStatus()

    expect(store.status).toEqual(status)
    expect(store.loadingStatus).toBe(false)
    expect(store.statusError).toBeNull()
  })

  it('loadStatus() sets a safe fallback status on network error', async () => {
    getStatusMock.mockRejectedValue(new Error('network down'))

    const store = useAiStore()
    await store.loadStatus()

    expect(store.status).toEqual({ enabled: false, provider: null, available: false })
    expect(store.statusError).toBe('errorLoad')
    expect(store.loadingStatus).toBe(false)
  })

  it('generateSummary() stores the summary content and clears loading', async () => {
    summariseMock.mockResolvedValue(makeResponse({ content: 'Development summary: ticket-1 content' }))

    const store = useAiStore()
    await store.generateSummary('ticket-1')

    expect(store.summaries['ticket-1']).toBe('Development summary: ticket-1 content')
    expect(store.summaryLoading['ticket-1']).toBe(false)
    expect(store.summaryError['ticket-1']).toBeNull()
  })

  it('generateSummary() is a no-op while already loading for that ticket', async () => {
    let resolveFirst!: (value: AiResponse) => void
    summariseMock.mockReturnValue(
      new Promise((resolve) => {
        resolveFirst = resolve
      }),
    )

    const store = useAiStore()
    const first = store.generateSummary('ticket-1')
    const second = store.generateSummary('ticket-1')

    resolveFirst!(makeResponse())
    await Promise.all([first, second])

    expect(summariseMock).toHaveBeenCalledTimes(1)
  })

  it('generateSummary() does not clobber the loading state of a different ticket', async () => {
    let resolveFirst!: (value: AiResponse) => void
    summariseMock.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveFirst = resolve
        }),
    )

    const store = useAiStore()
    const pending = store.generateSummary('ticket-1')
    expect(store.summaryLoading['ticket-1']).toBe(true)

    resolveFirst!(makeResponse())
    await pending
  })

  it('generateSummary() sets a mapped error code on 503 and rethrows', async () => {
    summariseMock.mockRejectedValue(new ApiError(503, 'AI is unavailable'))

    const store = useAiStore()
    await expect(store.generateSummary('ticket-1')).rejects.toThrow('AI is unavailable')

    expect(store.summaryError['ticket-1']).toBe('unavailable')
    expect(store.summaryLoading['ticket-1']).toBe(false)
    expect(store.summaries['ticket-1']).toBeUndefined()
  })

  it('generateSummary() sets a mapped error code on 502', async () => {
    summariseMock.mockRejectedValue(new ApiError(502, 'Provider failed'))

    const store = useAiStore()
    await expect(store.generateSummary('ticket-1')).rejects.toThrow('Provider failed')

    expect(store.summaryError['ticket-1']).toBe('providerFailed')
  })

  it('regenerateSummary() clears the previous summary before fetching a new one', async () => {
    summariseMock.mockResolvedValueOnce(makeResponse({ content: 'first' }))
    const store = useAiStore()
    await store.generateSummary('ticket-1')
    expect(store.summaries['ticket-1']).toBe('first')

    summariseMock.mockResolvedValueOnce(makeResponse({ content: 'second' }))
    await store.regenerateSummary('ticket-1')

    expect(store.summaries['ticket-1']).toBe('second')
    expect(summariseMock).toHaveBeenCalledTimes(2)
  })
})
