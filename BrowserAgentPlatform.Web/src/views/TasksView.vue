<template>
  <div>
    <div class="page-title">任务中心</div>
    <div class="page-subtitle">创建任务、查看运行记录，并跳转到 Live 调试。</div>

    <div class="grid" style="grid-template-columns: 420px 1fr 1fr; gap:16px;">
      <div class="card grid">
        <div style="font-weight:700;">创建任务</div>

        <input class="input" v-model="form.name" placeholder="任务名称" />

        <select class="input" v-model="form.browserProfileId">
          <option :value="null">选择 BrowserProfile</option>
          <option v-for="item in profileOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name }}（{{ item.status }}）
          </option>
        </select>

        <select class="input" v-model="form.schedulingStrategy">
          <option value="profile_owner">profile_owner</option>
          <option value="preferred_agent">preferred_agent</option>
          <option value="least_loaded">least_loaded</option>
        </select>

        <select class="input" v-model="form.preferredAgentId">
          <option :value="null">无 preferredAgent</option>
          <option v-for="item in agentOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name || 'Unnamed Agent' }}（{{ item.status }}）
          </option>
        </select>

        <input class="input" v-model.number="form.priority" type="number" placeholder="优先级" />

        <textarea class="input" rows="12" v-model="form.payloadJson" placeholder="任务 PayloadJson"></textarea>

        <div class="section-actions">
          <button class="btn" @click="save" :disabled="saving">{{ saving ? '创建中...' : '创建任务' }}</button>
          <button class="btn secondary" @click="fillExample">填充示例</button>
          <button class="btn secondary" @click="load">刷新</button>
        </div>

        <div v-if="message" class="muted">{{ message }}</div>
      </div>

      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">任务列表</div>
            <div class="muted">这里可以直观看到任务与 Profile / 调度策略的关系。</div>
          </div>
        </div>

        <div v-if="!tasks.length" class="muted">暂无任务</div>

        <div v-for="item in tasks" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
          <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;">
            <div style="flex:1;">
              <div style="font-weight:700;">{{ item.name }} #{{ item.id }}</div>
              <div style="margin-top:8px;">
                <span class="badge" :class="item.status">{{ item.status }}</span>
              </div>
              <div class="muted" style="margin-top:8px;">profile={{ item.browserProfileId }}</div>
              <div class="muted">strategy={{ item.schedulingStrategy }}</div>
              <div class="muted">preferredAgentId={{ item.preferredAgentId || '-' }}</div>
              <div class="muted">priority={{ item.priority }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">最近运行</div>
            <div class="muted">创建任务后，这里应该很快出现新的 run。</div>
          </div>
        </div>

        <div v-if="!runs.length" class="muted">暂无运行记录</div>

        <div v-for="run in runs" :key="run.id" class="card card-dark" style="margin-bottom:12px;">
          <div style="display:flex;justify-content:space-between;gap:12px;align-items:flex-start;">
            <div style="flex:1;">
              <div style="font-weight:700;">Run #{{ run.id }}</div>
              <div style="margin-top:8px;">
                <span class="badge" :class="run.status">{{ run.status }}</span>
              </div>
              <div class="muted" style="margin-top:8px;">task={{ run.taskId }} / profile={{ run.browserProfileId }}</div>
              <div class="muted">step={{ run.currentStepLabel || '-' }}</div>
              <div class="muted">url={{ run.currentUrl || '-' }}</div>
            </div>
            <RouterLink :to="`/live/${run.id}`" class="btn">查看 Live</RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, computed, reactive } from 'vue'
import { RouterLink } from 'vue-router'
import { api } from '../services/api'

const tasks = ref([])
const runs = ref([])
const agents = ref([])
const profiles = ref([])
const saving = ref(false)
const message = ref('')
let timer = null

const form = reactive({
  name: '',
  browserProfileId: null,
  schedulingStrategy: 'profile_owner',
  preferredAgentId: null,
  priority: 100,
  payloadJson: '{}'
})

const agentOptions = computed(() => agents.value)
const profileOptions = computed(() => profiles.value)

function fillExample() {
  form.name = '打开示例网站'
  form.payloadJson = JSON.stringify({
    steps: [
      { id: 'step_open', type: 'open', label: '打开 example', url: 'https://example.com' },
      { id: 'step_wait', type: 'wait_for_timeout', label: '等待', timeoutMs: 2000 },
      { id: 'step_done', type: 'end_success', label: '结束' }
    ]
  }, null, 2)
}

async function load() {
  const [taskList, runList, agentList, profileList] = await Promise.all([
    api.tasks(),
    api.runs(),
    api.agents(),
    api.profiles()
  ])
  tasks.value = taskList
  runs.value = runList
  agents.value = agentList
  profiles.value = profileList
}

async function save() {
  saving.value = true
  message.value = ''
  try {
    await api.createTask({
      name: form.name,
      browserProfileId: form.browserProfileId,
      schedulingStrategy: form.schedulingStrategy,
      preferredAgentId: form.preferredAgentId,
      priority: form.priority,
      payloadJson: form.payloadJson
    })
    message.value = '任务已创建'
    form.name = ''
    form.browserProfileId = null
    form.schedulingStrategy = 'profile_owner'
    form.preferredAgentId = null
    form.priority = 100
    form.payloadJson = '{}'
    await load()
  } catch (err) {
    message.value = err.message || '创建任务失败'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await load()
  timer = setInterval(load, 5000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>
