<template>
  <div>
    <div class="page-title">Live 调试</div>
    <div class="page-subtitle">Phase 5.1：补 isolation report 展示和更清楚的摘要。</div>

    <div v-if="!route.params.runId" class="card">
      <div class="muted">请从任务中心进入某个具体 Run 的 Live 页面。</div>
    </div>

    <template v-else>
      <div class="card" style="margin-bottom:16px;">
        <div class="toolbar">
          <div>
            <div style="font-weight:700;">Run #{{ route.params.runId }}</div>
            <div class="muted">状态：<span class="badge" :class="detail.run?.status || 'queued'">{{ detail.run?.status || '-' }}</span></div>
          </div>
          <div class="section-actions">
            <button class="btn secondary" @click="reload">刷新</button>
            <button class="btn secondary" @click="testOpenCurrentProfile" :disabled="!detail.run?.browserProfileId">测试打开</button>
            <button class="btn success" @click="startTakeover" :disabled="!detail.run?.browserProfileId">开始接管</button>
            <button class="btn warn" @click="stopTakeover" :disabled="!detail.run?.browserProfileId">结束接管</button>
            <button class="btn" @click="replayCurrentRun">重跑</button>
          </div>
        </div>
        <div v-if="message" class="muted" style="margin-top:10px;">{{ message }}</div>
      </div>

      <div class="grid" style="grid-template-columns:repeat(4,1fr); margin-bottom:16px;">
        <div class="card">
          <div style="font-weight:700;">当前步骤</div>
          <div class="muted" style="margin-top:8px;">{{ detail.run?.currentStepLabel || '-' }}</div>
        </div>
        <div class="card">
          <div style="font-weight:700;">当前 URL</div>
          <div class="muted" style="margin-top:8px; word-break:break-all;">{{ detail.run?.currentUrl || '-' }}</div>
        </div>
        <div class="card">
          <div style="font-weight:700;">错误代码</div>
          <div class="muted" style="margin-top:8px;">{{ detail.run?.errorCode || '-' }}</div>
        </div>
        <div class="card">
          <div style="font-weight:700;">Profile</div>
          <div class="muted" style="margin-top:8px;">{{ detail.run?.browserProfileId || '-' }}</div>
        </div>
      </div>

      <div class="grid" style="grid-template-columns:1.2fr 1fr; align-items:start;">
        <div class="card">
          <div class="toolbar">
            <div style="font-weight:700;">步骤日志时间线</div>
            <div class="muted">按记录顺序展示</div>
          </div>

          <div v-if="!detail.logs?.length" class="muted" style="margin-top:10px;">暂无日志。这个 run 可能还没开始写步骤日志，或者执行器尚未上报。</div>
          <div v-else>
            <div v-for="log in detail.logs || []" :key="log.id" class="card card-dark" style="margin-top:10px;">
              <div class="toolbar">
                <div>
                  <div style="font-weight:700;">{{ log.stepId || '-' }}</div>
                  <div class="muted">{{ log.message }}</div>
                </div>
                <div class="section-actions">
                  <span class="badge" :class="log.level || 'info'">{{ log.level || 'info' }}</span>
                  <span class="muted">{{ formatTime(log.createdAt) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="grid">
          <div class="card">
            <div style="font-weight:700;">结果 JSON</div>
            <pre style="white-space:pre-wrap; margin-top:10px;">{{ prettyJson(detail.run?.resultJson) }}</pre>
          </div>

          <div class="card">
            <div style="font-weight:700;">错误信息</div>
            <pre style="white-space:pre-wrap; margin-top:10px;">{{ detail.run?.errorMessage || '-' }}</pre>
          </div>

          <div class="card">
            <div style="font-weight:700;">隔离报告</div>
            <div v-if="!detail.isolationReports?.length" class="muted" style="margin-top:10px;">暂无 isolation report</div>
            <div v-else>
              <div v-for="item in detail.isolationReports" :key="item.id" class="card card-dark" style="margin-top:10px;">
                <div class="muted">结果：{{ item.result || '-' }}</div>
                <div class="muted">创建时间：{{ formatTime(item.createdAt) }}</div>
                <pre style="white-space:pre-wrap; margin-top:8px;">{{ prettyJson(item.storageCheckJson) }}</pre>
              </div>
            </div>
          </div>

          <div class="card">
            <div style="font-weight:700;">产物列表</div>
            <div v-if="!detail.artifacts?.length" class="muted" style="margin-top:10px;">暂无产物</div>
            <div v-else>
              <div v-for="item in detail.artifacts || []" :key="item.id" class="card card-dark" style="margin-top:10px;">
                <div style="font-weight:700;">{{ item.fileName || item.artifactType }}</div>
                <div class="muted" style="word-break:break-all;">{{ item.filePath }}</div>
              </div>
            </div>
          </div>

          <div class="card">
            <div style="font-weight:700;">运行摘要</div>
            <div class="muted" style="margin-top:10px;">createdAt：{{ formatTime(detail.run?.createdAt) }}</div>
            <div class="muted">startedAt：{{ formatTime(detail.run?.startedAt) }}</div>
            <div class="muted">heartbeatAt：{{ formatTime(detail.run?.heartbeatAt) }}</div>
            <div class="muted">finishedAt：{{ formatTime(detail.run?.finishedAt) }}</div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { reactive, onMounted, ref, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { api, SIGNALR_BASE_URL } from '../services/api'
import { auth } from '../services/auth'

const route = useRoute()
const detail = reactive({ run: null, logs: [], artifacts: [], isolationReports: [] })
const message = ref('')
let connection = null

function formatTime(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString()
}

function prettyJson(value) {
  if (!value) return '-'
  try { return JSON.stringify(JSON.parse(value), null, 2) }
  catch { return value }
}

async function reload() {
  if (!route.params.runId) return
  const [data, isolationReports] = await Promise.all([
    api.runDetail(route.params.runId),
    api.runIsolationReport(route.params.runId)
  ])
  detail.run = data.run
  detail.logs = data.logs || []
  detail.artifacts = data.artifacts || []
  detail.isolationReports = isolationReports || []
}

async function testOpenCurrentProfile() {
  if (!detail.run?.browserProfileId) return
  try {
    await api.testOpenProfile(detail.run.browserProfileId)
    message.value = `已发送测试打开命令：Profile #${detail.run.browserProfileId}`
  } catch (err) {
    message.value = err.message || '测试打开失败'
  }
}

async function startTakeover() {
  if (!detail.run?.browserProfileId) return
  try {
    await api.takeover(detail.run.browserProfileId, true)
    message.value = `已发送开始接管命令：Profile #${detail.run.browserProfileId}`
  } catch (err) {
    message.value = err.message || '开始接管失败'
  }
}

async function stopTakeover() {
  if (!detail.run?.browserProfileId) return
  try {
    await api.takeover(detail.run.browserProfileId, false)
    message.value = `已发送结束接管命令：Profile #${detail.run.browserProfileId}`
  } catch (err) {
    message.value = err.message || '结束接管失败'
  }
}

async function replayCurrentRun() {
  if (!route.params.runId) return
  try {
    const res = await api.replayRun(route.params.runId)
    message.value = `已创建重跑 run #${res.replayRunId}`
  } catch (err) {
    message.value = err.message || '重跑失败'
  }
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
