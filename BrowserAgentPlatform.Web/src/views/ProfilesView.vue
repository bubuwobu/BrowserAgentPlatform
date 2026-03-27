<template>
  <div>
    <div class="page-title">Profiles</div>
    <div class="page-subtitle">管理浏览器隔离身份、代理、指纹模板，以及测试打开与接管。</div>

    <div class="grid" style="grid-template-columns:380px 1fr;">
      <div class="card grid">
        <div style="font-weight:700;">新建 Profile</div>

        <input class="input" v-model="form.name" placeholder="名称" />

        <select class="input" v-model="form.ownerAgentId">
          <option :value="null">选择 Agent（推荐在线 Agent）</option>
          <option v-for="item in agentOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name || 'Unnamed Agent' }}（{{ item.status }}）
          </option>
        </select>

        <select class="input" v-model="form.proxyId">
          <option :value="null">无代理</option>
          <option v-for="item in proxyOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name || item.host }}
          </option>
        </select>

        <select class="input" v-model="form.fingerprintTemplateId">
          <option :value="null">无指纹模板</option>
          <option v-for="item in fingerprintOptions" :key="item.id" :value="item.id">
            {{ item.id }} - {{ item.name }}
          </option>
        </select>

        <input class="input" v-model="form.localProfilePath" placeholder="本地路径（可选）" />
        <input class="input" v-model="form.storageRootPath" placeholder="存储根路径（可选，建议绝对路径）" />
        <input class="input" v-model="form.downloadRootPath" placeholder="下载根路径（可选，建议绝对路径）" />
        <select class="input" v-model="form.isolationLevel">
          <option value="strict">strict</option>
          <option value="standard">standard</option>
          <option value="relaxed">relaxed</option>
        </select>
        <textarea class="input" v-model="form.startupArgsJson" rows="6" placeholder='["--start-maximized"]'></textarea>
        <textarea class="input" v-model="form.isolationPolicyJson" rows="8" placeholder='{"timezone":"Asia/Shanghai","locale":"zh-CN"}'></textarea>

        <div class="section-actions">
          <button class="btn" @click="save" :disabled="saving">{{ saving ? '保存中...' : '保存' }}</button>
          <button class="btn secondary" @click="resetForm">重置</button>
          <button class="btn secondary" @click="load">刷新</button>
        </div>

        <div v-if="message" class="muted">{{ message }}</div>
      </div>

      <div class="card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <div>
            <div style="font-weight:700;">Profile 列表</div>
            <div class="muted">从这里应该能直观看到隔离浏览器的状态和动作。</div>
          </div>
        </div>

        <div v-if="!items.length" class="muted">暂无 Profile</div>

        <div v-for="item in items" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
          <div style="display:flex;justify-content:space-between;gap:12px;align-items:flex-start;">
            <div style="flex:1;">
              <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">
                <div style="font-weight:700;">{{ item.name }} #{{ item.id }}</div>
                <span class="badge" :class="item.status">{{ item.status }}</span>
              </div>

              <div class="muted" style="margin-top:8px;">ownerAgentId={{ item.ownerAgentId || '-' }}</div>
              <div class="muted">proxyId={{ item.proxyId || '-' }} / fingerprintTemplateId={{ item.fingerprintTemplateId || '-' }}</div>
              <div class="muted">localProfilePath={{ item.localProfilePath || '-' }}</div>
              <div class="muted">storageRootPath={{ item.storageRootPath || '-' }} / downloadRootPath={{ item.downloadRootPath || '-' }}</div>
              <div class="muted">isolationLevel={{ item.isolationLevel || '-' }} / lastIsolationCheckAt={{ formatTime(item.lastIsolationCheckAt) }}</div>
              <div class="muted">lastUsedAt={{ formatTime(item.lastUsedAt) }}</div>
            </div>

            <div style="display:grid;gap:8px;min-width:120px;">
              <button class="btn secondary" @click="isolationCheck(item.id)">隔离检查</button>
              <button class="btn" @click="testOpen(item.id)">测试打开</button>
              <button class="btn success" @click="takeover(item.id, true)">开始接管</button>
              <button class="btn warn" @click="takeover(item.id, false)">结束接管</button>
              <button class="btn secondary" @click="unlock(item.id)">强制解锁</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted, onBeforeUnmount, computed } from 'vue'
import { api } from '../services/api'

const items = ref([])
const agents = ref([])
const proxies = ref([])
const fingerprints = ref([])
const saving = ref(false)
const message = ref('')
let timer = null

const form = reactive({
  name: '',
  ownerAgentId: null,
  proxyId: null,
  fingerprintTemplateId: null,
  localProfilePath: '',
  storageRootPath: '',
  downloadRootPath: '',
  startupArgsJson: '[]',
  isolationPolicyJson: '{}',
  isolationLevel: 'strict'
})

const agentOptions = computed(() => {
  return [...agents.value].sort((a, b) => {
    if (a.status === 'online' && b.status !== 'online') return -1
    if (a.status !== 'online' && b.status === 'online') return 1
    return a.id - b.id
  })
})
const proxyOptions = computed(() => proxies.value)
const fingerprintOptions = computed(() => fingerprints.value)

function resetForm() {
  Object.assign(form, {
    name: '',
    ownerAgentId: null,
    proxyId: null,
    fingerprintTemplateId: null,
    localProfilePath: '',
    storageRootPath: '',
    downloadRootPath: '',
    startupArgsJson: '[]',
    isolationPolicyJson: '{}',
    isolationLevel: 'strict'
  })
}

function formatTime(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString()
}

async function load() {
  const [profileList, agentList, proxyList, fingerprintList] = await Promise.all([
    api.profiles(),
    api.agents(),
    api.proxies(),
    api.fingerprints()
  ])
  items.value = profileList
  agents.value = agentList
  proxies.value = proxyList
  fingerprints.value = fingerprintList
}

async function save() {
  saving.value = true
  message.value = ''
  try {
    await api.createProfile(form)
    message.value = 'Profile 已创建'
    resetForm()
    await load()
  } catch (err) {
    message.value = err.message || '创建失败'
  } finally {
    saving.value = false
  }
}

async function testOpen(id) {
  try {
    await api.testOpenProfile(id)
    message.value = `已发送测试打开命令：Profile #${id}`
  } catch (err) {
    message.value = err.message || '测试打开失败'
  }
}

async function takeover(id, headed) {
  try {
    await api.takeover(id, headed)
    message.value = headed
      ? `已发送开始接管命令：Profile #${id}`
      : `已发送结束接管命令：Profile #${id}`
  } catch (err) {
    message.value = err.message || '接管命令发送失败'
  }
}

async function unlock(id) {
  try {
    await api.unlockProfile(id)
    message.value = `已尝试解锁 Profile #${id}`
    await load()
  } catch (err) {
    message.value = err.message || '解锁失败'
  }
}

async function isolationCheck(id) {
  try {
    const result = await api.profileIsolationCheck(id)
    if (result?.ok) {
      const warningText = (result.warnings || []).length ? `，warnings: ${(result.warnings || []).join(' | ')}` : ''
      message.value = `Profile #${id} 隔离检查通过${warningText}`
    } else {
      message.value = `Profile #${id} 隔离检查失败：${(result?.errors || []).join(' | ') || 'unknown'}`
    }
    await load()
  } catch (err) {
    message.value = err.message || '隔离检查失败'
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
