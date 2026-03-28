<template>
  <div>
    <div class="page-title">闭环工作台</div>
    <div class="page-subtitle">按 1-2-3 步完成：隔离检查 → 创建闭环任务 → 执行并查看结果。</div>

    <div class="grid" style="grid-template-columns: 1fr 1fr 1fr; gap: 16px;">
      <div class="card">
        <div style="font-weight:700;">Step 1: 选择 Profile 并做隔离检查</div>
        <div class="muted" style="margin:6px 0 10px;">隔离通过后再执行，避免无效 run。</div>
        <select class="input" v-model.number="form.profileId">
          <option :value="null">请选择 Profile</option>
          <option v-for="p in profiles" :key="p.id" :value="p.id">{{ p.id }} - {{ p.name }}（{{ p.status }}）</option>
        </select>
        <button class="btn" style="margin-top:10px;" @click="checkIsolation" :disabled="!form.profileId || checkingIsolation">
          {{ checkingIsolation ? '检查中...' : '执行隔离检查' }}
        </button>
        <div class="muted" style="margin-top:10px;">结果：{{ isolationStatus }}</div>
      </div>

      <div class="card">
        <div style="font-weight:700;">Step 2: 创建闭环任务</div>
        <div class="muted" style="margin:6px 0 10px;">默认使用 TikTok 模板 Payload（可自定义）。</div>
        <input class="input" v-model="form.agentKey" placeholder="agent key" />
        <input class="input" v-model="form.taskName" style="margin-top:8px;" placeholder="任务名（可选）" />
        <button class="btn" style="margin-top:10px;" @click="startClosedLoop" :disabled="!canStart || starting">
          {{ starting ? '创建中...' : '创建闭环 Run' }}
        </button>
        <div class="muted" style="margin-top:10px;">runId：{{ form.runId || '-' }}</div>
      </div>

      <div class="card">
        <div style="font-weight:700;">Step 3: 执行闭环并查看</div>
        <div class="muted" style="margin:6px 0 10px;">执行后可一键跳转 Live。</div>
        <button class="btn" @click="executeClosedLoop" :disabled="!form.runId || executing">{{ executing ? '执行中...' : '执行闭环' }}</button>
        <RouterLink v-if="form.runId" :to="`/live/${form.runId}`" class="btn secondary" style="margin-top:8px;">查看 Live</RouterLink>
        <div class="muted" style="margin-top:10px;">执行状态：{{ executeStatus }}</div>
      </div>
    </div>

    <div class="card" style="margin-top:16px;">
      <div style="font-weight:700;">闭环说明</div>
      <ol class="muted" style="margin:8px 0 0; padding-left:18px; display:grid; gap:6px;">
        <li>Step 1 不通过时，不建议继续执行。</li>
        <li>Step 2 会创建一个高优先级闭环 run。</li>
        <li>Step 3 执行后，去 Live 看结果和日志，再决定是否重跑。</li>
      </ol>
      <div v-if="message" class="muted" style="margin-top:10px;">{{ message }}</div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, computed } from 'vue'
import { RouterLink } from 'vue-router'
import { api } from '../services/api'

const profiles = ref([])
const checkingIsolation = ref(false)
const starting = ref(false)
const executing = ref(false)
const isolationStatus = ref('未检查')
const executeStatus = ref('未执行')
const message = ref('')

const form = reactive({
  profileId: null,
  agentKey: 'agent-local-001',
  taskName: 'closed-loop-workbench',
  runId: null
})

const canStart = computed(() => !!form.profileId && isolationStatus.value.startsWith('通过'))

async function load() {
  profiles.value = await api.profiles()
}

async function checkIsolation() {
  if (!form.profileId) return
  checkingIsolation.value = true
  try {
    const result = await api.profileIsolationCheck(form.profileId)
    isolationStatus.value = result?.ok
      ? `通过${(result.warnings || []).length ? `（警告:${(result.warnings || []).join('|')}）` : ''}`
      : `失败：${(result?.errors || []).join('|') || 'unknown'}`
  } catch (err) {
    isolationStatus.value = `失败：${err.message || err}`
  } finally {
    checkingIsolation.value = false
  }
}

async function startClosedLoop() {
  starting.value = true
  message.value = ''
  try {
    const payload = {
      steps: [
        {
          id: 'tiktok_session',
          type: 'tiktok_mock_session',
          data: {
            label: 'workbench session',
            baseUrl: 'http://localhost:3001',
            username: 'alice',
            password: '123456',
            minVideos: 2,
            maxVideos: 4,
            minWatchMs: 2500,
            maxWatchMs: 7000,
            minLikes: 1,
            maxLikes: 2,
            minComments: 1,
            maxComments: 2,
            behaviorProfile: 'balanced',
            commentProvider: 'deepseek'
          }
        },
        { id: 'done', type: 'end_success', data: { label: '完成' } }
      ],
      edges: [{ source: 'tiktok_session', target: 'done' }]
    }

    const res = await api.closedLoopStart({
      profileId: form.profileId,
      agentKey: form.agentKey,
      taskName: form.taskName,
      payloadJson: JSON.stringify(payload)
    })
    form.runId = res?.runId || res?.taskRunId || null
    message.value = `闭环任务创建成功，runId=${form.runId}`
  } catch (err) {
    message.value = err.message || '创建闭环失败'
  } finally {
    starting.value = false
  }
}

async function executeClosedLoop() {
  if (!form.runId) return
  executing.value = true
  try {
    const res = await api.closedLoopExecute({ runId: form.runId, agentKey: form.agentKey })
    executeStatus.value = res?.ok ? '执行成功' : `执行失败：${res?.message || 'unknown'}`
  } catch (err) {
    executeStatus.value = `执行失败：${err.message || err}`
  } finally {
    executing.value = false
  }
}

onMounted(load)
</script>
