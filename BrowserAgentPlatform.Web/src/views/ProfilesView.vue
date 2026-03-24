<template>
  <div class="grid" style="grid-template-columns:380px 1fr;">
    <div class="card grid">
      <h3>新建 Profile</h3>
      <input v-model="form.name" placeholder="名称" />
      <input v-model.number="form.ownerAgentId" placeholder="绑定 AgentId" />
      <input v-model.number="form.proxyId" placeholder="ProxyId" />
      <input v-model.number="form.fingerprintTemplateId" placeholder="FingerprintTemplateId" />
      <input v-model="form.localProfilePath" placeholder="本地路径（可选）" />
      <textarea v-model="form.startupArgsJson" rows="5"></textarea>
      <button class="btn" @click="save">保存</button>
    </div>
    <div class="card">
      <h3>Profiles</h3>
      <div v-for="item in items" :key="item.id" class="card" style="margin-bottom:12px;background:#020617;">
        <div style="display:flex;justify-content:space-between;gap:12px;align-items:center;">
          <div>
            <div>{{ item.name }} #{{ item.id }}</div>
            <div style="font-size:12px;color:#94a3b8;">owner={{ item.ownerAgentId }} status={{ item.status }}</div>
          </div>
          <div style="display:flex;gap:8px;">
            <button class="btn" @click="testOpen(item.id)">测试打开</button>
            <button class="btn" @click="takeover(item.id,true)">接管</button>
            <button class="btn" @click="takeover(item.id,false)">结束接管</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { reactive, ref, onMounted } from 'vue'
import { api } from '../services/api'
const items = ref([])
const form = reactive({ name:'', ownerAgentId:null, proxyId:null, fingerprintTemplateId:null, localProfilePath:'', startupArgsJson:'[]' })
async function load(){ items.value = await api.profiles() }
async function save(){ await api.createProfile(form); Object.assign(form, { name:'', ownerAgentId:null, proxyId:null, fingerprintTemplateId:null, localProfilePath:'', startupArgsJson:'[]' }); await load() }
async function testOpen(id){ await api.testOpenProfile(id) }
async function takeover(id,headed){ await api.takeover(id, headed) }
onMounted(load)
</script>
