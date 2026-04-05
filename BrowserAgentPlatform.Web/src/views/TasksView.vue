<template>
  <div>
    <div class="page-title">任务中心</div>
    <div class="page-subtitle">支持账号绑定、调度配置、真实编辑和立即执行。</div>

    <div class="card" style="margin-bottom:16px;">
      <div class="toolbar">
        <div>
          <div style="font-weight:700;">任务操作区</div>
          <div class="muted">列表优先；新增与编辑统一在弹窗中完成。</div>
        </div>
        <div class="section-actions">
          <button class="btn" @click="openCreate">新增任务</button>
          <button class="btn secondary" @click="load">刷新</button>
        </div>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div class="grid" style="grid-template-columns:1fr 1fr; gap:16px;">
      <div class="card">
        <div style="font-weight:700;margin-bottom:12px;">任务列表</div>
        <div v-if="!tasks.length" class="muted">暂无任务</div>
        <div v-for="item in tasks" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
          <div class="toolbar">
            <div>
              <div style="font-weight:700;">{{ item.name }} #{{ item.id }}</div>
              <div class="muted">profile={{ item.browserProfileId }} / account={{ item.accountId || '-' }}</div>
              <div class="muted">schedule={{ item.scheduleType }} / nextRunAt={{ formatTime(item.nextRunAt) }}</div>
            </div>
            <div class="section-actions">
              <span class="badge" :class="item.isEnabled ? 'active' : 'disabled'">{{ item.isEnabled ? 'enabled' : 'disabled' }}</span>
              <button class="btn secondary" @click="openEdit(item)">编辑</button>
              <button class="btn success" @click="runNow(item.id)">立即执行</button>
              <button class="btn secondary" @click="toggleEnabled(item.id)">启停</button>
              <button class="btn warn" @click="askDelete(item.id)">删除</button>
            </div>
          </div>
          <div class="kv-grid muted" style="margin-top:10px;">
            <div>优先级：{{ item.priority }}</div>
            <div>超时：{{ item.timeoutSeconds }} 秒</div>
            <div>调度策略：{{ item.schedulingStrategy }}</div>
            <div>最近执行：{{ formatTime(item.lastRunAt) }}</div>
          </div>
        </div>
      </div>

      <div class="card">
        <div style="font-weight:700;margin-bottom:12px;">最近运行</div>
        <div v-if="!runs.length" class="muted">暂无运行记录</div>
        <div v-for="run in runs" :key="run.id" class="card card-dark" style="margin-bottom:12px;">
          <div class="toolbar">
            <div>
              <div style="font-weight:700;">Run #{{ run.id }}</div>
              <div class="muted">task={{ run.taskId }} / profile={{ run.browserProfileId }}</div>
              <div class="muted">step={{ run.currentStepLabel || '-' }}</div>
            </div>
            <div class="section-actions">
              <span class="badge" :class="run.status">{{ run.status }}</span>
              <RouterLink :to="`/live/${run.id}`" class="btn">查看 Live</RouterLink>
            </div>
          </div>
        </div>
      </div>
    </div>

    <ConfirmDialog
      :open="deleteOpen"
      title="删除任务"
      message="删除后，任务本身会被移除。
已产生的运行记录不会自动回滚。"
      confirm-text="确认删除"
      @cancel="deleteOpen = false"
      @confirm="removeTaskConfirmed"
    />

    <div v-if="editorOpen" class="modal-mask">
      <div class="modal-panel card">
        <div class="toolbar">
          <div style="font-weight:700;">{{ editingId ? '编辑任务' : '新增任务' }}</div>
          <div class="section-actions">
            <button class="btn secondary" @click="editorMode = editorMode === 'form' ? 'json' : 'form'">{{ editorMode === 'form' ? '切到 JSON' : '切到表单' }}</button>
            <button class="btn secondary" @click="editorOpen = false">关闭</button>
          </div>
        </div>

        <div class="grid" style="grid-template-columns:1fr 1fr; gap:12px; margin-top:12px;">
          <FormField label="任务名称" :required="true" help="用于任务中心识别和筛选。">
            <input class="input" v-model="form.name" placeholder="例如：账号1每日浏览任务" />
          </FormField>

          <FormField label="绑定账号" help="选择后，如果账号已绑定 BrowserProfile，会自动带出。">
            <select class="input" v-model="form.accountId" @change="syncProfileFromAccount">
              <option :value="null">不绑定账号</option>
              <option v-for="item in accounts" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}（{{ item.username }}）</option>
            </select>
          </FormField>

          <FormField label="BrowserProfile" :required="true" help="任务实际执行时使用的浏览器身份。">
            <select class="input" v-model="form.browserProfileId">
              <option :value="null">请选择 BrowserProfile</option>
              <option v-for="item in profiles" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}（{{ item.status }}）</option>
            </select>
          </FormField>

          <FormField label="调度策略（Agent 选择）" help="决定任务由哪个 Agent 拿到。">
            <select class="input" v-model="form.schedulingStrategy">
              <option value="least_loaded">least_loaded</option>
              <option value="profile_owner">profile_owner</option>
              <option value="preferred_agent">preferred_agent</option>
            </select>
          </FormField>

          <FormField v-if="form.schedulingStrategy === 'preferred_agent'" label="指定 Agent">
            <select class="input" v-model="form.preferredAgentId">
              <option :value="null">请选择 Agent</option>
              <option v-for="item in agents" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}</option>
            </select>
          </FormField>

          <FormField label="是否启用" help="禁用后调度器不会自动生成新的 run。">
            <select class="input" v-model="form.isEnabled">
              <option :value="true">true</option>
              <option :value="false">false</option>
            </select>
          </FormField>

          <FormField label="任务调度类型" help="manual 为纯手动，daily_window_random 为每日时间窗随机触发。">
            <select class="input" v-model="form.scheduleType">
              <option value="manual">manual</option>
              <option value="daily_window_random">daily_window_random</option>
            </select>
          </FormField>

          <template v-if="form.scheduleType === 'daily_window_random'">
            <FormField label="开始时间" help="示例：09:00">
              <input class="input" v-model="scheduleForm.windowStart" placeholder="09:00" />
            </FormField>
            <FormField label="结束时间" help="示例：18:00">
              <input class="input" v-model="scheduleForm.windowEnd" placeholder="18:00" />
            </FormField>
            <FormField label="每日最大触发次数" help="当前后端已开始按这个值限制每日创建 run 的上限。">
              <input class="input" type="number" v-model.number="scheduleForm.maxRunsPerDay" />
            </FormField>
            <FormField label="随机分钟步长" help="例如 5 表示按 5 分钟粒度随机。">
              <input class="input" type="number" v-model.number="scheduleForm.randomMinuteStep" />
            </FormField>
          </template>

          <FormField label="优先级" help="数字越大，理论上越优先。">
            <input class="input" type="number" v-model.number="form.priority" />
          </FormField>

          <FormField label="超时（秒）">
            <input class="input" type="number" v-model.number="form.timeoutSeconds" />
          </FormField>
        </div>

        <div class="card card-dark" style="margin-top:12px;">
          <div class="toolbar">
            <div style="font-weight:700;">Payload 编辑</div>
            <div class="muted">普通用户推荐用编排器页面维护；这里可直接调整任务 payload。</div>
          </div>

          <template v-if="editorMode === 'json'">
            <FormField label="Payload JSON">
              <textarea class="input" rows="14" v-model="form.payloadJson"></textarea>
            </FormField>
          </template>

          <template v-else>
            <div class="help" style="margin-top:12px;">当前阶段这里保留文本编辑，完整可视化编辑请去“编排器”页面。</div>
            <textarea class="input" rows="10" v-model="form.payloadJson" style="margin-top:8px;"></textarea>
          </template>
        </div>

        <div class="card card-dark" v-if="form.scheduleType === 'daily_window_random'" style="margin-top:12px;">
          <div style="font-weight:700;margin-bottom:8px;">调度预览</div>
          <div class="muted">
            每天会在 {{ scheduleForm.windowStart }} 到 {{ scheduleForm.windowEnd }} 之间，
            按 {{ scheduleForm.randomMinuteStep }} 分钟粒度随机挑选时间，
            最多触发 {{ scheduleForm.maxRunsPerDay }} 次。
          </div>
        </div>

        <div class="section-actions" style="margin-top:12px;">
          <button class="btn" @click="save">{{ editingId ? '保存修改' : '创建任务' }}</button>
          <button class="btn secondary" @click="fillSamplePayload">填充示例 Payload</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api } from '../services/api'
import FormField from '../components/FormField.vue'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const tasks = ref([])
const runs = ref([])
const agents = ref([])
const profiles = ref([])
const accounts = ref([])
const editorOpen = ref(false)
const editingId = ref(null)
const editorMode = ref('form')
const message = ref('')
const deleteOpen = ref(false)
const deleteId = ref(null)

const form = reactive({
  name: '',
  browserProfileId: null,
  accountId: null,
  schedulingStrategy: 'least_loaded',
  preferredAgentId: null,
  isEnabled: true,
  scheduleType: 'manual',
  scheduleConfigJson: '{}',
  payloadJson: '{}',
  priority: 100,
  timeoutSeconds: 300,
  retryPolicyJson: '{"maxRetries":1}'
})

const scheduleForm = reactive({
  windowStart: '09:00',
  windowEnd: '18:00',
  maxRunsPerDay: 1,
  randomMinuteStep: 5
})

function resetForm() {
  editingId.value = null
  Object.assign(form, {
    name: '',
    browserProfileId: null,
    accountId: null,
    schedulingStrategy: 'least_loaded',
    preferredAgentId: null,
    isEnabled: true,
    scheduleType: 'manual',
    scheduleConfigJson: '{}',
    payloadJson: '{}',
    priority: 100,
    timeoutSeconds: 300,
    retryPolicyJson: '{"maxRetries":1}'
  })
  Object.assign(scheduleForm, {
    windowStart: '09:00',
    windowEnd: '18:00',
    maxRunsPerDay: 1,
    randomMinuteStep: 5
  })
}

function openCreate() {
  resetForm()
  editorOpen.value = true
}

function openEdit(item) {
  editingId.value = item.id
  form.name = item.name || ''
  form.browserProfileId = item.browserProfileId
  form.accountId = item.accountId
  form.schedulingStrategy = item.schedulingStrategy || 'least_loaded'
  form.preferredAgentId = item.preferredAgentId
  form.isEnabled = item.isEnabled ?? true
  form.scheduleType = item.scheduleType || 'manual'
  form.scheduleConfigJson = item.scheduleConfigJson || '{}'
  form.payloadJson = item.payloadJson || '{}'
  form.priority = item.priority || 100
  form.timeoutSeconds = item.timeoutSeconds || 300
  form.retryPolicyJson = item.retryPolicyJson || '{"maxRetries":1}'
  try {
    const parsed = JSON.parse(form.scheduleConfigJson || '{}')
    scheduleForm.windowStart = parsed.windowStart || '09:00'
    scheduleForm.windowEnd = parsed.windowEnd || '18:00'
    scheduleForm.maxRunsPerDay = parsed.maxRunsPerDay || 1
    scheduleForm.randomMinuteStep = parsed.randomMinuteStep || 5
  } catch {}
  editorOpen.value = true
}

function syncProfileFromAccount() {
  const account = accounts.value.find(x => x.id === Number(form.accountId))
  if (account?.browserProfileId) {
    form.browserProfileId = account.browserProfileId
  }
}

function buildScheduleJson() {
  if (form.scheduleType !== 'daily_window_random') return '{}'
  return JSON.stringify({
    timezone: 'UTC',
    windowStart: scheduleForm.windowStart,
    windowEnd: scheduleForm.windowEnd,
    maxRunsPerDay: scheduleForm.maxRunsPerDay,
    randomMinuteStep: scheduleForm.randomMinuteStep
  })
}

function fillSamplePayload() {
  form.payloadJson = JSON.stringify({
    steps: [
      { id: 'step_open', type: 'open', data: { label: '打开页面', url: 'https://example.com' } },
      { id: 'step_wait', type: 'wait_for_timeout', data: { label: '等待', timeout: 1000 } },
      { id: 'step_done', type: 'end_success', data: { label: '完成' } }
    ],
    edges: [
      { source: 'step_open', target: 'step_wait' },
      { source: 'step_wait', target: 'step_done' }
    ]
  }, null, 2)
}

function formatTime(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString()
}

function askDelete(id) {
  deleteId.value = id
  deleteOpen.value = true
}

async function load() {
  const [taskList, runList, agentList, profileList, accountList] = await Promise.all([
    api.tasks(), api.runs(), api.agents(), api.profiles(), api.accounts()
  ])
  tasks.value = taskList
  runs.value = runList
  agents.value = agentList
  profiles.value = profileList
  accounts.value = accountList
}

async function save() {
  try {
    const body = {
      ...form,
      scheduleConfigJson: buildScheduleJson()
    }
    if (editingId.value) {
      await api.updateTask(editingId.value, body)
      message.value = '任务已更新'
    } else {
      await api.createTask(body)
      message.value = '任务已创建'
    }
    editorOpen.value = false
    await load()
  } catch (err) {
    message.value = err.message || '保存失败'
  }
}

async function runNow(id) {
  try {
    await api.runNowTask(id)
    message.value = `任务 #${id} 已立即入队`
    await load()
  } catch (err) {
    message.value = err.message || '立即执行失败'
  }
}

async function toggleEnabled(id) {
  try {
    await api.toggleTaskEnabled(id)
    await load()
  } catch (err) {
    message.value = err.message || '启停失败'
  }
}

async function removeTaskConfirmed() {
  try {
    if (deleteId.value) {
      await api.deleteTask(deleteId.value)
      message.value = `任务 #${deleteId.value} 已删除`
      deleteOpen.value = false
      deleteId.value = null
      await load()
    }
  } catch (err) {
    message.value = err.message || '删除失败'
  }
}

onMounted(() => {
  load()
  fillSamplePayload()
})
</script>
