<template>
  <div class="grid" style="grid-template-columns:360px 1fr;">
    <div class="card grid">
      <h3>新建指纹模板</h3>
      <input v-model="form.name" placeholder="模板名称" />
      <textarea v-model="form.configJson" rows="12"></textarea>
      <button class="btn" @click="save">保存</button>
    </div>
    <div class="card">
      <h3>模板列表</h3>
      <pre v-for="item in items" :key="item.id" style="white-space:pre-wrap;background:#020617;padding:12px;border-radius:10px;">{{ item.name }}
{{ item.configJson }}</pre>
    </div>
  </div>
</template>
<script setup>
import { reactive, ref, onMounted } from 'vue'
import { api } from '../services/api'
const items = ref([])
const form = reactive({ name:'默认桌面', configJson: '{\n  "userAgent":"Mozilla/5.0",\n  "viewport":{"width":1366,"height":768},\n  "locale":"zh-CN",\n  "timezoneId":"Asia/Singapore"\n}' })
async function load(){ items.value = await api.fingerprints() }
async function save(){ await api.createFingerprint(form); await load() }
onMounted(load)
</script>
