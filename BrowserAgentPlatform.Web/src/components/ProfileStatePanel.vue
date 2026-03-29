<template>
  <div class="profile-state-panel card card-dark" :style="compact ? 'padding:12px;' : ''">
    <div class="toolbar" style="align-items:flex-start; gap:12px;">
      <div style="flex:1; min-width:0;">
        <div style="display:flex; align-items:center; gap:8px; flex-wrap:wrap;">
          <div style="font-weight:700;">{{ item.name }} <span class="muted">#{{ item.id }}</span></div>
          <span class="badge" :class="item.lifecycleState || item.status || 'idle'">{{ item.lifecycleState || '-' }}</span>
          <span class="badge" :class="item.status || 'idle'">{{ item.status || '-' }}</span>
        </div>
        <div class="muted" style="margin-top:6px;">proxy={{ item.proxyId || '-' }} / fingerprint={{ item.fingerprintTemplateId || '-' }} / isolation={{ item.isolationLevel || '-' }}</div>
      </div>
      <div class="section-actions" v-if="showLinks">
        <RouterLink class="btn secondary" :to="`/profiles`">Profiles</RouterLink>
        <RouterLink v-if="runId" class="btn" :to="`/live/${runId}`">Run Live</RouterLink>
      </div>
    </div>

    <div class="grid" :style="gridStyle">
      <div class="mini-panel">
        <div class="mini-title">Lifecycle</div>
        <div class="state-row"><span class="chip">{{ item.lifecycleState || '-' }}</span><span class="muted">status={{ item.status || '-' }}</span></div>
        <div class="muted">lastStarted: {{ formatDate(item.lastStartedAt) }}</div>
        <div class="muted">lastStopped: {{ formatDate(item.lastStoppedAt) }}</div>
        <div class="muted">lastUsed: {{ formatDate(item.lastUsedAt) }}</div>
        <div class="muted">lastIsolationCheck: {{ formatDate(item.lastIsolationCheckAt) }}</div>
      </div>

      <div class="mini-panel">
        <div class="mini-title">Workspace</div>
        <div class="muted">workspaceKey: {{ item.workspaceKey || '-' }}</div>
        <div class="muted mono">profileRoot: {{ displayPath(item.profileRootPath || item.localProfilePath) }}</div>
        <div class="muted mono">storageRoot: {{ displayPath(item.storageRootPath) }}</div>
        <div class="muted mono">downloadRoot: {{ displayPath(item.downloadRootPath) }}</div>
        <div class="muted mono">artifactRoot: {{ displayPath(item.artifactRootPath) }}</div>
        <div class="muted mono">tempRoot: {{ displayPath(item.tempRootPath) }}</div>
      </div>

      <div class="mini-panel">
        <div class="mini-title">Runtime</div>
        <div class="muted">currentStep: {{ runtimeMeta?.lifecycle?.currentStepLabel || runtimeMeta?.lifecycle?.currentStepId || '-' }}</div>
        <div class="muted">currentUrl: {{ runtimeMeta?.lifecycle?.currentUrl || '-' }}</div>
        <div class="muted">updatedAt: {{ formatDate(runtimeMeta?.lifecycle?.updatedAt) }}</div>
        <details style="margin-top:8px;">
          <summary class="muted" style="cursor:pointer;">查看 runtime_meta_json</summary>
          <pre class="json-preview">{{ pretty(runtimeMeta) }}</pre>
        </details>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'

const props = defineProps({
  item: { type: Object, required: true },
  compact: { type: Boolean, default: false },
  showLinks: { type: Boolean, default: false },
  runId: { type: [Number, String], default: null }
})

const runtimeMeta = computed(() => {
  const raw = props.item?.runtimeMetaJson ?? props.item?.runtimeMeta
  if (!raw) return null
  if (typeof raw === 'object') return raw
  try { return JSON.parse(raw) } catch { return null }
})

const gridStyle = computed(() => props.compact
  ? 'grid-template-columns:repeat(3, minmax(0, 1fr)); gap:12px; margin-top:12px;'
  : 'grid-template-columns:repeat(3, minmax(0, 1fr)); gap:12px; margin-top:12px;')

function formatDate(value) {
  if (!value) return '-'
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString()
}
function displayPath(v) { return v || '-' }
function pretty(value) { return JSON.stringify(value || {}, null, 2) }
</script>

<style scoped>
.mini-panel { background:#0f172a; border:1px solid #1e293b; border-radius:12px; padding:12px; min-width:0; }
.mini-title { font-weight:700; margin-bottom:8px; }
.state-row { display:flex; justify-content:space-between; align-items:center; gap:8px; margin-bottom:8px; }
.chip { display:inline-flex; align-items:center; padding:4px 10px; border-radius:999px; background:#1e293b; color:#e2e8f0; font-size:12px; }
.mono { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace; word-break:break-all; }
.json-preview { white-space:pre-wrap; margin-top:8px; max-height:240px; overflow:auto; }
</style>
