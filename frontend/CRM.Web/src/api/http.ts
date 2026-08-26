const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '/api'

const AUTH_TOKEN_STORAGE_KEY = 'crm.auth.token'

export class NetworkError extends Error {
  constructor(message = 'Network request failed.') {
    super(message)
    this.name = 'NetworkError'
  }
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const AUTH_USER_STORAGE_KEY = 'crm.auth.user'

function getStoredToken(): string | null {
  try {
    return sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)
  } catch {
    return null
  }
}

// A 401 means the session token is missing/expired/revoked — no retry can
// fix that, so drop the local session and send the user back to login.
// Skipped for /auth/login itself, where a 401 just means bad credentials.
function handleUnauthorized(path: string) {
  if (path.startsWith('/auth/login')) {
    return
  }

  try {
    sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY)
    sessionStorage.removeItem(AUTH_USER_STORAGE_KEY)
  } catch {
    // sessionStorage may be unavailable — nothing to clear.
  }

  void import('@/router').then(({ default: router }) => {
    const current = router.currentRoute.value
    if (current.name === 'login') {
      return
    }
    void router.push({ path: '/login', query: { redirect: current.fullPath, sessionExpired: '1' } })
  })
}

// Exposed for callers that need a raw fetch (e.g. multipart upload, blob
// download) instead of apiRequest's JSON-only Content-Type/parsing behavior.
export function resolveApiUrl(path: string): string {
  return `${API_BASE_URL}${path}`
}

export function authHeaders(): HeadersInit {
  const token = getStoredToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string }
    return body?.message ?? 'Something went wrong. Please try again.'
  } catch {
    return 'Something went wrong. Please try again.'
  }
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Content-Type', 'application/json')

  const token = getStoredToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })
  } catch {
    throw new NetworkError()
  }

  if (!response.ok) {
    let message = 'Something went wrong. Please try again.'
    try {
      const body = (await response.json()) as { message?: string }
      if (body?.message) {
        message = body.message
      }
    } catch {
      // response body was not JSON — fall back to the generic message.
    }

    if (response.status === 401) {
      handleUnauthorized(path)
    }

    throw new ApiError(response.status, message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
