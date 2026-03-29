
<template>
  <div class="card card-dark">
    <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
      <div>
        <div style="font-weight:700;">元素拾取器</div>
        <div class="muted">Phase 6.2：保留现有节点/连线能力，同时支持 API 推荐模板一键生成整段流程。</div>
      </div>
      <div class="section-actions">
        <button class="btn" @click="$emit('start')" :disabled="busy">开始拾取</button>
        <button class="btn warn" @click="$emit('stop')" :disabled="busy || !sessionId">停止拾取</button>
      </div>
    </div>

    <div class="muted" style="margin-top:8px;">Session：{{ sessionId || '-' }}</div>

    <div class="grid" style="grid-template-columns:1fr 1fr; gap:10px; margin-top:10px;">
      <label class="form-field" style="margin:0;">
        <span>连续拾取</span>
        <select class="input" :value="continuousPick ? 'on' : 'off'" @change="$emit('toggle-continuous', $event.target.value === 'on')">
          <option value="off">关闭</option>
          <option value="on">开启</option>
        </select>
      </label>

      <label class="form-field" style="margin:0;">
        <span>自动连线</span>
        <select class="input" :value="autoLink ? 'on' : 'off'" @change="$emit('toggle-autolink', $event.target.value === 'on')">
          <option value="on">开启</option>
          <option value="off">关闭</option>
        </select>
      </label>
    </div>

    <div v-if="result" style="margin-top:12px;">
      <div style="font-weight:700;">当前拾取结果</div>
      <div class="card" style="margin-top:8px;">
        <div class="muted">tag: {{ result.element?.tagName || '-' }}</div>
        <div class="muted">text: {{ result.element?.text || '-' }}</div>
        <div class="muted">id: {{ result.element?.id || '-' }}</div>
        <div class="muted">name: {{ result.element?.name || '-' }}</div>
        <div class="muted">推荐节点: {{ result.recommendedNodeType || '-' }}</div>
        <div class="muted">推荐字段: {{ result.recommendedTargetField || '-' }}</div>
        <div class="muted">推荐模板: {{ result.recommendedFlowTemplate || '-' }}</div>
        <div class="muted">推荐动作数: {{ (result.recommendedFlowSteps || []).length }}</div>
      </div>

      <div class="section-actions" style="margin-top:10px; flex-wrap:wrap;">
        <button class="btn" @click="$emit('stash-current')">加入暂存</button>
        <button class="btn" @click="$emit('create-recommended-node')" :disabled="!result.recommendedNodeType">按推荐生成节点</button>
        <button class="btn success" @click="$emit('create-mini-flow')">生成小流程模板</button>
        <button class="btn success" @click="$emit('generate-api-flow')" :disabled="!(result.recommendedFlowSteps || []).length">按 API 推荐生成整段流程</button>
      </div>

      <div v-if="(result.recommendedFlowSteps || []).length" style="margin-top:12px;">
        <div style="font-weight:700;">API 推荐动作链</div>
        <div v-for="(step, idx) in result.recommendedFlowSteps" :key="'flow-' + idx" class="card" style="margin-top:8px;">
          <div style="font-weight:700;">{{ idx + 1 }}. {{ step.type }}</div>
          <div class="muted">{{ step.data?.label || '-' }}</div>
        </div>
      </div>

      <div style="font-weight:700;margin-top:12px;">Selector 候选</div>
      <div v-if="!(result.selectors || []).length" class="muted" style="margin-top:6px;">暂无候选</div>
      <div v-for="(item, idx) in result.selectors || []" :key="idx" class="card" style="margin-top:8px;">
        <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
          <div style="min-width:0;">
            <div style="font-weight:700;word-break:break-all;">{{ item.selector }}</div>
            <div class="muted">{{ item.level }} / {{ item.source }} / score={{ item.score ?? '-' }}</div>
          </div>
          <div class="section-actions">
            <button class="btn secondary" @click="$emit('apply-selector', item.selector)">回填</button>
            <button class="btn" @click="$emit('create-node-with-selector', item.selector)">生成节点</button>
          </div>
        </div>
      </div>
    </div>

    <div style="margin-top:14px;">
      <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
        <div style="font-weight:700;">暂存队列（{{ queue.length }}）</div>
        <div class="section-actions">
          <button class="btn secondary" @click="$emit('clear-queue')" :disabled="!queue.length">清空</button>
          <button class="btn" @click="$emit('bulk-generate')" :disabled="!queue.length">批量生成节点</button>
          <button class="btn success" @click="$emit('bulk-generate-flow')" :disabled="!queue.length">批量生成小流程</button>
          <button class="btn success" @click="$emit('bulk-generate-api-flow')" :disabled="!queue.length">批量按 API 流程生成</button>
        </div>
      </div>

      <div v-if="!queue.length" class="muted" style="margin-top:8px;">暂无暂存元素。</div>

      <div v-for="(item, idx) in queue" :key="idx" class="card" style="margin-top:8px;">
        <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:8px;">
          <div style="min-width:0;">
            <div style="font-weight:700;">{{ idx + 1 }}. {{ item.recommendedNodeType || 'click' }}</div>
            <div class="muted">tag={{ item.element?.tagName || '-' }} / text={{ item.element?.text || '-' }}</div>
            <div class="muted" style="word-break:break-all;">{{ item.selectors?.[0]?.selector || item.element?.cssPath || '-' }}</div>
          </div>
          <div class="section-actions">
            <button class="btn secondary" @click="$emit('remove-queue-item', idx)">移除</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  sessionId: { type: String, default: '' },
  result: { type: Object, default: null },
  queue: { type: Array, default: () => [] },
  busy: { type: Boolean, default: false },
  continuousPick: { type: Boolean, default: false },
  autoLink: { type: Boolean, default: true }
})

defineEmits([
  'start',
  'stop',
  'apply-selector',
  'create-node-with-selector',
  'create-recommended-node',
  'create-mini-flow',
  'generate-api-flow',
  'stash-current',
  'clear-queue',
  'bulk-generate',
  'bulk-generate-flow',
  'bulk-generate-api-flow',
  'remove-queue-item',
  'toggle-continuous',
  'toggle-autolink'
])
</script>
