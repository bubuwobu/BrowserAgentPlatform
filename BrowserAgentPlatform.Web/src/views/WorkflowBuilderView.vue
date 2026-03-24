<template>
  <div class="grid" style="grid-template-columns:220px 1fr 320px;">
    <div class="card">
      <h3>节点库</h3>
      <div v-for="t in nodeTypes" :key="t" style="margin-bottom:8px;">
        <button class="btn" style="width:100%;" @click="addNode(t)">{{ t }}</button>
      </div>
    </div>
    <div class="card" style="position:relative;height:720px;overflow:hidden;">
      <svg style="position:absolute;inset:0;width:100%;height:100%;">
        <line v-for="e in edges" :key="e.id" :x1="findNode(e.source).x+90" :y1="findNode(e.source).y+24" :x2="findNode(e.target).x+90" :y2="findNode(e.target).y+24" stroke="#60a5fa" stroke-width="2" />
      </svg>
      <div
        v-for="n in nodes"
        :key="n.id"
        @mousedown="startDrag($event, n)"
        @click="selected = n"
        :style="{position:'absolute',left:n.x+'px',top:n.y+'px',width:'180px',padding:'10px',borderRadius:'12px',border:selected?.id===n.id?'2px solid #60a5fa':'1px solid #374151',background:'#0f172a',cursor:'move'}"
      >
        <div style="font-weight:700">{{ n.type }}</div>
        <div style="font-size:12px;color:#94a3b8">{{ n.data.label || n.id }}</div>
      </div>
    </div>
    <div class="card grid">
      <h3>编辑器</h3>
      <div v-if="selected">
        <input v-model="selected.data.label" placeholder="label" />
        <textarea v-model="selected.dataText" rows="14" @change="syncJson(selected)"></textarea>
      </div>
      <div class="grid" style="grid-template-columns:1fr 1fr;">
        <select v-model="edge.source">
          <option v-for="n in nodes" :key="n.id" :value="n.id">{{ n.id }}</option>
        </select>
        <select v-model="edge.target">
          <option v-for="n in nodes" :key="n.id" :value="n.id">{{ n.id }}</option>
        </select>
      </div>
      <input v-model="edge.sourceHandle" placeholder="sourceHandle true/false/loop/done" />
      <button class="btn" @click="addEdge">连接节点</button>
      <input v-model="task.name" placeholder="任务名称" />
      <input v-model.number="task.browserProfileId" placeholder="BrowserProfileId" />
      <select v-model="task.schedulingStrategy">
        <option value="profile_owner">profile_owner</option>
        <option value="preferred_agent">preferred_agent</option>
        <option value="least_loaded">least_loaded</option>
      </select>
      <input v-model.number="task.preferredAgentId" placeholder="PreferredAgentId" />
      <div style="display:flex;gap:8px;">
        <button class="btn" @click="saveTask">保存任务</button>
        <button class="btn" @click="saveTemplate">保存模板</button>
      </div>
    </div>
  </div>
</template>
<script setup>
import { reactive, ref } from 'vue'
import { api } from '../services/api'
const nodeTypes = ['open','click','type','wait_for_element','wait_for_timeout','hover','select','upload_file','scroll','extract_text','execute_js','if_text_contains','branch','loop','end_success','end_fail']
const nodes = ref([])
const edges = ref([])
const selected = ref(null)
const task = reactive({ name:'', browserProfileId:1, schedulingStrategy:'profile_owner', preferredAgentId:null })
const edge = reactive({ source:'', target:'', sourceHandle:'' })
let seq = 1
function addNode(type){
  const n = { id:'n'+seq++, type, x:50+nodes.value.length*24, y:50+nodes.value.length*24, data:{ label:type }, dataText:'{}' }
  if(type==='open') { n.data = { label:'打开页面', url:'https://example.com' }; n.dataText = JSON.stringify(n.data,null,2) }
  nodes.value.push(n); selected.value=n
}
function addEdge(){ if(edge.source && edge.target) edges.value.push({ id:'e'+Math.random(), ...edge }) }
function syncJson(n){ try{ n.data = JSON.parse(n.dataText) }catch{} }
function findNode(id){ return nodes.value.find(x=>x.id===id) || {x:0,y:0} }
let dragging = null
function startDrag(evt,n){
  dragging = { n, ox:evt.clientX-n.x, oy:evt.clientY-n.y }
  window.onmousemove = (e)=>{ if(dragging){ n.x=e.clientX-dragging.ox-250; n.y=e.clientY-dragging.oy-24 } }
  window.onmouseup = ()=>{ dragging=null; window.onmousemove=null; window.onmouseup=null }
}
function payload(){
  return {
    steps: nodes.value.map(n => ({ id:n.id, type:n.type, data:n.data })),
    edges: edges.value,
    startupArgsJson:'[]'
  }
}
async function saveTask(){
  await api.createTask({ ...task, payloadJson: JSON.stringify(payload()), priority:100 })
  alert('任务已保存')
}
async function saveTemplate(){
  await api.createTemplate({ name: task.name || '未命名模板', definitionJson: JSON.stringify(payload(), null, 2) })
  alert('模板已保存')
}
</script>
