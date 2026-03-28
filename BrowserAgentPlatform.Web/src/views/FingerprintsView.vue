<template>
  <div>
    <div class="page-title">指纹模板</div>
    <div class="page-subtitle">默认只显示模板列表；新增模板通过弹窗完成。</div>

    <div class="card" style="margin-bottom:12px;">
      <div class="section-actions">
        <button class="btn" @click="openCreate">新增指纹模板</button>
        <button class="btn secondary" @click="load">刷新</button>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div class="card">
      <div style="font-weight:700;margin-bottom:10px;">模板列表</div>
      <div v-if="!items.length" class="muted">暂无模板</div>
      <div v-for="item in items" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
        <div style="font-weight:700;">{{ item.name }}</div>
        <pre style="white-space:pre-wrap;margin-top:8px;">{{ item.configJson }}</pre>
      </div>
    </div>

    <div v-if="createOpen" class="modal-mask" @click.self="createOpen = false">
      <div class="modal-panel card">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
          <div style="font-weight:700;">新建指纹模板</div>
          <button class="btn secondary" @click="createOpen = false">关闭</button>
        </div>

        <div class="grid" style="margin-top:10px;">
          <input class="input" v-model="form.name" placeholder="模板名称" />
          <textarea class="input" v-model="form.configJson" rows="12"></textarea>
        </div>

        <div class="section-actions" style="margin-top:10px;">
          <button class="btn" @click="save">保存</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { api } from '../services/api'

const items = ref([])
const message = ref('')
const createOpen = ref(false)
const form = reactive({
  name: '默认桌面',
  configJson: '{\n  "userAgent":"Mozilla/5.0",\n  "viewport":{"width":1366,"height":768},\n  "locale":"zh-CN",\n  "timezoneId":"Asia/Singapore"\n}'
})

async function load() {
  items.value = await api.fingerprints()
}

function openCreate() {
  createOpen.value = true
  message.value = ''
}

async function save() {
  try {
    await api.createFingerprint(form)
    message.value = '指纹模板保存成功'
    createOpen.value = false
    await load()
  } catch (err) {
    message.value = err.message || '保存失败'
  }
}

onMounted(load)
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
  width: min(760px, 92vw);
  max-height: 88vh;
  overflow: auto;
}
</style>
