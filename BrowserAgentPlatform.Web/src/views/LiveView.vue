<template>
  <div>
    <div class="page-title">Live 调试</div>
    <div class="page-subtitle">查看运行状态、日志、预览图，并通过 SignalR 自动刷新。</div>

    <div v-if="!route.params.runId" class="card">
      <div class="muted">请从任务中心进入某个具体 Run 的 Live 页面。</div>
    </div>

    <div v-else class="grid" style="grid-template-columns:420px 1fr;">
      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">运行日志</div>
            <div class="muted">Run #{{ route.params.runId }}</div>
          </div>
          <div class="section-actions">
            <button class="btn secondary" @click="reload">刷新</button>
            <button class="btn" @click="testOpenCurrentProfile" :disabled="!detail.run?.browserProfileId">测试打开</button>
            <button class="btn success" @click="startTakeover" :disabled="!detail.run?.browserProfileId">开始接管</button>
            <button class="btn warn" @click="stopTakeover" :disabled="!detail.run?.browserProfileId">结束接管</button>
          </div>
        </div>

        <div v-if="detail.run" class="card card-dark" style="margin-bottom:12px;">
          <div style="display:flex;justify-content:space-between;align-items:center;gap:10px;">
            <div style="font-weight:700;">运行状态</div>
            <span class="badge" :class="detail.run.status">{{ detail.run.status }}</span>
          </div>
          <div class="muted" style="margin-top:8px;">步骤：{{ detail.run.currentStepLabel || '-' }}</div>
          <div class="muted">URL：{{ detail.run.currentUrl || '-' }}</div>
          <div class="muted">profileId：{{ detail.run.browserProfileId || '-' }}</div>
          <div class="muted">startedAt：{{ formatTime(detail.run.startedAt) }}</div>
          <div class="muted">finishedAt：{{ formatTime(detail.run.finishedAt) }}</div>
        </div>

        <div v-if="message" class="muted" style="margin-bottom:12px;">{{ message }}</div>
        <div v-if="!detail.logs?.length" class="muted">暂无日志</div>

        <div
          v-for="log in detail.logs || []"
          :key="log.id"
          style="padding:10px 0;border-bottom:1px solid #1f2937;"
        >
          <div style="display:flex;justify-content:space-between;gap:12px;">
            <span class="badge" :class="log.level">{{ log.level }}</span>
            <span class="muted">{{ formatTime(log.createdAt) }}</span>
          </div>
          <div style="margin-top:8px;">{{ log.message }}</div>
        </div>
      </div>

      <div class="card">
        <div style="font-weight:700;margin-bottom:12px;">实时预览</div>

        <div v-if="detail.run">
          <div class="muted">状态：{{ detail.run.status }}</div>
          <div class="muted">步骤：{{ detail.run.currentStepLabel || '-' }}</div>
          <div class="muted">URL：{{ detail.run.currentUrl || '-' }}</div>
        </div>

        <img
          v-if="detail.run?.lastPreviewPath"
          :src="apiBase + detail.run.lastPreviewPath + cacheBust"
          style="max-width:100%;border-radius:12px;border:1px solid #334155;margin-top:12px;"
        />

        <div v-else class="muted" style="margin-top:12px;">暂无预览图</div>

        <div class="card card-dark" style="margin-top:16px;">
          <div style="font-weight:700;margin-bottom:10px;">产物列表</div>
          <div v-if="!detail.artifacts?.length" class="muted">暂无产物</div>
          <div v-for="item in detail.artifacts || []" :key="item.id" style="padding:8px 0;border-bottom:1px solid #1f2937;">
            <div>{{ item.fileName || item.artifactType }}</div>
            <div class="muted">{{ item.filePath }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, onMounted, ref, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { api, API_BASE_URL, SIGNALR_BASE_URL } from '../services/api'
import { auth } from '../services/auth'

const route = useRoute()
const apiBase = API_BASE_URL
const cacheBust = ref('')
const detail = reactive({ run: null, logs: [], artifacts: [] })
const message = ref('')

let connection = null

function formatTime(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString()
}

async function reload() {
  if (!route.params.runId) return
  const data = await api.runDetail(route.params.runId)
  detail.run = data.run
  detail.logs = data.logs || []
  detail.artifacts = data.artifacts || []
  cacheBust.value = `?t=${Date.now()}`
}

async function testOpenCurrentProfile() {
  if (!detail.run?.browserProfileId) return
  await api.testOpenProfile(detail.run.browserProfileId)
  message.value = `已发送测试打开命令：Profile #${detail.run.browserProfileId}`
}

async function startTakeover() {
  if (!detail.run?.browserProfileId) return
  await api.takeover(detail.run.browserProfileId, true)
  message.value = `已发送开始接管命令：Profile #${detail.run.browserProfileId}`
}

async function stopTakeover() {
  if (!detail.run?.browserProfileId) return
  await api.takeover(detail.run.browserProfileId, false)
  message.value = `已发送结束接管命令：Profile #${detail.run.browserProfileId}`
}

onMounted(async () => {
  if (!route.params.runId) return

  await reload()

  connection = new HubConnectionBuilder()
    .withUrl(`${SIGNALR_BASE_URL}/hubs/live?access_token=${auth.token()}`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on('runUpdate', async (payload) => {
    if (!payload || payload.id !== Number(route.params.runId)) return
    await reload()
  })

  await connection.start()
  await connection.invoke('JoinRun', Number(route.params.runId))
})

onBeforeUnmount(async () => {
  if (connection) {
    await connection.stop()
    connection = null
  }
})
</script>
