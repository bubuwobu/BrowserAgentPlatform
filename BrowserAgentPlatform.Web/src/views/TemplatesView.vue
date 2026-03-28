<template>
  <div>
    <div class="page-title">模板中心</div>
    <div class="page-subtitle">页面默认只展示模板列表；新增模板通过按钮弹窗完成。</div>

    <div class="card" style="margin-bottom:12px;">
      <div class="section-actions">
        <button class="btn" @click="openCreate">新增模板</button>
        <button class="btn secondary" @click="load">刷新</button>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div class="card">
      <div style="font-weight:700;margin-bottom:10px;">模板列表</div>
      <div v-if="!items.length" class="muted">暂无模板</div>
      <div v-for="item in items" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
        <div style="font-weight:700;">{{ item.name }}</div>
        <pre style="white-space:pre-wrap;margin-top:8px;">{{ item.definitionJson }}</pre>
      </div>
    </div>

    <div v-if="createOpen" class="modal-mask" @click.self="createOpen = false">
      <div class="modal-panel card">
        <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
          <div style="font-weight:700;">新增模板</div>
          <button class="btn secondary" @click="createOpen = false">关闭</button>
        </div>

        <div class="grid" style="margin-top:10px;">
          <input class="input" v-model="name" placeholder="模板名称" />
          <textarea class="input" v-model="definitionJson" rows="12" placeholder='{"steps":[],"edges":[]}'></textarea>
        </div>

        <div class="section-actions" style="margin-top:10px;">
          <button class="btn" @click="save">保存模板</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { api } from '../services/api'

const items = ref([])
const message = ref('')
const createOpen = ref(false)
const name = ref('示例模板')
const definitionJson = ref('{"steps":[],"edges":[]}')

async function load() {
  items.value = await api.templates()
}

function openCreate() {
  createOpen.value = true
  message.value = ''
}

async function save() {
  try {
    await api.createTemplate({ name: name.value, definitionJson: definitionJson.value })
    message.value = '模板保存成功'
    createOpen.value = false
    await load()
  } catch (err) {
    message.value = err.message || '模板保存失败'
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
