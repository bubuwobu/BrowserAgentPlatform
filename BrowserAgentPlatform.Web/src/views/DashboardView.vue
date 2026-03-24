<template>
  <div class="grid" style="grid-template-columns:repeat(4,1fr);">
    <div class="card"><div>Agents</div><h1>{{ summary.agents || 0 }}</h1></div>
    <div class="card"><div>Profiles</div><h1>{{ summary.profiles || 0 }}</h1></div>
    <div class="card"><div>Queued</div><h1>{{ summary.queued || 0 }}</h1></div>
    <div class="card"><div>Running</div><h1>{{ summary.running || 0 }}</h1></div>
  </div>
</template>
<script setup>
import { onMounted, reactive } from 'vue'
import { api } from '../services/api'
const summary = reactive({})
onMounted(async ()=> Object.assign(summary, await api.summary()))
</script>
