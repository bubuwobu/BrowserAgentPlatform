<template>
  <div>
    <div class="page-title">任务中心</div>
    <div class="page-subtitle">创建任务、查看运行记录，并跳转到 Live 调试。</div>

    <div class="grid" style="grid-template-columns: 420px 1fr 1fr; gap:16px;">
      <div class="card grid">
        <div style="font-weight:700;">创建任务</div>

        <input class="input" v-model="form.name" placeholder="任务名称" />

        <select class="input" v-model="form.browserProfileId">
          <option :value="null">请选择 BrowserProfile（必选）</option>
          <option v-for="item in profileOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name }}（{{ item.status }}）
          </option>
        </select>

        <select class="input" v-model="form.schedulingStrategy">
          <option value="least_loaded">least_loaded（推荐）</option>
          <option value="profile_owner">profile_owner（需 Profile 绑定 owner）</option>
          <option value="preferred_agent">preferred_agent</option>
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

        <div class="muted">
          * BrowserProfile 必选。<br />
          * `least_loaded`：任意在线 Agent 都可执行（最不容易卡队列）。<br />
          * `profile_owner`：仅 Profile 的 owner Agent 可执行。<br />
          * `preferred_agent`：必须再选择 preferredAgent。
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
              <div class="muted">result={{ run.resultJson || '-' }}</div>
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
  schedulingStrategy: 'least_loaded',
  preferredAgentId: null,
  priority: 100,
  payloadJson: '{}'
})

const agentOptions = computed(() => agents.value)
const profileOptions = computed(() => profiles.value)
const selectedProfile = computed(() => profiles.value.find(x => x.id === form.browserProfileId))

function fillExample() {
  form.name = '百度搜索示例流程'
  form.payloadJson = JSON.stringify({
    steps: [
      { id: 'step_open', type: 'open', data: { label: '打开百度首页', url: 'https://www.baidu.com' } },
      { id: 'step_wait_input', type: 'wait_for_element', data: { label: '等待搜索输入框', selector: '#kw', timeout: 15000 } },
      { id: 'step_type_keyword', type: 'type', data: { label: '输入关键词', selector: '#kw', value: 'BrowserAgentPlatform 自动化测试' } },
      { id: 'step_click_search', type: 'click', data: { label: '点击搜索按钮', selector: '#su' } },
      { id: 'step_wait_result', type: 'wait_for_element', data: { label: '等待结果区域', selector: '#content_left', timeout: 15000 } },
      { id: 'step_extract_title', type: 'extract_text', data: { label: '提取首条结果标题', selector: '#content_left h3' } },
      { id: 'step_done', type: 'end_success', data: { label: '完成' } }
    ],
    edges: [
      { source: 'step_open', target: 'step_wait_input' },
      { source: 'step_wait_input', target: 'step_type_keyword' },
      { source: 'step_type_keyword', target: 'step_click_search' },
      { source: 'step_click_search', target: 'step_wait_result' },
      { source: 'step_wait_result', target: 'step_extract_title' },
      { source: 'step_extract_title', target: 'step_done' }
    ]
  }, null, 2)
}

async function load() {
  const [taskRes, runRes, agentRes, profileRes] = await Promise.allSettled([
    api.tasks(),
    api.runs(),
    api.agents(),
    api.profiles()
  ])

  tasks.value = taskRes.status === 'fulfilled' ? taskRes.value : []
  runs.value = runRes.status === 'fulfilled' ? runRes.value : []
  agents.value = agentRes.status === 'fulfilled' ? agentRes.value : []
  profiles.value = profileRes.status === 'fulfilled' ? profileRes.value : []

  const errors = [taskRes, runRes, agentRes, profileRes]
    .filter(item => item.status === 'rejected')
    .map(item => item.reason?.message || '请求失败')

  if (errors.length) {
    message.value = `部分数据加载失败：${errors.join('；')}`
  } else if (!saving.value) {
    message.value = ''
  }
}

async function save() {
  saving.value = true
  message.value = ''
  try {
    if (!form.browserProfileId) {
      message.value = '请先选择 BrowserProfile。'
      return
    }

    if (form.schedulingStrategy === 'preferred_agent' && !form.preferredAgentId) {
      message.value = '当前策略为 preferred_agent，请选择 preferredAgent。'
      return
    }

    if (form.schedulingStrategy === 'profile_owner' && !selectedProfile.value?.ownerAgentId) {
      message.value = '当前 Profile 没有 ownerAgent，不能用 profile_owner。请改为 least_loaded 或先绑定 owner。'
      return
    }

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
    form.schedulingStrategy = 'least_loaded'
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
