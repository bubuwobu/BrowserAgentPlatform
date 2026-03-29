const key = 'browser-agent-token'
export const auth = {
  token: () => localStorage.getItem(key),
  set: (token) => localStorage.setItem(key, token),
  clear: () => localStorage.removeItem(key)
}
