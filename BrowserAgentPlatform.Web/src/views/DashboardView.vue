<template>
  <div>
    <div class="page-title">Dashboard</div>
    <div class="page-subtitle">统一查看 Agent、Run，以及 Profile 的 lifecycle / workspace 状态面板。</div>

    <div class="grid" style="grid-template-columns: repeat(6, 1fr);">
      <div class="card"><div class="muted">Agents</div><h1>{{ summary.agents || 0 }}</h1></div>
      <div class="card"><div class="muted">Online Agents</div><h1>{{ summary.onlineAgents || 0 }}</h1></div>
      <div class="card"><div class="muted">Profiles</div><h1>{{ summary.profiles || 0 }}</h1></div>
      <div class="card"><div class="muted">Ready Profiles</div><h1>{{ stateBoard.ready || 0 }}</h1></div>
      <div class="card"><div class="muted">Active Profiles</div><h1>{{ stateBoard.active || 0 }}</h1></div>
      <div class="card"><div class="muted">Broken Profiles</div><h1>{{ stateBoard.broken || 0 }}</h1></div>
    </div>

    <div class="grid" style="grid-template-columns: repeat(4, 1fr); margin-top: 12px;">
      <div class="card"><div class="muted">Queued</div><h2>{{ summary.queued || 0 }}</h2></div>
      <div class="card"><div class="muted">Running</div><h2>{{ summary.running || 0 }}</h2></div>
      <div class="card"><div class="muted">Completed</div><h2>{{ summary.completed || 0 }}</h2></div>
      <div class="card"><div class="muted">Failed</div><h2>{{ summary.failed || 0 }}</h2></div>
    </div>

    <div class="card" style="margin-top:16px;">
      <div class="toolbar">
        <div>
          <div style="font-weight:700;">Profile 状态总览</div>
          <div class="muted">Dashboard / Live / Profiles 统一使用同一套状态面板。</div>
        </div>
        <div class="section-actions">
          <button class="btn secondary" @click="load">刷新</button>
          <RouterLink class="btn" to="/profiles">打开 Profiles</RouterLink>
        </div>
      </div>
      <div class="section-actions" style="margin-top:10px; flex-wrap:wrap;">
        <span v-for="item in stateBoard.byLifecycle || []" :key="item.lifecycle" class="chip">{{ item.lifecycle }}: {{ item.count }}</span>
      </div>
    </div>

    <div class="grid" style="grid-template-columns: 1.1fr 1fr; margin-top: 16px; align-items:start;">
      <div class="card">
        <div class="toolbar">
          <div>
            <div style="font-weight:700;">最近运行</div>
            <div class="muted">点击后进入 Live 页面，右侧会显示同一个 Profile 状态面板。</div>
          </div>
          <button class="btn secondary" @click="load">刷新</button>
        </div>
        <div v-if="!summary.recentRuns?.length" class="muted" style="margin-top:10px;">暂无运行记录</div>
        <div v-for="run in summary.recentRuns || []" :key="run.id" class="card card-dark" style="margin-top:12px;">
          <div style="display:flex;justify-content:space-between;gap:12px;align-items:flex-start;">
            <div style="flex:1;">
              <div style="font-weight:600;">Run #{{ run.id }} / Task #{{ run.taskId }}</div>
              <div style="margin-top:6px;"><span class="badge" :class="run.status">{{ run.status }}</span></div>
              <div class="muted" style="margin-top:8px;">步骤：{{ run.currentStepLabel || '-' }}</div>
              <div class="muted">URL：{{ run.currentUrl || '-' }}</div>
              <div class="muted">Profile：{{ run.browserProfileId || '-' }}</div>
            </div>
            <RouterLink :to="`/live/${run.id}`" class="btn">查看 Live</RouterLink>
          </div>
          <img v-if="run.lastPreviewPath" :src="apiBase + run.lastPreviewPath" style="margin-top:12px;max-width:100%;border-radius:12px;border:1px solid #334155;" />
        </div>
      </div>

      <div>
        <div class="card" style="margin-bottom:12px;">
          <div style="font-weight:700;">重点 Profile 状态面板</div>
          <div class="muted" style="margin-top:6px;">与 Profiles 页展示完全一致。</div>
        </div>
        <ProfileStatePanel v-for="item in stateBoard.items || []" :key="item.id" :item="item" compact show-links style="margin-bottom:12px;" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive } from 'vue'
import { RouterLink } from 'vue-router'
import ProfileStatePanel from '../components/ProfileStatePanel.vue'
import { api, API_BASE_URL } from '../services/api'

const summary = reactive({})
const stateBoard = reactive({ items: [], byLifecycle: [] })
const apiBase = API_BASE_URL

async function load() {
  const [liveSummary, board] = await Promise.allSettled([
    api.summary(),
    api.profileStateBoard(6)
  ])
  Object.assign(summary, liveSummary.status === 'fulfilled' ? liveSummary.value : {})
  Object.assign(stateBoard, board.status === 'fulfilled' ? board.value : { items: [], byLifecycle: [] })
}

onMounted(load)
</script>

<style scoped>
.chip { display:inline-flex; align-items:center; padding:6px 10px; border-radius:999px; background:#0f172a; border:1px solid #1e293b; color:#e2e8f0; }
</style>
