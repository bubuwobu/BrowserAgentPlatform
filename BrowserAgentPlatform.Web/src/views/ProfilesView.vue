<template>
  <div>
    <div class="page-title">Profiles</div>
    <div class="page-subtitle">统一查看和维护 lifecycle / workspace / runtime 状态。</div>

    <div class="card" style="margin-bottom:12px;">
      <div class="section-actions">
        <button class="btn" @click="openCreate">新增 Profile</button>
        <button class="btn secondary" @click="load">刷新</button>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
      <div class="section-actions" style="margin-top:10px; flex-wrap:wrap;">
        <span v-for="item in board.byLifecycle || []" :key="item.lifecycle" class="chip">{{ item.lifecycle }}: {{ item.count }}</span>
      </div>
    </div>

    <div class="card">
      <div style="font-weight:700;margin-bottom:12px;">Profile 状态面板</div>
      <div v-if="!items.length" class="muted">暂无 Profile</div>
      <div v-for="item in items" :key="item.id" style="margin-bottom:12px;">
        <ProfileStatePanel :item="item" />
        <div class="section-actions" style="margin-top:8px; justify-content:flex-end;">
          <button class="btn secondary" @click="openEdit(item)">编辑</button>
          <button class="btn secondary" @click="isolationCheck(item.id)">隔离检查</button>
          <button class="btn" @click="testOpen(item.id)">测试打开</button>
          <button class="btn warn" @click="remove(item.id)">删除</button>
        </div>
      </div>
    </div>

    <div v-if="editorOpen" class="modal-mask" @click.self="editorOpen = false">
      <div class="modal-panel card">
        <div class="toolbar">
          <div style="font-weight:700;">{{ editingId ? '编辑 Profile' : '新增 Profile' }}</div>
          <button class="btn secondary" @click="editorOpen = false">关闭</button>
        </div>

        <div class="grid" style="grid-template-columns:1fr 1fr; gap:12px; margin-top:12px;">
          <div class="form-field"><label>名称</label><input class="input" v-model="form.name" /></div>
          <div class="form-field"><label>Owner Agent</label><select class="input" v-model="form.ownerAgentId"><option :value="null">不绑定</option><option v-for="item in agents" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}</option></select></div>
          <div class="form-field"><label>代理</label><select class="input" v-model="form.proxyId"><option :value="null">无代理</option><option v-for="item in proxies" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}</option></select></div>
          <div class="form-field"><label>指纹模板</label><select class="input" v-model="form.fingerprintTemplateId"><option :value="null">无指纹模板</option><option v-for="item in fingerprints" :key="item.id" :value="item.id">{{ item.id }} - {{ item.name }}</option></select></div>
          <div class="form-field"><label>本地路径</label><input class="input" v-model="form.localProfilePath" /></div>
          <div class="form-field"><label>隔离级别</label><select class="input" v-model="form.isolationLevel"><option value="strict">strict</option><option value="standard">standard</option><option value="relaxed">relaxed</option></select></div>
          <div class="form-field"><label>Workspace Key</label><input class="input" v-model="form.workspaceKey" /></div>
          <div class="form-field"><label>Lifecycle State</label><select class="input" v-model="form.lifecycleState"><option value="created">created</option><option value="ready">ready</option><option value="leased">leased</option><option value="running">running</option><option value="cooling_down">cooling_down</option><option value="broken">broken</option></select></div>
        </div>

        <div class="form-field" style="margin-top:12px;"><label>存储根路径</label><input class="input" v-model="form.storageRootPath" /></div>
        <div class="form-field" style="margin-top:12px;"><label>下载根路径</label><input class="input" v-model="form.downloadRootPath" /></div>
        <div class="form-field" style="margin-top:12px;"><label>Profile Root Path</label><input class="input" v-model="form.profileRootPath" /></div>
        <div class="form-field" style="margin-top:12px;"><label>Artifact Root Path</label><input class="input" v-model="form.artifactRootPath" /></div>
        <div class="form-field" style="margin-top:12px;"><label>Temp Root Path</label><input class="input" v-model="form.tempRootPath" /></div>
        <div class="form-field" style="margin-top:12px;"><label>启动参数 JSON</label><textarea class="input" v-model="form.startupArgsJson" rows="5"></textarea></div>
        <div class="form-field" style="margin-top:12px;"><label>隔离策略 JSON</label><textarea class="input" v-model="form.isolationPolicyJson" rows="6"></textarea></div>

        <div class="section-actions" style="margin-top:12px;"><button class="btn" @click="save">{{ editingId ? '保存修改' : '创建 Profile' }}</button></div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { reactive, ref, onMounted } from 'vue'
import ProfileStatePanel from '../components/ProfileStatePanel.vue'
import { api } from '../services/api'
const items = ref([]), agents = ref([]), proxies = ref([]), fingerprints = ref([]), message = ref(''), editorOpen = ref(false), editingId = ref(null), board = reactive({ byLifecycle: [] })
const form = reactive({ name:'', ownerAgentId:null, proxyId:null, fingerprintTemplateId:null, localProfilePath:'', storageRootPath:'', downloadRootPath:'', startupArgsJson:'[]', isolationPolicyJson:'{}', isolationLevel:'strict', workspaceKey:'', profileRootPath:'', artifactRootPath:'', tempRootPath:'', lifecycleState:'created' })
function resetForm(){ editingId.value=null; Object.assign(form,{ name:'', ownerAgentId:null, proxyId:null, fingerprintTemplateId:null, localProfilePath:'', storageRootPath:'', downloadRootPath:'', startupArgsJson:'[]', isolationPolicyJson:'{}', isolationLevel:'strict', workspaceKey:'', profileRootPath:'', artifactRootPath:'', tempRootPath:'', lifecycleState:'created' }) }
function openCreate(){ resetForm(); editorOpen.value=true }
function openEdit(item){ editingId.value=item.id; Object.assign(form,{ name:item.name||'', ownerAgentId:item.ownerAgentId, proxyId:item.proxyId, fingerprintTemplateId:item.fingerprintTemplateId, localProfilePath:item.localProfilePath||'', storageRootPath:item.storageRootPath||'', downloadRootPath:item.downloadRootPath||'', startupArgsJson:item.startupArgsJson||'[]', isolationPolicyJson:item.isolationPolicyJson||'{}', isolationLevel:item.isolationLevel||'strict', workspaceKey:item.workspaceKey||'', profileRootPath:item.profileRootPath||'', artifactRootPath:item.artifactRootPath||'', tempRootPath:item.tempRootPath||'', lifecycleState:item.lifecycleState||'created' }); editorOpen.value=true }
async function load(){ const [p,a,px,f,b] = await Promise.all([api.profiles(),api.agents(),api.proxies(),api.fingerprints(),api.profileStateBoard(20)]); items.value=p; agents.value=a; proxies.value=px; fingerprints.value=f; Object.assign(board, b||{}) }
async function save(){ try{ const body = { ...form }; editingId.value ? await api.updateProfile(editingId.value, body) : await api.createProfile(body); message.value='Profile 保存成功'; editorOpen.value=false; await load() } catch(err){ message.value=err.message || '保存失败' } }
async function remove(id){ try{ await api.deleteProfile(id); message.value='Profile 已删除'; await load() } catch(err){ message.value = err.message || '删除失败' } }
async function testOpen(id){ try{ await api.testOpenProfile(id); message.value='已发送测试打开命令' } catch(err){ message.value=err.message || '失败' } }
async function isolationCheck(id){ try{ const result=await api.profileIsolationCheck(id); message.value=result?.ok?'隔离检查通过':'隔离检查失败'; await load() } catch(err){ message.value=err.message || '检查失败' } }
onMounted(load)
</script>
<style scoped>
.chip { display:inline-flex; align-items:center; padding:6px 10px; border-radius:999px; background:#0f172a; border:1px solid #1e293b; color:#e2e8f0; }
</style>
