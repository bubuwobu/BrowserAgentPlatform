<template>
  <div class="grid" style="grid-template-columns:340px 1fr;">
    <div class="card grid">
      <h3>保存模板</h3>
      <input v-model="name" placeholder="模板名称" />
      <textarea v-model="definitionJson" rows="12"></textarea>
      <button class="btn" @click="save">保存模板</button>
    </div>
    <div class="card">
      <h3>模板列表</h3>
      <div v-for="item in items" :key="item.id" class="card" style="margin-bottom:12px;background:#020617;">
        <div>{{ item.name }}</div>
        <pre style="white-space:pre-wrap;">{{ item.definitionJson }}</pre>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { api } from '../services/api'
const items = ref([])
const name = ref('示例模板')
const definitionJson = ref('{"steps":[],"edges":[]}')
async function load(){ items.value = await api.templates() }
async function save(){ await api.createTemplate({ name:name.value, definitionJson:definitionJson.value }); await load() }
onMounted(load)
</script>
