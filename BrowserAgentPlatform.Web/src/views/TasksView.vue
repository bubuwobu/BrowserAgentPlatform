<template>
  <div class="grid" style="grid-template-columns:1fr 1fr;">
    <div class="card">
      <h3>任务</h3>
      <div v-for="item in tasks" :key="item.id" class="card" style="margin-bottom:10px;background:#020617;">
        <div>{{ item.name }} #{{ item.id }}</div>
        <div style="font-size:12px;color:#94a3b8;">profile={{ item.browserProfileId }} strategy={{ item.schedulingStrategy }} status={{ item.status }}</div>
      </div>
    </div>
    <div class="card">
      <h3>运行记录</h3>
      <div v-for="run in runs" :key="run.id" class="card" style="margin-bottom:10px;background:#020617;">
        <div style="display:flex;justify-content:space-between;">
          <div>#{{ run.id }} {{ run.status }} - {{ run.currentStepLabel }}</div>
          <RouterLink :to="'/live/'+run.id" class="btn">查看 Live</RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { api } from '../services/api'
const tasks = ref([])
const runs = ref([])
async function load(){ tasks.value = await api.tasks(); runs.value = await api.runs() }
onMounted(load)
</script>
