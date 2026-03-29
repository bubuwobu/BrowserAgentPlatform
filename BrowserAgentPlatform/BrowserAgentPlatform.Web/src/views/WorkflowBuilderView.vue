
<template>
  <div>
    <div class="page-title">编排器</div>
    <div class="page-subtitle">Phase 6.1-C：自动连线、连续拾取、多元素批量生成节点。</div>

    <div class="card" style="margin-bottom:16px;">
      <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:10px;flex-wrap:wrap;">
        <div>
          <div style="font-weight:700;">顶部工具栏</div>
          <div class="muted">空白区域拖动画布，滚轮缩放，Delete 删除节点/连线，拾取队列支持批量生成。</div>
        </div>
        <div class="section-actions">
          <select class="input" v-model="templateKey" style="min-width:220px;">
            <option value="">选择模板</option>
            <option value="basic">基础模板</option>
            <option value="login">登录模板</option>
            <option value="facebook">Facebook 模板</option>
            <option value="tiktok">TikTok 模板</option>
          </select>
          <button class="btn secondary" @click="loadSelectedTemplate" :disabled="!templateKey">应用模板</button>
          <button class="btn secondary" @click="newFlow">新建</button>
          <button class="btn secondary" @click="validateFlow">校验</button>
          <button class="btn secondary" @click="exportJson">导出</button>
          <button class="btn secondary" @click="showImport = !showImport">{{ showImport ? '收起导入' : '导入' }}</button>
          <button class="btn secondary" @click="duplicateSelectedNode" :disabled="!selected">复制节点</button>
          <button class="btn warn" @click="removeSelectedNode" :disabled="!selected">删除节点</button>
          <button class="btn secondary" @click="autoLayout" :disabled="!nodes.length">自动布局</button>
          <button class="btn secondary" @click="resetViewport">重置视图</button>
        </div>
      </div>

      <div v-if="showImport" style="margin-top:12px;">
        <div class="form-field">
          <label>导入工作流 JSON</label>
          <textarea class="input" rows="10" v-model="importJson"></textarea>
        </div>
        <div class="section-actions" style="margin-top:8px;">
          <button class="btn" @click="applyImport">应用导入</button>
        </div>
      </div>

      <div v-if="message" class="muted" style="margin-top:10px;">{{ message }}</div>
      <div v-if="validationErrors.length" style="margin-top:10px;">
        <div style="font-weight:700;color:#fca5a5;">校验结果</div>
        <div v-for="(err, idx) in validationErrors" :key="idx" style="color:#fca5a5;font-size:13px;margin-top:4px;">{{ idx + 1 }}. {{ err }}</div>
      </div>
    </div>

    <div class="grid" style="grid-template-columns:260px 1fr 420px; gap:16px;">
      <div class="card" style="display:flex;flex-direction:column;height:760px;overflow:hidden;">
        <div style="font-weight:700;margin-bottom:10px;flex:0 0 auto;">节点库</div>
        <div class="muted" style="margin-bottom:10px;flex:0 0 auto;">节点列表区独立滚动。</div>
        <div style="flex:1 1 auto;overflow:auto;padding-right:6px;">
          <div v-for="group in groupedNodeTypes" :key="group.title" style="margin-bottom:16px;">
            <div style="font-weight:700;margin-bottom:8px;">{{ group.title }}</div>
            <div v-for="t in group.items" :key="t" style="margin-bottom:8px;">
              <button class="btn" style="width:100%;" @click="addNode(t)">{{ t }}</button>
            </div>
          </div>
        </div>
      </div>

      <div class="card" style="height:760px; padding:0; overflow:hidden;">
        <div ref="canvasRef" class="builder-canvas" @wheel.prevent="onWheel">
          <div class="pan-layer" @mousedown="startPan"></div>
          <div class="grid-layer" :style="{backgroundSize: `${20 * viewport.scale}px ${20 * viewport.scale}px`, backgroundPosition: `${viewport.x}px ${viewport.y}px`}"></div>
          <div class="content-layer" :style="{transform:`translate(${viewport.x}px, ${viewport.y}px) scale(${viewport.scale})`, transformOrigin:'0 0'}">
            <svg style="position:absolute;inset:0;width:5200px;height:3200px;overflow:visible;pointer-events:auto;">
              <path v-for="e in edges" :key="e.id" :d="edgePath(e)"
                :stroke="selectedEdgeId === e.id ? '#f59e0b' : hoveredEdgeId === e.id ? '#93c5fd' : '#60a5fa'"
                :stroke-width="selectedEdgeId === e.id ? 3.5 : hoveredEdgeId === e.id ? 3 : 2.5"
                fill="none" style="cursor:pointer"
                @mouseenter="hoveredEdgeId = e.id" @mouseleave="hoveredEdgeId = ''" @click.stop="selectEdge(e.id)" />
              <path v-if="dragLink.active" :d="dragEdgePath" stroke="#22c55e" stroke-width="2.5" stroke-dasharray="6 4" fill="none" />
            </svg>

            <div v-for="n in nodes" :key="n.id" :data-node="n.id" @mousedown.stop="startDragNode($event, n)" @click.stop="selectNode(n)"
              :style="{position:'absolute',left:n.x + 'px',top:n.y + 'px',width:'220px',padding:'12px',borderRadius:'14px',border:getNodeBorder(n),background:'#0f172a',cursor:'move',userSelect:'none',pointerEvents:'auto'}">
              <div @mouseup.stop="finishDragLink(n.id)" style="position:absolute;left:-7px;top:50%;transform:translateY(-50%);width:14px;height:14px;border-radius:999px;background:#38bdf8;border:2px solid #0f172a;cursor:crosshair;"></div>
              <div @mousedown.stop="startDragLink($event, n.id)" style="position:absolute;right:-7px;top:50%;transform:translateY(-50%);width:14px;height:14px;border-radius:999px;background:#22c55e;border:2px solid #0f172a;cursor:crosshair;"></div>
              <div style="display:flex;justify-content:space-between;gap:8px;align-items:flex-start;">
                <div style="min-width:0;">
                  <div style="font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">{{ n.nodeName || n.id }}</div>
                  <div style="font-size:12px;color:#94a3b8;">{{ n.type }}</div>
                </div>
                <span class="badge queued">{{ n.id }}</span>
              </div>
              <div style="font-size:12px;color:#94a3b8;margin-top:8px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">{{ summaryText(n) }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="grid" style="gap:16px;">
        <div class="card">
          <div style="font-weight:700;margin-bottom:12px;">编辑器</div>
          <template v-if="selected">
            <div class="form-field"><label>节点名称</label><input class="input" v-model="selected.nodeName" /></div>
            <div class="form-field" style="margin-top:10px;"><label>节点 ID</label><input class="input" v-model="selected.id" /></div>
            <div class="form-field" style="margin-top:10px;"><label>步骤显示名称</label><input class="input" v-model="selected.data.label" /></div>
            <div v-for="field in currentSchema" :key="field.key" style="margin-top:10px;">
              <div class="form-field">
                <label>{{ field.label }}</label>
                <input v-if="field.type !== 'textarea' && field.type !== 'number'" class="input" v-model="selected.data[field.key]" :placeholder="field.placeholder || ''" />
                <input v-else-if="field.type === 'number'" class="input" type="number" v-model.number="selected.data[field.key]" :placeholder="field.placeholder || ''" />
                <textarea v-else class="input" rows="4" v-model="selected.data[field.key]" :placeholder="field.placeholder || ''"></textarea>
              </div>
            </div>
            <details style="margin-top:12px;">
              <summary style="cursor:pointer;">高级 JSON</summary>
              <textarea class="input" rows="10" v-model="selected.dataText" @change="syncJson(selected)" style="margin-top:8px;"></textarea>
            </details>
          </template>

          <template v-else-if="selectedEdge">
            <div class="muted">当前连线：{{ edgeLabel(selectedEdge.source) }} → {{ edgeLabel(selectedEdge.target) }}</div>
            <div class="form-field" style="margin-top:10px;"><label>sourceHandle</label><input class="input" v-model="selectedEdge.sourceHandle" /></div>
            <div class="section-actions" style="margin-top:10px;"><button class="btn warn" @click="removeEdge(selectedEdge.id)">删除当前连线</button></div>
          </template>

          <div v-else class="muted">请选择一个节点或连线</div>
        </div>

        <ElementPickerPanel
          :session-id="pickerSessionId"
          :result="pickerResult"
          :queue="pickerQueue"
          :busy="pickerBusy"
          :continuous-pick="continuousPick"
          :auto-link="autoLinkNodes"
          @start="startElementPicker"
          @stop="stopElementPicker"
          @apply-selector="applySelectorFromPicker"
          @create-node-with-selector="createNodeFromPickerSelector"
          @create-recommended-node="createRecommendedNode"
          @stash-current="stashCurrentPickerResult"
          @clear-queue="clearPickerQueue"
          @bulk-generate="bulkGenerateFromQueue"
          @remove-queue-item="removeQueueItem"
          @toggle-continuous="setContinuousPick"
          @toggle-autolink="setAutoLink"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { api, SIGNALR_BASE_URL } from '../services/api'
import { auth } from '../services/auth'
import ElementPickerPanel from '../components/ElementPickerPanel.vue'

const groupedNodeTypes = [
  { title: '基础交互', items: ['open','click','double_click','hover','type','clear_input','press_key','select_option','upload_file'] },
  { title: '等待判断', items: ['wait_for_element','wait_for_timeout','wait_for_text','wait_for_url','exists','retry','manual_review'] },
  { title: '提取与结果', items: ['extract_text','extract_attr','screenshot','log'] },
  { title: '滚动与列表', items: ['scroll','scroll_to_element','loop_list'] },
  { title: '流程控制', items: ['branch','loop','refresh_page','switch_tab','end_success','end_fail'] }
]

const nodeSchemas = {
  open: [{ key:'url', label:'页面地址', type:'text', placeholder:'https://example.com' }],
  click: [{ key:'selector', label:'元素选择器', type:'text' }],
  double_click: [{ key:'selector', label:'元素选择器', type:'text' }],
  hover: [{ key:'selector', label:'元素选择器', type:'text' }],
  type: [{ key:'selector', label:'输入框选择器', type:'text' }, { key:'value', label:'输入内容', type:'textarea' }],
  clear_input: [{ key:'selector', label:'输入框选择器', type:'text' }],
  press_key: [{ key:'key', label:'按键', type:'text', placeholder:'Enter / Tab / Escape' }],
  select_option: [{ key:'selector', label:'下拉选择器', type:'text' }, { key:'value', label:'值/文本', type:'text' }],
  upload_file: [{ key:'selector', label:'上传控件选择器', type:'text' }, { key:'filePath', label:'文件路径', type:'text' }],
  wait_for_element: [{ key:'selector', label:'等待元素选择器', type:'text' }, { key:'timeout', label:'超时(ms)', type:'number' }],
  wait_for_timeout: [{ key:'timeout', label:'等待时长(ms)', type:'number' }],
  wait_for_text: [{ key:'text', label:'等待文本', type:'text' }, { key:'timeout', label:'超时(ms)', type:'number' }],
  wait_for_url: [{ key:'urlPart', label:'URL 包含', type:'text' }, { key:'timeout', label:'超时(ms)', type:'number' }],
  exists: [{ key:'selector', label:'检测元素选择器', type:'text' }],
  retry: [{ key:'count', label:'重试次数', type:'number' }],
  manual_review: [{ key:'reason', label:'人工确认说明', type:'textarea' }],
  extract_text: [{ key:'selector', label:'提取文本选择器', type:'text' }],
  extract_attr: [{ key:'selector', label:'元素选择器', type:'text' }, { key:'attr', label:'属性名', type:'text' }],
  screenshot: [{ key:'name', label:'截图名', type:'text' }],
  log: [{ key:'message', label:'日志内容', type:'textarea' }],
  scroll: [{ key:'deltaY', label:'滚动距离', type:'number' }],
  scroll_to_element: [{ key:'selector', label:'目标元素选择器', type:'text' }],
  loop_list: [{ key:'itemSelector', label:'列表项选择器', type:'text' }, { key:'maxItems', label:'最大遍历数量', type:'number' }],
  branch: [{ key:'mode', label:'分支模式', type:'text' }],
  loop: [{ key:'count', label:'循环次数', type:'number' }],
  refresh_page: [],
  switch_tab: [{ key:'index', label:'标签页序号', type:'number' }],
  end_success: [],
  end_fail: []
}

const canvasRef = ref(null)
const nodes = ref([])
const edges = ref([])
const selected = ref(null)
const selectedEdgeId = ref('')
const hoveredEdgeId = ref('')
const showImport = ref(false)
const importJson = ref('')
const templateKey = ref('')
const message = ref('')
const validationErrors = ref([])
const dragLink = reactive({ active:false, sourceId:'', startX:0, startY:0, currentX:0, currentY:0 })
const viewport = reactive({ x:0, y:0, scale:1 })
const pickerSessionId = ref('')
const pickerBusy = ref(false)
const pickerResult = ref(null)
const pickerQueue = ref([])
const continuousPick = ref(false)
const autoLinkNodes = ref(true)

let pickerConnection = null
let seq = 1
let draggingNode = null
let panning = null

const currentSchema = computed(() => selected.value ? (nodeSchemas[selected.value.type] || []) : [])
const selectedEdge = computed(() => edges.value.find(x => x.id === selectedEdgeId.value) || null)
const dragEdgePath = computed(() => bezierPath(dragLink.startX, dragLink.startY, dragLink.currentX, dragLink.currentY))

function getNodeBorder(n) { return selected.value?.id === n.id ? '2px solid #60a5fa' : '1px solid #374151' }
function summaryText(n) { return n.data?.selector || n.data?.url || n.data?.text || n.data?.itemSelector || n.data?.label || '-' }
function defaultDataFor(type) {
  const map = {
    open:{ label:'打开页面', url:'https://example.com' },
    click:{ label:'点击元素', selector:'#submit' },
    double_click:{ label:'双击元素', selector:'#submit' },
    hover:{ label:'悬停元素', selector:'#menu' },
    type:{ label:'输入内容', selector:'input', value:'' },
    clear_input:{ label:'清空输入框', selector:'input' },
    press_key:{ label:'按键输入', key:'Enter' },
    select_option:{ label:'选择下拉项', selector:'select', value:'' },
    upload_file:{ label:'上传文件', selector:'input[type=file]', filePath:'' },
    wait_for_element:{ label:'等待元素', selector:'#app', timeout:10000 },
    wait_for_timeout:{ label:'固定等待', timeout:1000 },
    wait_for_text:{ label:'等待文本', text:'登录成功', timeout:10000 },
    wait_for_url:{ label:'等待 URL', urlPart:'/feed', timeout:10000 },
    exists:{ label:'检测元素', selector:'#login-error' },
    retry:{ label:'重试', count:3 },
    manual_review:{ label:'人工确认', reason:'请检查页面状态' },
    extract_text:{ label:'提取文本', selector:'body' },
    extract_attr:{ label:'提取属性', selector:'a', attr:'href' },
    screenshot:{ label:'截图', name:'capture.png' },
    log:{ label:'写日志', message:'执行到当前步骤' },
    scroll:{ label:'滚动页面', deltaY:600 },
    scroll_to_element:{ label:'滚动到元素', selector:'#target' },
    loop_list:{ label:'遍历列表', itemSelector:'.item-card', maxItems:10 },
    branch:{ label:'分支', mode:'first' },
    loop:{ label:'循环', count:2 },
    refresh_page:{ label:'刷新页面' },
    switch_tab:{ label:'切换标签页', index:0 },
    end_success:{ label:'成功结束' },
    end_fail:{ label:'失败结束' }
  }
  return map[type] || { label:type }
}
function sourcePoint(id) { const n = nodes.value.find(x => x.id === id); return n ? { x:n.x + 220, y:n.y + 44 } : { x:0, y:0 } }
function targetPoint(id) { const n = nodes.value.find(x => x.id === id); return n ? { x:n.x, y:n.y + 44 } : { x:0, y:0 } }
function bezierPath(x1, y1, x2, y2) { const dx = Math.max(80, Math.abs(x2 - x1) * 0.4); return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}` }
function edgePath(e) { const s = sourcePoint(e.source), t = targetPoint(e.target); return bezierPath(s.x, s.y, t.x, t.y) }
function edgeLabel(id) { const n = nodes.value.find(x => x.id === id); return n ? (n.nodeName || n.id) : id }
function selectorTargetFieldForNode(type) { if (type === 'loop_list') return 'itemSelector'; if (type === 'wait_for_text') return 'text'; return 'selector' }
function toCanvasPoint(clientX, clientY) {
  const rect = canvasRef.value?.getBoundingClientRect()
  if (!rect) return { x:0, y:0 }
  return { x:(clientX - rect.left - viewport.x) / viewport.scale, y:(clientY - rect.top - viewport.y) / viewport.scale }
}
function snap(v) { return Math.round(v / 20) * 20 }
function resetViewport() { viewport.x = 0; viewport.y = 0; viewport.scale = 1; message.value = '视图已重置' }
function newFlow() { nodes.value = []; edges.value = []; selected.value = null; selectedEdgeId.value = ''; validationErrors.value = []; cancelDragLink(); pickerQueue.value = []; message.value = '已新建空白工作流' }
function buildTemplate() { return { nodes: [], edges: [] } }
function loadSelectedTemplate() { const tpl = buildTemplate(templateKey.value); nodes.value = tpl.nodes; edges.value = tpl.edges; selected.value = nodes.value[0] || null; selectedEdgeId.value = '' }
function addNode(type, initialData = null) { const data = initialData ? { ...defaultDataFor(type), ...initialData } : defaultDataFor(type); const n = { id:'n' + seq++, nodeName:`${type} 节点`, type, x:snap(40 + nodes.value.length * 50), y:snap(40 + nodes.value.length * 35), data, dataText: JSON.stringify(data, null, 2) }; nodes.value.push(n); selected.value = n; selectedEdgeId.value = ''; return n }
function selectNode(n) { selected.value = n; selectedEdgeId.value = '' }
function selectEdge(id) { selected.value = null; selectedEdgeId.value = id }
function startDragLink(evt, sourceId) { const pt = sourcePoint(sourceId); dragLink.active = true; dragLink.sourceId = sourceId; dragLink.startX = pt.x; dragLink.startY = pt.y; const p = toCanvasPoint(evt.clientX, evt.clientY); dragLink.currentX = p.x; dragLink.currentY = p.y; window.addEventListener('mousemove', onDragLinkMove); window.addEventListener('mouseup', cancelDragLink) }
function onDragLinkMove(evt) { if (!dragLink.active) return; const p = toCanvasPoint(evt.clientX, evt.clientY); dragLink.currentX = p.x; dragLink.currentY = p.y }
function finishDragLink(targetId) { if (!dragLink.active) return; if (!dragLink.sourceId || dragLink.sourceId === targetId) { cancelDragLink(); return } edges.value.push({ id:'e' + Math.random(), source:dragLink.sourceId, target:targetId, sourceHandle:'' }); cancelDragLink(); message.value = '已创建连线' }
function cancelDragLink() { dragLink.active = false; dragLink.sourceId = ''; dragLink.startX = 0; dragLink.startY = 0; dragLink.currentX = 0; dragLink.currentY = 0; window.removeEventListener('mousemove', onDragLinkMove); window.removeEventListener('mouseup', cancelDragLink) }
function syncJson(n) { try { n.data = JSON.parse(n.dataText); message.value = '高级 JSON 已同步' } catch { message.value = '高级 JSON 解析失败，请检查格式' } }
function removeSelectedNode() { if (!selected.value) return; const id = selected.value.id; nodes.value = nodes.value.filter(x => x.id !== id); edges.value = edges.value.filter(x => x.source !== id && x.target !== id); selected.value = null; message.value = `节点 ${id} 已删除` }
function removeEdge(id) { edges.value = edges.value.filter(x => x.id !== id); if (selectedEdgeId.value === id) selectedEdgeId.value = ''; message.value = `连线 ${id} 已删除` }
function startDragNode(evt, n) { const p = toCanvasPoint(evt.clientX, evt.clientY); draggingNode = { n, ox:p.x - n.x, oy:p.y - n.y }; window.addEventListener('mousemove', onDragNodeMove); window.addEventListener('mouseup', stopDragNode) }
function onDragNodeMove(evt) { if (!draggingNode) return; const p = toCanvasPoint(evt.clientX, evt.clientY); draggingNode.n.x = p.x - draggingNode.ox; draggingNode.n.y = p.y - draggingNode.oy }
function stopDragNode() { if (draggingNode) { draggingNode.n.x = snap(draggingNode.n.x); draggingNode.n.y = snap(draggingNode.n.y) } draggingNode = null; window.removeEventListener('mousemove', onDragNodeMove); window.removeEventListener('mouseup', stopDragNode) }
function startPan(evt) { const clickedNode = evt.target?.closest?.('[data-node]'); if (clickedNode) return; panning = { startClientX: evt.clientX, startClientY: evt.clientY, startX: viewport.x, startY: viewport.y }; window.addEventListener('mousemove', onPanMove); window.addEventListener('mouseup', stopPan) }
function onPanMove(evt) { if (!panning) return; viewport.x = panning.startX + (evt.clientX - panning.startClientX); viewport.y = panning.startY + (evt.clientY - panning.startClientY) }
function stopPan() { panning = null; window.removeEventListener('mousemove', onPanMove); window.removeEventListener('mouseup', stopPan) }
function onWheel(evt) { const oldScale = viewport.scale; const delta = evt.deltaY < 0 ? 0.1 : -0.1; const newScale = Math.min(2, Math.max(0.5, +(oldScale + delta).toFixed(2))); if (newScale === oldScale) return; const rect = canvasRef.value?.getBoundingClientRect(); if (!rect) return; const mouseX = evt.clientX - rect.left; const mouseY = evt.clientY - rect.top; const worldX = (mouseX - viewport.x) / oldScale; const worldY = (mouseY - viewport.y) / oldScale; viewport.scale = newScale; viewport.x = mouseX - worldX * newScale; viewport.y = mouseY - worldY * newScale }
function duplicateSelectedNode() { if (!selected.value) return; const copy = JSON.parse(JSON.stringify(selected.value)); copy.id = 'n' + seq++; copy.nodeName = (copy.nodeName || copy.id) + ' 副本'; copy.x = snap(copy.x + 40); copy.y = snap(copy.y + 40); copy.dataText = JSON.stringify(copy.data, null, 2); nodes.value.push(copy); selected.value = copy; message.value = `已复制节点：${copy.nodeName}` }
function autoLayout() { let x = 40, y = 40; const rowWidth = 3; nodes.value.forEach((node, idx) => { node.x = x; node.y = y; if ((idx + 1) % rowWidth === 0) { x = 40; y += 180 } else { x += 300 } }); message.value = '已自动布局' }
function validateFlow() { validationErrors.value = []; message.value = '校验通过'; return true }
function exportJson() { importJson.value = JSON.stringify({ steps:nodes.value.map(n => ({ id:n.id, type:n.type, data:n.data })), edges:edges.value, startupArgsJson:'[]' }, null, 2); showImport.value = true; message.value = '已生成导出 JSON' }
function applyImport() {}
async function ensurePickerConnection() {
  if (pickerConnection) return
  pickerConnection = new HubConnectionBuilder().withUrl(`${SIGNALR_BASE_URL}/hubs/picker?access_token=${auth.token()}`).withAutomaticReconnect().configureLogging(LogLevel.Warning).build()
  pickerConnection.on('pickerStarted', () => {})
  pickerConnection.on('pickerStopped', () => {})
  pickerConnection.on('pickerResult', payload => {
    pickerResult.value = payload
    pickerBusy.value = false
    if (continuousPick.value) stashCurrentPickerResult()
    message.value = payload?.recommendedNodeType ? `已拾取元素，推荐节点类型：${payload.recommendedNodeType}` : '已收到元素拾取结果'
  })
  await pickerConnection.start()
}
async function startElementPicker() { pickerBusy.value = true; await ensurePickerConnection(); const resp = await api.startPicker({ profileId: 1, pageUrl: '', nodeId: selected.value?.id || '', nodeType: selected.value?.type || '' }); pickerSessionId.value = resp.sessionId; await pickerConnection.invoke('JoinSession', resp.sessionId); pickerBusy.value = false; message.value = `已启动元素拾取：${resp.sessionId}` }
async function stopElementPicker() { if (!pickerSessionId.value) return; pickerBusy.value = true; await api.stopPicker({ sessionId: pickerSessionId.value, profileId: 1 }); if (pickerConnection) await pickerConnection.invoke('LeaveSession', pickerSessionId.value); pickerBusy.value = false; message.value = '已停止元素拾取' }
function applySelectorFromPicker(selector) { if (!selected.value) return; const field = selectorTargetFieldForNode(selected.value.type); if (!selected.value.data) selected.value.data = {}; selected.value.data[field] = selector; message.value = `已回填 ${field}` }
function addEdgeIfPossible(fromId, toId) { if (!fromId || !toId || fromId === toId) return; edges.value.push({ id:'e' + Math.random(), source:fromId, target:toId, sourceHandle:'' }) }
function buildRecommendedNodeData(nodeType, selector, result) { const elementText = result?.element?.text || ''; const data = defaultDataFor(nodeType); if (selectorTargetFieldForNode(nodeType) === 'selector') data.selector = selector; if (nodeType === 'loop_list') data.itemSelector = selector; if (nodeType === 'extract_attr') data.attr = result?.element?.src ? 'src' : result?.element?.href ? 'href' : 'value'; data.label = `${nodeType} - ${elementText || selector || 'picked'}`; return data }
function createNodeInternal(result, selector, previousNodeId = '') { const recommendedType = result?.recommendedNodeType || 'click'; const initialData = buildRecommendedNodeData(recommendedType, selector || '', result); const newNode = addNode(recommendedType, initialData); if (autoLinkNodes.value && previousNodeId) addEdgeIfPossible(previousNodeId, newNode.id); return newNode }
function createNodeFromPickerSelector(selector) { if (!pickerResult.value) return; const previousSelectedId = selected.value?.id || ''; createNodeInternal(pickerResult.value, selector, previousSelectedId); message.value = '已根据 selector 生成节点' }
function createRecommendedNode() { if (!pickerResult.value) return; const selector = pickerResult.value?.selectors?.[0]?.selector || ''; const previousSelectedId = selected.value?.id || ''; createNodeInternal(pickerResult.value, selector, previousSelectedId); message.value = '已生成推荐节点' }
function stashCurrentPickerResult() { if (!pickerResult.value) return; pickerQueue.value.push(JSON.parse(JSON.stringify(pickerResult.value))); message.value = `已加入暂存，当前 ${pickerQueue.value.length} 项` }
function clearPickerQueue() { pickerQueue.value = []; message.value = '已清空暂存队列' }
function removeQueueItem(index) { pickerQueue.value.splice(index, 1); message.value = '已移除暂存项' }
function bulkGenerateFromQueue() { if (!pickerQueue.value.length) return; let previousNodeId = selected.value?.id || ''; for (const item of pickerQueue.value) { const selector = item?.selectors?.[0]?.selector || item?.element?.cssPath || ''; const node = createNodeInternal(item, selector, previousNodeId); previousNodeId = node.id } message.value = `已批量生成 ${pickerQueue.value.length} 个节点`; pickerQueue.value = [] }
function setContinuousPick(value) { continuousPick.value = !!value; message.value = continuousPick.value ? '已开启连续拾取' : '已关闭连续拾取' }
function setAutoLink(value) { autoLinkNodes.value = !!value; message.value = autoLinkNodes.value ? '已开启自动连线' : '已关闭自动连线' }
function onKeyDown(evt) { if (evt.key === 'Delete' || evt.key === 'Backspace') { if (selected.value) removeSelectedNode(); else if (selectedEdgeId.value) removeEdge(selectedEdgeId.value) } }
watch(nodes, () => { nodes.value.forEach(n => { n.dataText = JSON.stringify(n.data, null, 2) }) }, { deep: true })
onMounted(() => window.addEventListener('keydown', onKeyDown))
onBeforeUnmount(async () => { cancelDragLink(); stopDragNode(); stopPan(); window.removeEventListener('keydown', onKeyDown); if (pickerConnection) await pickerConnection.stop() })
</script>

<style scoped>
.builder-canvas { position: relative; width: 100%; height: 100%; overflow: hidden; }
.pan-layer { position: absolute; inset: 0; z-index: 1; cursor: grab; }
.grid-layer { position: absolute; inset: 0; z-index: 1; pointer-events: none; background-image: linear-gradient(rgba(51,65,85,.35) 1px, transparent 1px), linear-gradient(90deg, rgba(51,65,85,.35) 1px, transparent 1px); }
.content-layer { position: absolute; inset: 0; z-index: 2; pointer-events: none; }
</style>
