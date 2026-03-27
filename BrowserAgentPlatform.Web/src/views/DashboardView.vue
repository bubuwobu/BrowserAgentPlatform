<template>
  <div>
    <div class="page-title">Dashboard</div>
    <div class="page-subtitle">查看 Agent、Profile、任务运行和最近预览。</div>

    <div class="grid" style="grid-template-columns: repeat(6, 1fr);">
      <div class="card"><div class="muted">Agents</div><h1>{{ summary.agents || 0 }}</h1></div>
      <div class="card"><div class="muted">Online Agents</div><h1>{{ summary.onlineAgents || 0 }}</h1></div>
      <div class="card"><div class="muted">Profiles</div><h1>{{ summary.profiles || 0 }}</h1></div>
      <div class="card"><div class="muted">Idle Profiles</div><h1>{{ summary.idleProfiles || 0 }}</h1></div>
      <div class="card"><div class="muted">Queued</div><h1>{{ summary.queued || 0 }}</h1></div>
      <div class="card"><div class="muted">Running</div><h1>{{ summary.running || 0 }}</h1></div>
    </div>

    <div class="grid" style="grid-template-columns: repeat(4, 1fr); margin-top: 12px;">
      <div class="card"><div class="muted">Avg Typing Delay (24h)</div><h2>{{ formatMetric(summary.behaviorQuality?.avgTypingDelayMs24h, 1) }} ms</h2></div>
      <div class="card"><div class="muted">Comment Duplicate Rate</div><h2>{{ formatPercent(summary.behaviorQuality?.avgCommentDuplicateRate24h) }}</h2></div>
      <div class="card"><div class="muted">Behavior Anomaly Rate</div><h2>{{ formatPercent(summary.behaviorQuality?.avgAnomalyRate24h) }}</h2></div>
      <div class="card"><div class="muted">Sampled Behavior Runs</div><h2>{{ summary.behaviorQuality?.sampledRuns24h || 0 }}</h2></div>
    </div>

    <div class="grid" style="grid-template-columns: 1.1fr 1fr; margin-top: 16px;">
      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">最近运行</div>
            <div class="muted">这里应该能直接看到系统是否真的在跑。</div>
          </div>
          <div class="section-actions">
            <button class="btn warn" @click="resetDemoData" :disabled="resetting">{{ resetting ? '重灌中...' : '重灌 Demo 数据' }}</button>
            <button class="btn secondary" @click="load">刷新</button>
          </div>
        </div>

        <div v-if="!summary.recentRuns?.length" class="muted">暂无运行记录</div>

        <div
          v-for="run in summary.recentRuns || []"
          :key="run.id"
          class="card card-dark"
          style="margin-bottom:12px;"
        >
          <div style="display:flex;justify-content:space-between;gap:12px;align-items:flex-start;">
            <div style="flex:1;">
              <div style="font-weight:600;">Run #{{ run.id }} / Task #{{ run.taskId }}</div>
              <div style="margin-top:6px;">
                <span class="badge" :class="run.status">{{ run.status }}</span>
              </div>
              <div class="muted" style="margin-top:8px;">步骤：{{ run.currentStepLabel || '-' }}</div>
              <div class="muted">URL：{{ run.currentUrl || '-' }}</div>
            </div>
            <RouterLink :to="`/live/${run.id}`" class="btn">查看 Live</RouterLink>
          </div>

          <img
            v-if="run.lastPreviewPath"
            :src="apiBase + run.lastPreviewPath"
            style="margin-top:12px;max-width:100%;border-radius:12px;border:1px solid #334155;"
          />
        </div>
      </div>

      <div class="grid" style="grid-template-rows: 1fr 1fr;">
        <div class="card">
          <div style="font-weight:700;margin-bottom:10px;">最近 Agent</div>
          <div v-if="!summary.recentAgents?.length" class="muted">暂无 Agent 数据</div>
          <div
            v-for="item in summary.recentAgents || []"
            :key="item.id"
            style="padding:10px 0;border-bottom:1px solid #1e293b;"
          >
            <div style="display:flex;justify-content:space-between;align-items:center;">
              <div>
                <div>{{ item.name || 'Unnamed Agent' }} #{{ item.id }}</div>
                <div class="muted">{{ item.machineName }}</div>
              </div>
              <span class="badge" :class="item.status">{{ item.status }}</span>
            </div>
          </div>
        </div>

        <div class="card">
          <div style="font-weight:700;margin-bottom:10px;">最近 Profile</div>
          <div v-if="!summary.recentProfiles?.length" class="muted">暂无 Profile 数据</div>
          <div
            v-for="item in summary.recentProfiles || []"
            :key="item.id"
            style="padding:10px 0;border-bottom:1px solid #1e293b;"
          >
            <div style="display:flex;justify-content:space-between;align-items:center;">
              <div>
                <div>{{ item.name }} #{{ item.id }}</div>
                <div class="muted">
                  owner={{ item.ownerAgentId || '-' }} proxy={{ item.proxyId || '-' }}
                </div>
              </div>
              <span class="badge" :class="item.status">{{ item.status }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, API_BASE_URL } from '../services/api'

const summary = reactive({})
const apiBase = API_BASE_URL
const resetting = ref(false)

async function load() {
  const [liveSummary, observability] = await Promise.allSettled([
    api.summary(),
    api.observabilityOverview()
  ])
  Object.assign(summary, liveSummary.status === 'fulfilled' ? liveSummary.value : {})
  if (observability.status === 'fulfilled') {
    summary.behaviorQuality = observability.value?.behaviorQuality || null
  }
}

async function resetDemoData() {
  const confirmed = window.confirm('该操作会清空并重灌 DEMO 数据，是否继续？')
  if (!confirmed) return
  resetting.value = true
  try {
    await api.resetAndReseedDemoData()
    await load()
    window.alert('Demo 数据已重灌完成。')
  } catch (err) {
    window.alert(err.message || '重灌失败')
  } finally {
    resetting.value = false
  }
}

function formatMetric(value, digits = 2) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '0'
  return Number(value).toFixed(digits)
}

function formatPercent(value) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '0%'
  return `${(Number(value) * 100).toFixed(1)}%`
}

async function resetDemoData() {
  const confirmed = window.confirm('该操作会清空并重灌 DEMO 数据，是否继续？')
  if (!confirmed) return
  resetting.value = true
  try {
    await api.resetAndReseedDemoData()
    await load()
    window.alert('Demo 数据已重灌完成。')
  } catch (err) {
    window.alert(err.message || '重灌失败')
  } finally {
    resetting.value = false
  }
}

onMounted(load)
</script>
