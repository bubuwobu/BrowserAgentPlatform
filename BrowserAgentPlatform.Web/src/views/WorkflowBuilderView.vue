
<template>
  <div>
    <div class="page-title">编排器</div>
    <div class="page-subtitle">Phase 5.4 修正版：多选、复制粘贴、自动布局、网格吸附、小地图、连线插入节点。</div>

    <div class="card" style="margin-bottom:16px;">
      <div class="toolbar">
        <div>
          <div style="font-weight:700;">顶部工具栏</div>
          <div class="muted">Shift+点击多选，Ctrl/Cmd+C 复制，Ctrl/Cmd+V 粘贴，Delete 删除已选连线。</div>
        </div>
        <div class="section-actions">
          <button class="btn secondary" @click="newFlow">新建</button>
          <button class="btn secondary" @click="loadQuickTemplate('basic')">基础模板</button>
          <button class="btn secondary" @click="loadQuickTemplate('login')">登录模板</button>
          <button class="btn secondary" @click="validateFlow">校验</button>
          <button class="btn secondary" @click="exportJson">导出</button>
          <button class="btn secondary" @click="showImport = !showImport">{{ showImport ? '收起导入' : '导入' }}</button>
          <button class="btn secondary" @click="duplicateSelectedNode" :disabled="!selected">复制节点</button>
          <button class="btn secondary" @click="autoLayout" :disabled="!nodes.length">自动布局</button>
          <button class="btn secondary" @click="resetViewport">重置视图</button>
          <button class="btn" @click="saveTask">保存任务</button>
          <button class="btn success" @click="saveTemplate">保存模板</button>
        </div>
      </div>

      <div v-if="showImport" style="margin-top:12px;">
        <div class="form-field">
          <label>导入工作流 JSON</label>
          <textarea class="input" rows="10" v-model="importJson"></textarea>
          <div class="help">导入后会覆盖当前画布。</div>
        </div>
        <div class="section-actions" style="margin-top:10px;">
          <button class="btn" @click="applyImport">应用导入</button>
        </div>
      </div>

      <div v-if="message" class="muted" style="margin-top:10px;">{{ message }}</div>
      <div v-if="validationErrors.length" style="margin-top:10px;">
        <div style="font-weight:700;color:#fca5a5;">校验结果</div>
        <div v-for="(err, idx) in validationErrors" :key="idx" style="color:#fca5a5;font-size:13px;margin-top:4px;">
          {{ idx + 1 }}. {{ err }}
        </div>
      </div>
    </div>

    <div class="grid" style="grid-template-columns:220px 1fr 420px;">
      <div class="card">
        <div style="font-weight:700;margin-bottom:10px;">节点库</div>
        <div v-for="t in nodeTypes" :key="t" style="margin-bottom:8px;">
          <button class="btn" style="width:100%;" @click="addNode(t)">{{ t }}</button>
        </div>
      </div>

      <div
        class="card"
        ref="canvasRef"
        style="position:relative;height:720px;overflow:hidden;cursor:grab;"
        @mousedown.self="startPan"
        @wheel.prevent="onWheel"
      >
        <div
          :style="{
            position:'absolute',
            inset:'0',
            backgroundImage:'linear-gradient(rgba(51,65,85,.35) 1px, transparent 1px), linear-gradient(90deg, rgba(51,65,85,.35) 1px, transparent 1px)',
            backgroundSize:`${20 * viewport.scale}px ${20 * viewport.scale}px`,
            backgroundPosition:`${viewport.x}px ${viewport.y}px`
          }"
        ></div>

        <div
          :style="{
            position:'absolute',
            inset:'0',
            transform:`translate(${viewport.x}px, ${viewport.y}px) scale(${viewport.scale})`,
            transformOrigin:'0 0'
          }"
        >
          <svg style="position:absolute;inset:0;width:3200px;height:2200px;overflow:visible;">
            <path
              v-for="e in edges"
              :key="e.id"
              :d="edgePath(e)"
              :stroke="selectedEdgeId === e.id ? '#f59e0b' : hoveredEdgeId === e.id ? '#93c5fd' : '#60a5fa'"
              :stroke-width="selectedEdgeId === e.id ? 3.5 : hoveredEdgeId === e.id ? 3 : 2.5"
              fill="none"
              style="cursor:pointer"
              @mouseenter="hoveredEdgeId = e.id"
              @mouseleave="hoveredEdgeId = ''"
              @click.stop="selectEdge(e.id)"
            />
            <path
              v-if="dragLink.active"
              :d="dragEdgePath"
              stroke="#22c55e"
              stroke-width="2.5"
              stroke-dasharray="6 4"
              fill="none"
            />
          </svg>

          <div
            v-for="n in nodes"
            :key="n.id"
            @mousedown.stop="startDragNode($event, n)"
            @click.stop="selectNode(n, $event)"
            :style="{
              position:'absolute',
              left:n.x + 'px',
              top:n.y + 'px',
              width:'210px',
              padding:'12px',
              borderRadius:'14px',
              border: nodeBorder(n),
              background:'#0f172a',
              cursor:'move',
              userSelect:'none'
            }"
          >
            <div
              @mouseup.stop="finishDragLink(n.id)"
              style="position:absolute;left:-7px;top:50%;transform:translateY(-50%);width:14px;height:14px;border-radius:999px;background:#38bdf8;border:2px solid #0f172a;cursor:crosshair;"
              title="输入锚点"
            ></div>

            <div
              @mousedown.stop="startDragLink($event, n.id)"
              style="position:absolute;right:-7px;top:50%;transform:translateY(-50%);width:14px;height:14px;border-radius:999px;background:#22c55e;border:2px solid #0f172a;cursor:crosshair;"
              title="输出锚点"
            ></div>

            <div style="display:flex;justify-content:space-between;gap:8px;align-items:flex-start;">
              <div style="min-width:0;">
                <div style="font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">{{ n.nodeName || n.id }}</div>
                <div style="font-size:12px;color:#94a3b8;">{{ n.type }}</div>
              </div>
              <span class="badge queued">{{ n.id }}</span>
            </div>

            <div style="font-size:12px;color:#94a3b8;margin-top:8px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
              {{ n.data.label || '-' }}
            </div>

            <div class="section-actions" style="margin-top:8px;gap:6px;">
              <button class="btn secondary" style="padding:6px 8px;" @click.stop="selectNode(n, { shiftKey: false })">编辑</button>
            </div>
          </div>
        </div>

        <div style="position:absolute;right:12px;bottom:12px;width:220px;height:140px;border:1px solid #334155;border-radius:12px;background:rgba(2,6,23,.88);overflow:hidden;">
          <svg style="width:100%;height:100%;">
            <line
              v-for="e in edges"
              :key="'mini-' + e.id"
              :x1="miniPoint(sourcePoint(e.source)).x"
              :y1="miniPoint(sourcePoint(e.source)).y"
              :x2="miniPoint(targetPoint(e.target)).x"
              :y2="miniPoint(targetPoint(e.target)).y"
              stroke="#64748b"
              stroke-width="1.5"
            />
            <rect
              v-for="n in nodes"
              :key="'mini-node-' + n.id"
              :x="miniPoint({ x: n.x, y: n.y }).x"
              :y="miniPoint({ x: n.x, y: n.y }).y"
              width="16"
              height="10"
              rx="2"
              :fill="selected?.id === n.id ? '#60a5fa' : selectedNodeIds.includes(n.id) ? '#22c55e' : '#94a3b8'"
            />
          </svg>
        </div>
      </div>

      <div class="card">
        <div style="font-weight:700;margin-bottom:12px;">属性配置</div>

        <template v-if="selected">
          <div class="form-field">
            <label>节点名称</label>
            <input class="input" v-model="selected.nodeName" />
          </div>

          <div class="form-field" style="margin-top:10px;">
            <label>节点 ID</label>
            <input class="input" v-model="selected.id" />
          </div>

          <div class="form-field" style="margin-top:10px;">
            <label>步骤显示名称</label>
            <input class="input" v-model="selected.data.label" />
          </div>

          <div v-for="field in currentSchema" :key="field.key" style="margin-top:10px;">
            <div class="form-field">
              <label>{{ field.label }}</label>
              <input v-if="field.type !== 'textarea' && field.type !== 'number'" class="input" v-model="selected.data[field.key]" :placeholder="field.placeholder || ''" />
              <input v-else-if="field.type === 'number'" class="input" type="number" v-model.number="selected.data[field.key]" :placeholder="field.placeholder || ''" />
              <textarea v-else class="input" rows="4" v-model="selected.data[field.key]" :placeholder="field.placeholder || ''"></textarea>
              <div v-if="field.help" class="help">{{ field.help }}</div>
            </div>
          </div>

          <details style="margin-top:12px;">
            <summary style="cursor:pointer;">高级 JSON</summary>
            <textarea class="input" rows="10" v-model="selected.dataText" @change="syncJson(selected)" style="margin-top:8px;"></textarea>
          </details>
        </template>

        <template v-else-if="selectedEdge">
          <div class="form-field">
            <label>当前连线</label>
            <div class="muted">{{ edgeLabel(selectedEdge.source) }} → {{ edgeLabel(selectedEdge.target) }}</div>
          </div>
          <div class="form-field" style="margin-top:10px;">
            <label>sourceHandle</label>
            <input class="input" v-model="selectedEdge.sourceHandle" />
          </div>
          <div class="section-actions" style="margin-top:12px;">
            <button class="btn warn" @click="removeEdge(selectedEdge.id)">删除当前连线</button>
          </div>
        </template>

        <div v-else class="muted">请选择一个节点或连线</div>

        <div class="card card-dark" style="margin-top:16px;">
          <div style="font-weight:700;margin-bottom:8px;">保存信息</div>
          <div class="form-field">
            <label>任务名称</label>
            <input class="input" v-model="task.name" />
          </div>
          <div class="form-field" style="margin-top:10px;">
            <label>BrowserProfileId</label>
            <input class="input" v-model.number="task.browserProfileId" />
          </div>
          <div class="form-field" style="margin-top:10px;">
            <label>任务说明</label>
            <textarea class="input" rows="3" v-model="task.note"></textarea>
          </div>
        </div>
      </div>
    </div>

    <div class="card" style="margin-top:16px;">
      <div class="toolbar">
        <div style="font-weight:700;">节点列表</div>
        <div class="muted">当前共 {{ nodes.length }} 个节点，{{ edges.length }} 条连线。已多选 {{ selectedNodeIds.length }} 个节点。</div>
      </div>

      <div v-if="!nodes.length" class="muted" style="margin-top:10px;">暂无节点</div>

      <div v-for="n in nodes" :key="n.id" class="card card-dark" style="margin-top:12px;">
        <div class="toolbar">
          <div>
            <div style="font-weight:700;">{{ n.nodeName || n.id }}</div>
            <div class="muted">{{ n.id }} / {{ n.type }}</div>
            <div class="muted">step label={{ n.data.label || '-' }}</div>
          </div>
          <div class="section-actions">
            <button class="btn secondary" @click="selectNode(n, { shiftKey: false })">编辑</button>
            <button class="btn warn" @click="askDeleteNode(n.id)">删除节点</button>
          </div>
        </div>
      </div>

      <div v-if="edges.length" style="margin-top:18px;">
        <div style="font-weight:700;">连线列表</div>
        <div v-for="e in edges" :key="e.id" class="card card-dark" style="margin-top:10px;">
          <div class="toolbar">
            <div class="muted">{{ edgeLabel(e.source) }} → {{ edgeLabel(e.target) }}</div>
            <div class="section-actions">
              <button class="btn secondary" @click="insertNodeIntoEdge(e.id)">中间插入等待节点</button>
              <button class="btn warn" @click="removeEdge(e.id)">删除连线</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <ConfirmDialog
      :open="deleteNodeOpen"
      title="删除节点"
      message="删除节点后，关联到这个节点的连线也会一并删除。"
      confirm-text="确认删除"
      @cancel="deleteNodeOpen = false"
      @confirm="removeNodeConfirmed"
    />
  </div>
</template>

<script setup>
import { computed, reactive, ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { api } from '../services/api'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const nodeTypes = ['open','click','type','wait_for_element','wait_for_timeout','scroll','extract_text','branch','loop','end_success','end_fail']
const nodeSchemas = {
  open: [{ key: 'url', label: '页面地址', type: 'text', placeholder: 'https://example.com', help: '任务打开的目标网页。' }],
  click: [{ key: 'selector', label: '元素选择器', type: 'text', placeholder: '#submit', help: '建议填写稳定的 CSS 选择器。' }],
  type: [
    { key: 'selector', label: '输入框选择器', type: 'text', placeholder: 'input[name=email]' },
    { key: 'value', label: '输入内容', type: 'textarea', placeholder: '需要输入的内容' }
  ],
  wait_for_element: [
    { key: 'selector', label: '等待元素选择器', type: 'text', placeholder: '#app' },
    { key: 'timeout', label: '超时(ms)', type: 'number', placeholder: '10000' }
  ],
  wait_for_timeout: [{ key: 'timeout', label: '等待时长(ms)', type: 'number', placeholder: '1000' }],
  scroll: [{ key: 'deltaY', label: '滚动距离', type: 'number', placeholder: '600' }],
  extract_text: [{ key: 'selector', label: '提取文本选择器', type: 'text', placeholder: 'body' }],
  branch: [{ key: 'mode', label: '分支模式', type: 'text', placeholder: 'first / random' }],
  loop: [{ key: 'count', label: '循环次数', type: 'number', placeholder: '3' }],
  end_success: [],
  end_fail: []
}

const canvasRef = ref(null)
const nodes = ref([])
const edges = ref([])
const selected = ref(null)
const selectedNodeIds = ref([])
const selectedEdgeId = ref('')
const hoveredEdgeId = ref('')
const task = reactive({ name:'', browserProfileId:1, note:'' })
const showImport = ref(false)
const importJson = ref('')
const message = ref('')
const validationErrors = ref([])
const deleteNodeOpen = ref(false)
const deleteNodeId = ref('')
const dragLink = reactive({ active: false, sourceId: '', startX: 0, startY: 0, currentX: 0, currentY: 0 })
const viewport = reactive({ x: 0, y: 0, scale: 1 })
const clipboardNode = ref(null)

let seq = 1
let draggingNode = null
let panning = null

const currentSchema = computed(() => selected.value ? (nodeSchemas[selected.value.type] || []) : [])
const selectedEdge = computed(() => edges.value.find(x => x.id === selectedEdgeId.value) || null)
const dragEdgePath = computed(() => bezierPath(dragLink.startX, dragLink.startY, dragLink.currentX, dragLink.currentY))

function nodeBorder(n) {
  if (selected.value?.id === n.id) return '2px solid #60a5fa'
  if (selectedNodeIds.value.includes(n.id)) return '2px solid #22c55e'
  return '1px solid #374151'
}

function defaultDataFor(type) {
  const map = {
    open: { label: '打开页面', url: 'https://example.com' },
    click: { label: '点击元素', selector: '#submit' },
    type: { label: '输入内容', selector: 'input', value: '' },
    wait_for_element: { label: '等待元素', selector: '#app', timeout: 10000 },
    wait_for_timeout: { label: '固定等待', timeout: 1000 },
    scroll: { label: '滚动页面', deltaY: 600 },
    extract_text: { label: '提取文本', selector: 'body' },
    branch: { label: '分支', mode: 'first' },
    loop: { label: '循环', count: 2 },
    end_success: { label: '成功结束' },
    end_fail: { label: '失败结束' }
  }
  return map[type] || { label: type }
}

function sourcePoint(id) {
  const n = nodes.value.find(x => x.id === id)
  return n ? { x: n.x + 210, y: n.y + 44 } : { x: 0, y: 0 }
}

function targetPoint(id) {
  const n = nodes.value.find(x => x.id === id)
  return n ? { x: n.x, y: n.y + 44 } : { x: 0, y: 0 }
}

function miniPoint(p) {
  return { x: p.x * 0.08 + 10, y: p.y * 0.06 + 10 }
}

function bezierPath(x1, y1, x2, y2) {
  const dx = Math.max(80, Math.abs(x2 - x1) * 0.4)
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`
}

function edgePath(e) {
  const s = sourcePoint(e.source)
  const t = targetPoint(e.target)
  return bezierPath(s.x, s.y, t.x, t.y)
}

function edgeLabel(id) {
  const n = nodes.value.find(x => x.id === id)
  return n ? (n.nodeName || n.id) : id
}

function toCanvasPoint(clientX, clientY) {
  const rect = canvasRef.value?.getBoundingClientRect()
  if (!rect) return { x: 0, y: 0 }
  return {
    x: (clientX - rect.left - viewport.x) / viewport.scale,
    y: (clientY - rect.top - viewport.y) / viewport.scale
  }
}

function snap(v) {
  return Math.round(v / 20) * 20
}

function resetViewport() {
  viewport.x = 0
  viewport.y = 0
  viewport.scale = 1
  message.value = '视图已重置'
}

function newFlow() {
  nodes.value = []
  edges.value = []
  selected.value = null
  selectedNodeIds.value = []
  selectedEdgeId.value = ''
  validationErrors.value = []
  cancelDragLink()
  message.value = '已新建空白工作流'
}

function loadQuickTemplate(type) {
  if (type === 'basic') {
    nodes.value = [
      { id: 'n1', nodeName: '打开首页节点', type: 'open', x: 40, y: 40, data: { label: '打开页面', url: 'https://example.com' }, dataText: '' },
      { id: 'n2', nodeName: '等待节点', type: 'wait_for_timeout', x: 320, y: 160, data: { label: '等待', timeout: 1000 }, dataText: '' },
      { id: 'n3', nodeName: '完成节点', type: 'end_success', x: 620, y: 280, data: { label: '完成' }, dataText: '' }
    ]
    edges.value = [
      { id: 'e1', source: 'n1', target: 'n2', sourceHandle: '' },
      { id: 'e2', source: 'n2', target: 'n3', sourceHandle: '' }
    ]
  } else {
    nodes.value = [
      { id: 'n1', nodeName: '打开登录页节点', type: 'open', x: 40, y: 40, data: { label: '打开登录页', url: 'http://localhost:3000/login' }, dataText: '' },
      { id: 'n2', nodeName: '等待表单节点', type: 'wait_for_element', x: 300, y: 140, data: { label: '等待登录表单', selector: "form[action='/login']", timeout: 10000 }, dataText: '' },
      { id: 'n3', nodeName: '输入用户名节点', type: 'type', x: 580, y: 240, data: { label: '输入用户名', selector: "input[name='username']", value: 'alice' }, dataText: '' },
      { id: 'n4', nodeName: '输入密码节点', type: 'type', x: 860, y: 340, data: { label: '输入密码', selector: "input[name='password']", value: '123456' }, dataText: '' },
      { id: 'n5', nodeName: '点击登录节点', type: 'click', x: 1140, y: 440, data: { label: '点击登录', selector: "button[type='submit']" }, dataText: '' },
      { id: 'n6', nodeName: '完成节点', type: 'end_success', x: 1420, y: 540, data: { label: '完成' }, dataText: '' }
    ]
    edges.value = [
      { id: 'e1', source: 'n1', target: 'n2', sourceHandle: '' },
      { id: 'e2', source: 'n2', target: 'n3', sourceHandle: '' },
      { id: 'e3', source: 'n3', target: 'n4', sourceHandle: '' },
      { id: 'e4', source: 'n4', target: 'n5', sourceHandle: '' },
      { id: 'e5', source: 'n5', target: 'n6', sourceHandle: '' }
    ]
  }
  nodes.value.forEach(n => { n.dataText = JSON.stringify(n.data, null, 2) })
  selected.value = nodes.value[0] || null
  selectedNodeIds.value = selected.value ? [selected.value.id] : []
  selectedEdgeId.value = ''
  validationErrors.value = []
  cancelDragLink()
  message.value = '已加载快速模板'
}

function addNode(type) {
  const data = defaultDataFor(type)
  const n = {
    id: 'n' + seq++,
    nodeName: `${type} 节点`,
    type,
    x: snap(40 + nodes.value.length * 40),
    y: snap(40 + nodes.value.length * 30),
    data,
    dataText: JSON.stringify(data, null, 2)
  }
  nodes.value.push(n)
  selected.value = n
  selectedNodeIds.value = [n.id]
  selectedEdgeId.value = ''
  message.value = `已添加节点：${type}`
}

function selectNode(n, evt) {
  selected.value = n
  selectedEdgeId.value = ''
  if (evt?.shiftKey) {
    selectedNodeIds.value = selectedNodeIds.value.includes(n.id)
      ? selectedNodeIds.value.filter(x => x !== n.id)
      : [...selectedNodeIds.value, n.id]
  } else {
    selectedNodeIds.value = [n.id]
  }
}

function selectEdge(id) {
  selected.value = null
  selectedNodeIds.value = []
  selectedEdgeId.value = id
  message.value = `已选中连线 ${id}`
}

function startDragLink(evt, sourceId) {
  const pt = sourcePoint(sourceId)
  dragLink.active = true
  dragLink.sourceId = sourceId
  dragLink.startX = pt.x
  dragLink.startY = pt.y
  const p = toCanvasPoint(evt.clientX, evt.clientY)
  dragLink.currentX = p.x
  dragLink.currentY = p.y
  window.addEventListener('mousemove', onDragLinkMove)
  window.addEventListener('mouseup', cancelDragLink)
  message.value = `正在从 ${sourceId} 拖拽连线`
}

function onDragLinkMove(evt) {
  if (!dragLink.active) return
  const p = toCanvasPoint(evt.clientX, evt.clientY)
  dragLink.currentX = p.x
  dragLink.currentY = p.y
}

function finishDragLink(targetId) {
  if (!dragLink.active) return
  if (!dragLink.sourceId || dragLink.sourceId === targetId) {
    cancelDragLink()
    return
  }
  edges.value.push({ id: 'e' + Math.random(), source: dragLink.sourceId, target: targetId, sourceHandle: '' })
  const sourceName = edgeLabel(dragLink.sourceId)
  const targetName = edgeLabel(targetId)
  cancelDragLink()
  message.value = `已创建连线：${sourceName} → ${targetName}`
}

function cancelDragLink() {
  dragLink.active = false
  dragLink.sourceId = ''
  dragLink.startX = 0
  dragLink.startY = 0
  dragLink.currentX = 0
  dragLink.currentY = 0
  window.removeEventListener('mousemove', onDragLinkMove)
  window.removeEventListener('mouseup', cancelDragLink)
}

function syncJson(n) {
  try {
    n.data = JSON.parse(n.dataText)
    message.value = '高级 JSON 已同步'
  } catch {
    message.value = '高级 JSON 解析失败，请检查格式'
  }
}

function askDeleteNode(id) {
  deleteNodeId.value = id
  deleteNodeOpen.value = true
}

function removeNodeConfirmed() {
  const id = deleteNodeId.value
  nodes.value = nodes.value.filter(x => x.id !== id)
  edges.value = edges.value.filter(x => x.source !== id && x.target !== id)
  if (selected.value?.id === id) selected.value = nodes.value[0] || null
  selectedNodeIds.value = selectedNodeIds.value.filter(x => x !== id)
  deleteNodeOpen.value = false
  deleteNodeId.value = ''
  message.value = `节点 ${id} 已删除`
}

function removeEdge(id) {
  edges.value = edges.value.filter(x => x.id !== id)
  if (selectedEdgeId.value === id) selectedEdgeId.value = ''
  message.value = `连线 ${id} 已删除`
}

function startDragNode(evt, n) {
  const p = toCanvasPoint(evt.clientX, evt.clientY)
  const ids = selectedNodeIds.value.includes(n.id) ? selectedNodeIds.value : [n.id]
  draggingNode = {
    startMouseX: p.x,
    startMouseY: p.y,
    starts: ids.map(id => {
      const node = nodes.value.find(x => x.id === id)
      return { id, x: node.x, y: node.y }
    })
  }
  window.addEventListener('mousemove', onDragNodeMove)
  window.addEventListener('mouseup', stopDragNode)
}

function onDragNodeMove(evt) {
  if (!draggingNode) return
  const p = toCanvasPoint(evt.clientX, evt.clientY)
  const dx = p.x - draggingNode.startMouseX
  const dy = p.y - draggingNode.startMouseY
  draggingNode.starts.forEach(item => {
    const node = nodes.value.find(x => x.id === item.id)
    if (node) {
      node.x = item.x + dx
      node.y = item.y + dy
    }
  })
}

function stopDragNode() {
  if (draggingNode) {
    draggingNode.starts.forEach(item => {
      const node = nodes.value.find(x => x.id === item.id)
      if (node) {
        node.x = snap(node.x)
        node.y = snap(node.y)
      }
    })
  }
  draggingNode = null
  window.removeEventListener('mousemove', onDragNodeMove)
  window.removeEventListener('mouseup', stopDragNode)
}

function startPan(evt) {
  panning = { startClientX: evt.clientX, startClientY: evt.clientY, startX: viewport.x, startY: viewport.y }
  window.addEventListener('mousemove', onPanMove)
  window.addEventListener('mouseup', stopPan)
}

function onPanMove(evt) {
  if (!panning) return
  viewport.x = panning.startX + (evt.clientX - panning.startClientX)
  viewport.y = panning.startY + (evt.clientY - panning.startClientY)
}

function stopPan() {
  panning = null
  window.removeEventListener('mousemove', onPanMove)
  window.removeEventListener('mouseup', stopPan)
}

function onWheel(evt) {
  const oldScale = viewport.scale
  const delta = evt.deltaY < 0 ? 0.1 : -0.1
  const newScale = Math.min(2, Math.max(0.5, +(oldScale + delta).toFixed(2)))
  if (newScale === oldScale) return
  const rect = canvasRef.value?.getBoundingClientRect()
  if (!rect) return
  const mouseX = evt.clientX - rect.left
  const mouseY = evt.clientY - rect.top
  const worldX = (mouseX - viewport.x) / oldScale
  const worldY = (mouseY - viewport.y) / oldScale
  viewport.scale = newScale
  viewport.x = mouseX - worldX * newScale
  viewport.y = mouseY - worldY * newScale
}

function duplicateSelectedNode() {
  if (!selected.value) return
  const copy = JSON.parse(JSON.stringify(selected.value))
  copy.id = 'n' + seq++
  copy.nodeName = (selected.value.nodeName || selected.value.id) + ' 副本'
  copy.x = snap(selected.value.x + 40)
  copy.y = snap(selected.value.y + 40)
  copy.dataText = JSON.stringify(copy.data, null, 2)
  nodes.value.push(copy)
  selected.value = copy
  selectedNodeIds.value = [copy.id]
  message.value = `已复制节点：${copy.nodeName}`
}

function autoLayout() {
  let x = 40
  let y = 40
  const rowWidth = 3
  nodes.value.forEach((node, idx) => {
    node.x = x
    node.y = y
    if ((idx + 1) % rowWidth === 0) {
      x = 40
      y += 180
    } else {
      x += 280
    }
  })
  message.value = '已自动布局'
}

function insertNodeIntoEdge(edgeId) {
  const edge = edges.value.find(x => x.id === edgeId)
  if (!edge) return
  const s = nodes.value.find(x => x.id === edge.source)
  const t = nodes.value.find(x => x.id === edge.target)
  if (!s || !t) return
  const newNode = {
    id: 'n' + seq++,
    nodeName: '中间等待节点',
    type: 'wait_for_timeout',
    x: snap((s.x + t.x) / 2),
    y: snap((s.y + t.y) / 2),
    data: { label: '等待', timeout: 1000 },
    dataText: JSON.stringify({ label: '等待', timeout: 1000 }, null, 2)
  }
  nodes.value.push(newNode)
  edges.value = edges.value.filter(x => x.id !== edgeId)
  edges.value.push({ id: 'e' + Math.random(), source: edge.source, target: newNode.id, sourceHandle: '' })
  edges.value.push({ id: 'e' + Math.random(), source: newNode.id, target: edge.target, sourceHandle: '' })
  selected.value = newNode
  selectedNodeIds.value = [newNode.id]
  selectedEdgeId.value = ''
  message.value = '已在连线中间插入等待节点'
}

watch(nodes, () => {
  nodes.value.forEach(n => { n.dataText = JSON.stringify(n.data, null, 2) })
}, { deep: true })

function payload() {
  return {
    steps: nodes.value.map(n => ({ id: n.id, type: n.type, data: n.data })),
    edges: edges.value,
    startupArgsJson: '[]'
  }
}

function validateFlow() {
  const errors = []
  const ids = new Set()
  for (const n of nodes.value) {
    if (!n.id) errors.push('存在没有 id 的节点')
    if (ids.has(n.id)) errors.push(`节点 id 重复：${n.id}`)
    ids.add(n.id)
    if (!n.nodeName) errors.push(`节点 ${n.id} 缺少节点名称`)
    if (!n.data?.label) errors.push(`节点 ${n.id} 缺少步骤显示名称`)
    if (n.type === 'open' && !n.data?.url) errors.push(`节点 ${n.id} 缺少 url`)
    if (['click','type','wait_for_element','extract_text'].includes(n.type) && !n.data?.selector) errors.push(`节点 ${n.id} 缺少 selector`)
    if (n.type === 'type' && !n.data?.value) errors.push(`节点 ${n.id} 缺少输入内容 value`)
  }
  for (const e of edges.value) {
    if (!ids.has(e.source)) errors.push(`连线起点不存在：${e.source}`)
    if (!ids.has(e.target)) errors.push(`连线终点不存在：${e.target}`)
  }
  if (!nodes.value.some(x => x.type === 'end_success' || x.type === 'end_fail')) {
    errors.push('至少需要一个结束节点（end_success 或 end_fail）')
  }
  validationErrors.value = errors
  message.value = errors.length ? '校验未通过' : '校验通过'
  return errors.length === 0
}

function exportJson() {
  importJson.value = JSON.stringify(payload(), null, 2)
  showImport.value = true
  message.value = '已生成导出 JSON'
}

function applyImport() {
  try {
    const parsed = JSON.parse(importJson.value || '{}')
    const parsedSteps = Array.isArray(parsed.steps) ? parsed.steps : []
    const parsedEdges = Array.isArray(parsed.edges) ? parsed.edges : []
    nodes.value = parsedSteps.map((step, idx) => ({
      id: step.id || `n${idx + 1}`,
      nodeName: step.data?.label || step.id || `节点${idx + 1}`,
      type: step.type || 'open',
      x: snap(40 + idx * 80),
      y: snap(40 + idx * 50),
      data: step.data || { label: step.type || 'node' },
      dataText: JSON.stringify(step.data || {}, null, 2)
    }))
    edges.value = parsedEdges.map((e, idx) => ({ id: e.id || `e${idx + 1}`, ...e }))
    selected.value = nodes.value[0] || null
    selectedNodeIds.value = selected.value ? [selected.value.id] : []
    selectedEdgeId.value = ''
    validationErrors.value = []
    cancelDragLink()
    message.value = '导入成功'
  } catch {
    message.value = '导入 JSON 失败，请检查格式'
  }
}

async function saveTask() {
  if (!validateFlow()) return
  try {
    await api.createTask({
      name: task.name || '未命名任务',
      browserProfileId: task.browserProfileId,
      accountId: null,
      schedulingStrategy: 'least_loaded',
      preferredAgentId: null,
      isEnabled: true,
      scheduleType: 'manual',
      scheduleConfigJson: '{}',
      payloadJson: JSON.stringify(payload(), null, 2),
      priority: 100,
      timeoutSeconds: 300,
      retryPolicyJson: '{"maxRetries":1}'
    })
    message.value = '任务已保存'
  } catch (err) {
    message.value = err.message || '保存任务失败'
  }
}

async function saveTemplate() {
  if (!validateFlow()) return
  try {
    await api.createTemplate({
      name: task.name || '未命名模板',
      definitionJson: JSON.stringify(payload(), null, 2)
    })
    message.value = '模板已保存'
  } catch (err) {
    message.value = err.message || '保存模板失败'
  }
}

function onKeyDown(evt) {
  const isMeta = evt.ctrlKey || evt.metaKey
  if ((evt.key === 'Delete' || evt.key === 'Backspace') && selectedEdgeId.value) {
    removeEdge(selectedEdgeId.value)
  }
  if (isMeta && evt.key.toLowerCase() === 'c' && selected.value) {
    clipboardNode.value = JSON.parse(JSON.stringify(selected.value))
    message.value = `已复制节点：${selected.value.nodeName || selected.value.id}`
  }
  if (isMeta && evt.key.toLowerCase() === 'v' && clipboardNode.value) {
    const copy = JSON.parse(JSON.stringify(clipboardNode.value))
    copy.id = 'n' + seq++
    copy.nodeName = (copy.nodeName || copy.id) + ' 副本'
    copy.x = snap((copy.x || 40) + 40)
    copy.y = snap((copy.y || 40) + 40)
    copy.dataText = JSON.stringify(copy.data, null, 2)
    nodes.value.push(copy)
    selected.value = copy
    selectedNodeIds.value = [copy.id]
    selectedEdgeId.value = ''
    message.value = `已粘贴节点：${copy.nodeName}`
  }
}

onMounted(() => {
  window.addEventListener('keydown', onKeyDown)
})

onBeforeUnmount(() => {
  cancelDragLink()
  stopDragNode()
  stopPan()
  window.removeEventListener('keydown', onKeyDown)
})
</script>
