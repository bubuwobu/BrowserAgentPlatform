<template>
  <div>
    <div class="page-title">账号中心</div>
    <div class="page-subtitle">管理任务账号，并绑定到固定的 BrowserProfile。</div>

    <div class="card" style="margin-bottom:16px;">
      <div class="toolbar">
        <div>
          <div style="font-weight:700;">账号操作区</div>
          <div class="muted">新增与编辑统一在弹窗中处理。</div>
        </div>
        <div class="section-actions">
          <button class="btn" @click="openCreate">新增账号</button>
          <button class="btn secondary" @click="load">刷新</button>
        </div>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div class="card">
      <div style="font-weight:700;margin-bottom:12px;">账号列表</div>
      <div v-if="!items.length" class="muted">暂无账号</div>

      <div v-for="item in items" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
        <div class="toolbar">
          <div>
            <div style="font-weight:700;">{{ item.name }} #{{ item.id }}</div>
            <div class="muted">platform={{ item.platform }} / username={{ item.username }}</div>
            <div class="muted">profile={{ item.browserProfileId || '-' }}</div>
          </div>
          <div class="section-actions">
            <span class="badge" :class="item.status === 'active' ? 'active' : 'disabled'">{{ item.status }}</span>
            <button class="btn secondary" @click="openEdit(item)">编辑</button>
            <button class="btn warn" @click="askDelete(item.id)">删除</button>
          </div>
        </div>
      </div>
    </div>

    <ConfirmDialog
      :open="deleteOpen"
      title="删除账号"
      message="删除后，这个账号将无法再被任务绑定。"
      confirm-text="确认删除"
      @cancel="deleteOpen = false"
      @confirm="removeConfirmed"
    />

    <div v-if="editorOpen" class="modal-mask">
      <div class="modal-panel card">
        <div class="toolbar">
          <div style="font-weight:700;">{{ editingId ? '编辑账号' : '新增账号' }}</div>
          <button class="btn secondary" @click="editorOpen = false">关闭</button>
        </div>

        <div class="grid" style="grid-template-columns:1fr 1fr; gap:12px; margin-top:12px;">
          <FormField label="账号名称" :required="true" help="用于任务选择和管理识别。">
            <input class="input" v-model="form.name" placeholder="例如：账号1" />
          </FormField>

          <FormField label="平台" help="例如：facebook / tiktok / generic">
            <input class="input" v-model="form.platform" placeholder="generic" />
          </FormField>

          <FormField label="用户名" help="登录用户名、昵称或展示名。">
            <input class="input" v-model="form.username" placeholder="登录用户名或展示名" />
          </FormField>

          <FormField label="状态">
            <select class="input" v-model="form.status">
              <option value="active">active</option>
              <option value="disabled">disabled</option>
            </select>
          </FormField>

          <FormField label="绑定 BrowserProfile" help="任务绑定账号后，可自动带出固定的浏览器身份。">
            <select class="input" v-model="form.browserProfileId">
              <option :value="null">不绑定</option>
              <option v-for="p in profiles" :key="p.id" :value="p.id">{{ p.id }} - {{ p.name }}</option>
            </select>
          </FormField>

          <FormField label="凭证 JSON" help="例如 Cookie、Token 或账号补充资料。">
            <textarea class="input" v-model="form.credentialJson" rows="5" placeholder='{"cookie":"..."}'></textarea>
          </FormField>
        </div>

        <FormField label="附加信息 JSON" help="可选。用于保存标签、备注等。">
          <textarea class="input" v-model="form.metadataJson" rows="5" placeholder='{"note":"..."}'></textarea>
        </FormField>

        <div class="section-actions" style="margin-top:12px;">
          <button class="btn" @click="save">{{ editingId ? '保存修改' : '创建账号' }}</button>
          <button class="btn secondary" @click="resetForm">重置</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { api } from '../services/api'
import FormField from '../components/FormField.vue'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const items = ref([])
const profiles = ref([])
const message = ref('')
const editorOpen = ref(false)
const editingId = ref(null)
const deleteOpen = ref(false)
const deleteId = ref(null)

const form = reactive({
  name: '',
  platform: 'generic',
  username: '',
  status: 'active',
  browserProfileId: null,
  credentialJson: '{}',
  metadataJson: '{}'
})

function resetForm() {
  editingId.value = null
  Object.assign(form, {
    name: '',
    platform: 'generic',
    username: '',
    status: 'active',
    browserProfileId: null,
    credentialJson: '{}',
    metadataJson: '{}'
  })
}

function openCreate() {
  resetForm()
  editorOpen.value = true
}

function openEdit(item) {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name || '',
    platform: item.platform || 'generic',
    username: item.username || '',
    status: item.status || 'active',
    browserProfileId: item.browserProfileId,
    credentialJson: item.credentialJson || '{}',
    metadataJson: item.metadataJson || '{}'
  })
  editorOpen.value = true
}

function askDelete(id) {
  deleteId.value = id
  deleteOpen.value = true
}

async function load() {
  const [accountList, profileList] = await Promise.all([api.accounts(), api.profiles()])
  items.value = accountList
  profiles.value = profileList
}

async function save() {
  try {
    const body = { ...form }
    if (editingId.value) {
      await api.updateAccount(editingId.value, body)
      message.value = '账号已更新'
    } else {
      await api.createAccount(body)
      message.value = '账号已创建'
    }
    editorOpen.value = false
    await load()
  } catch (err) {
    message.value = err.message || '保存失败'
  }
}

async function removeConfirmed() {
  try {
    if (deleteId.value) {
      await api.deleteAccount(deleteId.value)
      message.value = `账号 #${deleteId.value} 已删除`
      deleteOpen.value = false
      deleteId.value = null
      await load()
    }
  } catch (err) {
    message.value = err.message || '删除失败'
  }
}

onMounted(load)
</script>
