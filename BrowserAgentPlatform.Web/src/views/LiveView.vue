<template>
  <div class="grid" style="grid-template-columns:420px 1fr;">
    <div class="card">
      <h3>运行日志</h3>
      <div v-for="log in detail.logs" :key="log.id" style="padding:8px 0;border-bottom:1px solid #1f2937;">
        [{{ log.level }}] {{ log.message }}
      </div>
    </div>
    <div class="card">
      <h3>实时预览</h3>
      <div v-if="detail.run">
        <div>状态：{{ detail.run.status }}</div>
        <div>步骤：{{ detail.run.currentStepLabel }}</div>
        <div>URL：{{ detail.run.currentUrl }}</div>
      </div>
      <img v-if="detail.run?.lastPreviewPath" :src="apiBase + detail.run.lastPreviewPath" style="max-width:100%;border-radius:12px;border:1px solid #334155;margin-top:12px;" />
    </div>
  </div>
</template>
<script setup>
import { reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { api } from '../services/api'
import { auth } from '../services/auth'
const route = useRoute()
const apiBase = 'http://localhost:5216'
const detail = reactive({ run:null, logs:[], artifacts:[] })
onMounted(async ()=>{
  if(!route.params.runId) return
  Object.assign(detail, await api.runDetail(route.params.runId))
  const connection = new HubConnectionBuilder()
    .withUrl(`${apiBase}/hubs/live?access_token=${auth.token()}`)
    .withAutomaticReconnect()
    .build()
  connection.on('runUpdate', async () => Object.assign(detail, await api.runDetail(route.params.runId)))
  await connection.start()
  await connection.invoke('JoinRun', Number(route.params.runId))
})
</script>
