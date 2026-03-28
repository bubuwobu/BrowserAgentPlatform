<template>
  <div>
    <div class="page-title">任务中心</div>
    <div class="page-subtitle">创建任务、查看运行记录，并跳转到 Live 调试。</div>

    <div class="card" style="margin-bottom:16px;">
      <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
        <div>
          <div style="font-weight:700;">任务操作区</div>
          <div class="muted">默认只显示列表数据；新增/编辑任务通过弹窗完成。</div>
        </div>
        <div class="section-actions">
          <button class="btn" @click="editorExpanded = true">新增/编辑任务</button>
          <button class="btn secondary" @click="load">刷新</button>
        </div>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div v-if="editorExpanded" class="modal-mask" @click.self="editorExpanded = false">
      <div class="modal-panel card">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
          <div style="font-weight:700;">任务编辑弹窗</div>
          <div class="section-actions">
            <button class="btn secondary" @click="advancedMode = !advancedMode">{{ advancedMode ? '隐藏高级项' : '显示高级项' }}</button>
            <button class="btn secondary" @click="editorExpanded = false">关闭</button>
          </div>
        </div>

      <div class="grid" style="margin-top:12px;">
        <input class="input" v-model="form.name" placeholder="任务名称" />

        <div class="grid" style="grid-template-columns: 1fr 1fr; gap:8px;">
          <select class="input" v-model="form.browserProfileId">
            <option :value="null">请选择 BrowserProfile（必选）</option>
            <option v-for="item in profileOptions" :key="item.id" :value="item.id">
              {{ item.id }} - {{ item.name }}（{{ item.status }}）
            </option>
          </select>

          <div class="card card-dark" style="padding:10px;">
            <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
              <div style="font-weight:700;">编辑模式</div>
              <div class="section-actions">
                <button class="btn secondary" @click="editorMode = 'form'" :disabled="editorMode === 'form'">配置表单</button>
                <button class="btn secondary" @click="editorMode = 'json'" :disabled="editorMode === 'json'">高级 JSON</button>
              </div>
            </div>
          </div>
        </div>

        <select v-if="advancedMode" class="input" v-model="form.schedulingStrategy">
          <option value="least_loaded">least_loaded（推荐）</option>
          <option value="profile_owner">profile_owner（需 Profile 绑定 owner）</option>
          <option value="preferred_agent">preferred_agent</option>
        </select>

        <select v-if="advancedMode" class="input" v-model="form.preferredAgentId">
          <option :value="null">无 preferredAgent</option>
          <option v-for="item in agentOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name || 'Unnamed Agent' }}（{{ item.status }}）
          </option>
        </select>

        <input v-if="advancedMode" class="input" v-model.number="form.priority" type="number" placeholder="优先级" />

        <div v-if="advancedMode" class="card card-dark">
          <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;margin-bottom:8px;">
            <div style="font-weight:700;">任务模板库</div>
            <select class="input" style="max-width:220px;" v-model="selectedTemplate">
              <option value="tiktok">TikTok 模板</option>
              <option value="baidu">Baidu 模板</option>
              <option value="facebook">Facebook 模板</option>
            </select>
          </div>
          <div class="muted" style="margin-bottom:8px;">一键切换模板并自动填充对应参数。</div>
          <div class="section-actions">
            <button class="btn secondary" @click="applyTemplate">应用当前模板</button>
          </div>
        </div>

        <div v-if="editorMode === 'form'" class="card card-dark">
          <div style="font-weight:700;margin-bottom:8px;">模板参数配置（{{ templateLabel }}）</div>
          <div class="grid" style="grid-template-columns:1fr 1fr;gap:8px;">
            <template v-if="selectedTemplate === 'tiktok'">
              <input class="input" v-model="tiktokPlan.baseUrl" placeholder="baseUrl" />
              <input class="input" v-model="tiktokPlan.username" placeholder="username" />
              <input class="input" v-model="tiktokPlan.password" placeholder="password" />
              <input class="input" v-model.number="tiktokPlan.minVideos" type="number" placeholder="最少视频数" />
              <input class="input" v-model.number="tiktokPlan.maxVideos" type="number" placeholder="最多视频数" />
              <input class="input" v-model.number="tiktokPlan.minWatchMs" type="number" placeholder="最短停留(ms)" />
              <input class="input" v-model.number="tiktokPlan.maxWatchMs" type="number" placeholder="最长停留(ms)" />
              <input class="input" v-model.number="tiktokPlan.minLikes" type="number" placeholder="最少点赞" />
              <input class="input" v-model.number="tiktokPlan.maxLikes" type="number" placeholder="最多点赞" />
              <input class="input" v-model.number="tiktokPlan.minComments" type="number" placeholder="最少评论" />
              <input class="input" v-model.number="tiktokPlan.maxComments" type="number" placeholder="最多评论" />
              <select class="input" v-model="tiktokPlan.behaviorProfile">
                <option value="conservative">conservative</option>
                <option value="balanced">balanced</option>
                <option value="aggressive">aggressive</option>
              </select>
              <select class="input" v-model="tiktokPlan.commentProvider">
                <option value="rule">rule</option>
                <option value="deepseek">deepseek</option>
                <option value="openai">openai</option>
              </select>
            </template>
            <template v-if="selectedTemplate === 'baidu'">
              <input class="input" v-model="baiduPlan.url" placeholder="Baidu URL" />
              <input class="input" v-model="baiduPlan.keyword" placeholder="搜索关键词" />
            </template>
            <template v-if="selectedTemplate === 'facebook'">
              <input class="input" v-model="facebookPlan.url" placeholder="Facebook URL" />
              <input class="input" v-model="facebookPlan.keyword" placeholder="搜索关键词" />
            </template>
          </div>
          <div class="section-actions" style="margin-top:8px;">
            <button class="btn secondary" @click="syncPayloadFromForm">更新到 Payload</button>
          </div>
        </div>

        <details v-if="editorMode === 'json'" class="card card-dark">
          <summary style="cursor:pointer;font-weight:700;">高级 JSON 折叠区（点击展开编辑）</summary>
          <div class="muted" style="margin-top:8px;">支持直接粘贴编排 JSON；保存时按当前内容创建任务。</div>
          <textarea class="input" rows="12" v-model="form.payloadJson" placeholder="任务 PayloadJson" style="margin-top:8px;"></textarea>
        </details>

        <div v-if="editorMode === 'form'" class="card card-dark">
          <div style="font-weight:700;">编排预览（只读）</div>
          <div class="muted">按步骤顺序预览当前 payload，并默认附带 isolationGate + assertions（质量门禁）。</div>
          <div v-if="payloadPreviewError" class="muted" style="color:#ff9a9a;margin-top:6px;">{{ payloadPreviewError }}</div>
          <div v-else style="margin-top:8px;">
            <div v-for="(step, idx) in payloadSteps" :key="step.id || idx" class="muted" style="margin-bottom:4px;">
              {{ idx + 1 }}. {{ step.id || '(无ID)' }} - {{ step.type || '(无类型)' }} - {{ step.data?.label || '-' }}
            </div>
          </div>
        </div>

        <div class="section-actions">
          <button class="btn" @click="save" :disabled="saving">{{ saving ? '创建中...' : '创建任务' }}</button>
          <button class="btn secondary" @click="applyTemplate">填充当前模板</button>
        </div>

        <div class="muted">
          * BrowserProfile 必选。<br />
          * `least_loaded`：任意在线 Agent 都可执行（最不容易卡队列）。<br />
          * `profile_owner`：仅 Profile 的 owner Agent 可执行。<br />
          * `preferred_agent`：必须再选择 preferredAgent。
        </div>

      </div>
    </div>
    </div>

    <div class="grid" style="grid-template-columns: 1fr 1fr; gap:16px;">
      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">任务列表</div>
            <div class="muted">这里可以直观看到任务与 Profile / 调度策略的关系。</div>
          </div>
        </div>

        <div style="display:flex;gap:8px;margin-bottom:10px;">
          <span class="badge queued">总任务 {{ tasks.length }}</span>
          <span class="badge completed">已完成 {{ tasks.filter(x => x.status === 'completed').length }}</span>
          <span class="badge failed">失败 {{ tasks.filter(x => x.status === 'failed').length }}</span>
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
              <div class="muted" style="margin-top:8px;">payload={{ shortText(item.payloadJson, 120) }}</div>
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

        <div style="display:flex;gap:8px;margin-bottom:10px;">
          <span class="badge running">运行中 {{ runs.filter(x => x.status === 'running' || x.status === 'leased').length }}</span>
          <span class="badge completed">已完成 {{ runs.filter(x => x.status === 'completed').length }}</span>
          <span class="badge failed">失败 {{ runs.filter(x => x.status === 'failed' || x.status === 'dead' || x.status === 'timeout').length }}</span>
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
              <div class="muted">result={{ shortText(run.resultJson || '-', 180) }}</div>
            </div>
              <div style="display:grid;gap:8px;">
                <RouterLink :to="`/live/${run.id}`" class="btn">查看 Live</RouterLink>
                <button class="btn secondary" @click="replay(run.id)">重跑</button>
              </div>
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
const advancedMode = ref(false)
const editorExpanded = ref(false)

const form = reactive({
  name: '',
  browserProfileId: null,
  schedulingStrategy: 'least_loaded',
  preferredAgentId: null,
  priority: 100,
  payloadJson: '{}'
})
const selectedTemplate = ref('tiktok')
const editorMode = ref('form')

const tiktokPlan = reactive({
  baseUrl: 'http://localhost:3001',
  username: 'alice',
  password: '123456',
  minVideos: 3,
  maxVideos: 8,
  minWatchMs: 3000,
  maxWatchMs: 9000,
  minLikes: 1,
  maxLikes: 4,
  minComments: 1,
  maxComments: 3,
  behaviorProfile: 'balanced',
  commentProvider: 'deepseek'
})
const baiduPlan = reactive({
  url: 'https://www.baidu.com',
  keyword: 'BrowserAgentPlatform 自动化测试'
})
const facebookPlan = reactive({
  url: 'https://www.facebook.com',
  keyword: 'automation testing'
})

const agentOptions = computed(() => agents.value)
const profileOptions = computed(() => profiles.value)
const selectedProfile = computed(() => profiles.value.find(x => x.id === form.browserProfileId))
const templateLabel = computed(() => selectedTemplate.value === 'tiktok'
  ? 'TikTok'
  : selectedTemplate.value === 'baidu'
    ? 'Baidu'
    : 'Facebook')
const payloadSteps = computed(() => {
  try {
    const parsed = JSON.parse(form.payloadJson || '{}')
    return Array.isArray(parsed.steps) ? parsed.steps : []
  } catch {
    return []
  }
})
const payloadPreviewError = computed(() => {
  try {
    JSON.parse(form.payloadJson || '{}')
    return ''
  } catch (err) {
    return `PayloadJson 解析失败：${err?.message || 'unknown error'}`
  }
})

function shortText(text, max = 120) {
  if (!text) return '-'
  const value = String(text).replace(/\s+/g, ' ').trim()
  return value.length > max ? `${value.slice(0, max)}...` : value
}

function buildBaiduPayload() {
  return {
    isolationGate: {
      enforce: true,
      requireRecentCheckMinutes: 120
    },
    steps: [
      { id: 'step_open', type: 'open', data: { label: '打开百度首页', url: baiduPlan.url } },
      { id: 'step_wait_input', type: 'wait_for_element', data: { label: '等待搜索输入框', selector: 'textarea[name=\"wd\"]', timeout: 15000 } },
      { id: 'step_type_keyword', type: 'type', data: { label: '输入关键词', selector: 'textarea[name=\"wd\"]', value: baiduPlan.keyword } },
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
    ],
    assertions: [
      { type: 'step_exists', label: '提取步骤必须存在', stepId: 'step_extract_title' },
      { type: 'text_contains', label: '结果需包含关键词片段', sourceStepId: 'step_extract_title', expected: baiduPlan.keyword.split(' ')[0] || baiduPlan.keyword }
    ]
  }
}

function buildFacebookPayload() {
  return {
    isolationGate: {
      enforce: true,
      requireRecentCheckMinutes: 120
    },
    steps: [
      { id: 'step_open', type: 'open', data: { label: '打开 Facebook', url: facebookPlan.url } },
      { id: 'step_wait_search', type: 'wait_for_element', data: { label: '等待搜索框', selector: 'input[placeholder*=\"Search\"], input[type=\"search\"]', timeout: 15000 } },
      { id: 'step_type_keyword', type: 'type', data: { label: '输入关键词', selector: 'input[placeholder*=\"Search\"], input[type=\"search\"]', value: facebookPlan.keyword } },
      { id: 'step_done', type: 'end_success', data: { label: '完成' } }
    ],
    edges: [
      { source: 'step_open', target: 'step_wait_search' },
      { source: 'step_wait_search', target: 'step_type_keyword' },
      { source: 'step_type_keyword', target: 'step_done' }
    ],
    assertions: [
      { type: 'step_exists', label: '搜索输入步骤必须存在', stepId: 'step_type_keyword' }
    ]
  }
}

function fillExample() {
  form.name = '百度搜索示例流程'
  form.payloadJson = JSON.stringify(buildBaiduPayload(), null, 2)
}

function fillTikTokExample() {
  form.name = 'TikTok Mock 随机浏览点赞评论'
  form.payloadJson = JSON.stringify({
    isolationGate: {
      enforce: true,
      requireRecentCheckMinutes: 120
    },
    steps: [
      {
        id: 'tiktok_session',
        type: 'tiktok_mock_session',
        data: {
          label: '执行 TikTok Mock 自动化会话',
          baseUrl: tiktokPlan.baseUrl,
          username: tiktokPlan.username,
          password: tiktokPlan.password,
          minVideos: tiktokPlan.minVideos,
          maxVideos: tiktokPlan.maxVideos,
          minWatchMs: tiktokPlan.minWatchMs,
          maxWatchMs: tiktokPlan.maxWatchMs,
          minLikes: tiktokPlan.minLikes,
          maxLikes: tiktokPlan.maxLikes,
          minComments: tiktokPlan.minComments,
          maxComments: tiktokPlan.maxComments,
          behaviorProfile: tiktokPlan.behaviorProfile,
          commentProvider: tiktokPlan.commentProvider,
          watchPattern: 'engaged',
          commentStyle: 'friendly',
          typingMinDelayMs: 35,
          typingMaxDelayMs: 170,
          typingTypoRate: 0.025,
          typingBackspaceRate: 0.02,
          commentCooldownMinMs: 2200,
          commentCooldownMaxMs: 7200,
          likeByKeywords: ['教程', '经验', '技巧'],
          commentByKeywords: ['观点', '案例', '经验']
        }
      },
      { id: 'step_done', type: 'end_success', data: { label: '完成' } }
    ],
    edges: [{ source: 'tiktok_session', target: 'step_done' }],
    assertions: [
      { type: 'number_range', label: '浏览视频数量符合范围', sourcePath: 'tiktok_session.watchedVideos', min: tiktokPlan.minVideos, max: tiktokPlan.maxVideos },
      { type: 'number_range', label: '点赞数量符合范围', sourcePath: 'tiktok_session.likedVideos', min: tiktokPlan.minLikes, max: tiktokPlan.maxLikes },
      { type: 'number_range', label: '评论数量符合范围', sourcePath: 'tiktok_session.commentedVideos', min: tiktokPlan.minComments, max: tiktokPlan.maxComments }
    ]
  }, null, 2)
}

function fillFacebookExample() {
  form.name = 'Facebook 搜索示例流程'
  form.payloadJson = JSON.stringify(buildFacebookPayload(), null, 2)
}

function applyTemplate() {
  if (selectedTemplate.value === 'tiktok') {
    fillTikTokExample()
    return
  }
  if (selectedTemplate.value === 'facebook') {
    fillFacebookExample()
    return
  }
  fillExample()
}

function syncPayloadFromForm() {
  applyTemplate()
  message.value = '已从配置表单更新 PayloadJson。'
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
      payloadJson: form.payloadJson,
      timeoutSeconds: 300,
      retryPolicyJson: '{"maxRetries":1}'
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

async function replay(runId) {
  try {
    const res = await api.replayRun(runId)
    message.value = `已创建重跑任务，run #${res.replayRunId}`
    await load()
  } catch (err) {
    message.value = err.message || '重跑失败'
  }
}

onMounted(async () => {
  applyTemplate()
  await load()
  timer = setInterval(load, 5000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.modal-mask {
  position: fixed;
  inset: 0;
  background: rgba(2, 6, 23, 0.72);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 40;
}
.modal-panel {
  width: min(980px, 96vw);
  max-height: 88vh;
  overflow: auto;
}
</style>
