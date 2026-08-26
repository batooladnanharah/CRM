import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAiStore } from '@/stores/ai'
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

  it('summarise() returns the AiResponse and forwards an abort signal', async () => {
    const response = makeResponse()
    summariseMock.mockResolvedValue(response)

    const store = useAiStore()
    const result = await store.summarise('ticket-1')

    expect(result).toEqual(response)
    expect(summariseMock).toHaveBeenCalledWith('ticket-1', expect.any(AbortSignal))
  })

  it('cancelSummary() aborts the in-flight request', async () => {
    let capturedSignal: AbortSignal | undefined
    summariseMock.mockImplementation(
      (_ticketId, signal) =>
        new Promise((_resolve, reject) => {
          capturedSignal = signal
          signal?.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
        }),
    )

    const store = useAiStore()
    const pending = store.summarise('ticket-1')
    store.cancelSummary()

    await expect(pending).rejects.toThrow('Aborted')
    expect(capturedSignal?.aborted).toBe(true)
  })
})
