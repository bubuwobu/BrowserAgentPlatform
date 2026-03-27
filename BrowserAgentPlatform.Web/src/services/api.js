import { auth } from './auth'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:12126'
export const SIGNALR_BASE_URL = import.meta.env.VITE_SIGNALR_BASE_URL || API_BASE_URL

async function request(path, options = {}) {
  const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) }
  const token = auth.token()
  if (token) headers.Authorization = `Bearer ${token}`

  const res = await fetch(API_BASE_URL + path, { ...options, headers })

  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Request failed: ${res.status}`)
  }

  return res.status === 204 ? null : res.json()
}

export const api = {
  login: (body) => request('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),

  summary: () => request('/api/live/summary'),

  agents: () => request('/api/agents'),

  proxies: () => request('/api/config/proxies'),
  createProxy: (body) => request('/api/config/proxies', { method: 'POST', body: JSON.stringify(body) }),
  resetAndReseedDemoData: () => request('/api/config/demo/reset-reseed', { method: 'POST' }),

  fingerprints: () => request('/api/config/fingerprints'),
  createFingerprint: (body) => request('/api/config/fingerprints', { method: 'POST', body: JSON.stringify(body) }),

  profiles: () => request('/api/profiles'),
  createProfile: (body) => request('/api/profiles', { method: 'POST', body: JSON.stringify(body) }),
  profileIsolationCheck: (id) => request(`/api/profiles/${id}/isolation-check`, { method: 'POST' }),
  testOpenProfile: (id) => request(`/api/profiles/${id}/test-open`, { method: 'POST' }),
  takeover: (id, headed) =>
    request(`/api/profiles/${id}/takeover`, {
      method: 'POST',
      body: JSON.stringify({ profileId: id, headed })
    }),
  unlockProfile: (id) => request(`/api/profiles/${id}/unlock`, { method: 'POST' }),

  templates: () => request('/api/templates'),
  createTemplate: (body) => request('/api/templates', { method: 'POST', body: JSON.stringify(body) }),

  tasks: () => request('/api/tasks'),
  runs: () => request('/api/tasks/runs'),
  createTask: (body) => request('/api/tasks', { method: 'POST', body: JSON.stringify(body) }),
  runDetail: (id) => request(`/api/tasks/runs/${id}`),
  runIsolationReport: (runId) => request(`/api/tasks/runs/${runId}/isolation-report`),
  replayRun: (runId) => request(`/api/tasks/runs/${runId}/replay`, { method: 'POST' }),

  observabilityOverview: () => request('/api/observability/overview'),
  auditEvents: (take = 200) => request(`/api/observability/audit-events?take=${take}`),

  closedLoopStart: (body) => request('/api/validation/closed-loop/start', { method: 'POST', body: JSON.stringify(body) }),
  closedLoopExecute: (body) => request('/api/validation/closed-loop/execute', { method: 'POST', body: JSON.stringify(body) })
}
