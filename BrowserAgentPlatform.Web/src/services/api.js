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

  // Config: proxies / fingerprints
  proxies: () => request('/api/config/proxies'),
  createProxy: (body) => request('/api/config/proxies', { method: 'POST', body: JSON.stringify(body) }),
  updateProxy: (id, body) => request(`/api/config/proxies/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteProxy: (id) => request(`/api/config/proxies/${id}`, { method: 'DELETE' }),
  fingerprints: () => request('/api/config/fingerprints'),
  createFingerprint: (body) => request('/api/config/fingerprints', { method: 'POST', body: JSON.stringify(body) }),
  updateFingerprint: (id, body) => request(`/api/config/fingerprints/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteFingerprint: (id) => request(`/api/config/fingerprints/${id}`, { method: 'DELETE' }),

  // Profiles
  profiles: () => request('/api/profiles'),
  createProfile: (body) => request('/api/profiles', { method: 'POST', body: JSON.stringify(body) }),
  updateProfile: (id, body) => request(`/api/profiles/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteProfile: (id) => request(`/api/profiles/${id}`, { method: 'DELETE' }),
  profileStateBoard: (take = 12) => request(`/api/profiles/state-board?take=${take}`),
  profileStatePanel: (id) => request(`/api/profiles/${id}/state-panel`),
  profileIsolationCheck: (id) => request(`/api/profiles/${id}/isolation-check`, { method: 'POST' }),
  testOpenProfile: (id) => request(`/api/profiles/${id}/test-open`, { method: 'POST' }),
  takeover: (id, headed) => request(`/api/profiles/${id}/takeover`, { method: 'POST', body: JSON.stringify({ profileId: id, headed }) }),
  unlockProfile: (id) => request(`/api/profiles/${id}/unlock`, { method: 'POST' }),

  // Accounts
  accounts: () => request('/api/accounts'),
  createAccount: (body) => request('/api/accounts', { method: 'POST', body: JSON.stringify(body) }),
  updateAccount: (id, body) => request(`/api/accounts/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteAccount: (id) => request(`/api/accounts/${id}`, { method: 'DELETE' }),

  // Templates
  templates: () => request('/api/templates'),
  createTemplate: (body) => request('/api/templates', { method: 'POST', body: JSON.stringify(body) }),
  updateTemplate: (id, body) => request(`/api/templates/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteTemplate: (id) => request(`/api/templates/${id}`, { method: 'DELETE' }),

  // Tasks & runs
  tasks: () => request('/api/tasks'),
  runs: () => request('/api/tasks/runs'),
  createTask: (body) => request('/api/tasks', { method: 'POST', body: JSON.stringify(body) }),
  updateTask: (id, body) => request(`/api/tasks/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteTask: (id) => request(`/api/tasks/${id}`, { method: 'DELETE' }),
  runNowTask: (id) => request(`/api/tasks/${id}/run-now`, { method: 'POST' }),
  toggleTaskEnabled: (id) => request(`/api/tasks/${id}/toggle-enabled`, { method: 'POST' }),
  runDetail: (id) => request(`/api/tasks/runs/${id}`),
  runIsolationReport: (runId) => request(`/api/tasks/runs/${runId}/isolation-report`),
  replayRun: (runId) => request(`/api/tasks/runs/${runId}/replay`, { method: 'POST' }),
  observabilityOverview: () => request('/api/observability/overview'),
  auditEvents: (take = 200) => request(`/api/observability/audit-events?take=${take}`),
  closedLoopStart: (body) => request('/api/validation/closed-loop/start', { method: 'POST', body: JSON.stringify(body) }),
  closedLoopExecute: (body) => request('/api/validation/closed-loop/execute', { method: 'POST', body: JSON.stringify(body) }),
  startPicker: (body) => request('/api/picker/start', { method: 'POST', body: JSON.stringify(body) }),
  stopPicker: (body) => request('/api/picker/stop', { method: 'POST', body: JSON.stringify(body) }),
  getPickerSession: (sessionId) => request(`/api/picker/session/${sessionId}`)
}
