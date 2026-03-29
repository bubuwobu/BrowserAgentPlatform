
<template>
  <div class="card card-dark">
    <div class="toolbar">
      <div>
        <div style="font-weight:700;">元素选择助手</div>
        <div class="muted">根据元素信息自动生成 selector 候选。</div>
      </div>
      <div class="section-actions">
        <button class="btn secondary" @click="generate">生成候选</button>
      </div>
    </div>

    <div class="grid" style="grid-template-columns:1fr 1fr; gap:10px; margin-top:12px;">
      <div class="form-field">
        <label>目标页面 URL</label>
        <input class="input" v-model="form.url" placeholder="http://localhost:3000/login" />
      </div>
      <div class="form-field">
        <label>元素文本</label>
        <input class="input" v-model="form.text" placeholder="例如：登录 / 提交评论" />
      </div>
      <div class="form-field">
        <label>data-testid</label>
        <input class="input" v-model="form.testid" placeholder="例如：post-comment-input" />
      </div>
      <div class="form-field">
        <label>id</label>
        <input class="input" v-model="form.id" placeholder="例如：login-btn" />
      </div>
      <div class="form-field">
        <label>name</label>
        <input class="input" v-model="form.name" placeholder="例如：username" />
      </div>
      <div class="form-field">
        <label>aria-label</label>
        <input class="input" v-model="form.ariaLabel" placeholder="例如：搜索" />
      </div>
      <div class="form-field">
        <label>class</label>
        <input class="input" v-model="form.className" placeholder="例如：btn primary" />
      </div>
      <div class="form-field">
        <label>tag</label>
        <input class="input" v-model="form.tag" placeholder="例如：button / input" />
      </div>
    </div>

    <div style="margin-top:12px;">
      <div style="font-weight:700;">候选 selector</div>
      <div v-if="!candidates.length" class="muted" style="margin-top:8px;">暂无候选。填一些元素信息后点击“生成候选”。</div>
      <div v-for="(item, idx) in candidates" :key="idx" class="card" style="margin-top:10px;">
        <div class="toolbar">
          <div>
            <div style="font-weight:700;">{{ item.selector }}</div>
            <div class="muted">推荐级别：{{ item.level }} / 来源：{{ item.from }}</div>
          </div>
          <div class="section-actions">
            <button class="btn" @click="$emit('apply', item.selector)">回填 selector</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'

const emit = defineEmits(['apply'])

const form = reactive({
  url: '',
  text: '',
  testid: '',
  id: '',
  name: '',
  ariaLabel: '',
  className: '',
  tag: ''
})

const candidates = ref([])

function cssEscape(value) {
  return String(value || '').trim().replace(/"/g, '\\"')
}

function generate() {
  const arr = []

  if (form.testid.trim()) {
    arr.push({ selector: `[data-testid="${cssEscape(form.testid)}"]`, level: '高', from: 'data-testid' })
  }
  if (form.id.trim()) {
    arr.push({ selector: `#${cssEscape(form.id)}`, level: '高', from: 'id' })
  }
  if (form.name.trim() && form.tag.trim()) {
    arr.push({ selector: `${form.tag.trim()}[name="${cssEscape(form.name)}"]`, level: '高', from: 'tag+name' })
  }
  if (form.name.trim()) {
    arr.push({ selector: `[name="${cssEscape(form.name)}"]`, level: '中', from: 'name' })
  }
  if (form.ariaLabel.trim()) {
    arr.push({ selector: `[aria-label="${cssEscape(form.ariaLabel)}"]`, level: '中', from: 'aria-label' })
  }
  if (form.className.trim() && form.tag.trim()) {
    const classSelector = form.className.trim().split(/\s+/).filter(Boolean).map(x => `.${x}`).join('')
    arr.push({ selector: `${form.tag.trim()}${classSelector}`, level: '中', from: 'tag+class' })
  }
  if (form.className.trim()) {
    const classSelector = form.className.trim().split(/\s+/).filter(Boolean).map(x => `.${x}`).join('')
    arr.push({ selector: classSelector, level: '低', from: 'class' })
  }
  if (form.text.trim()) {
    arr.push({ selector: `text=${form.text.trim()}`, level: '中', from: 'text' })
  }
  if (form.tag.trim() && form.text.trim()) {
    arr.push({ selector: `${form.tag.trim()}:has-text("${cssEscape(form.text)}")`, level: '中', from: 'tag+text' })
  }
  if (form.tag.trim()) {
    arr.push({ selector: form.tag.trim(), level: '低', from: 'tag' })
  }

  const dedup = []
  const seen = new Set()
  for (const item of arr) {
    if (!seen.has(item.selector)) {
      seen.add(item.selector)
      dedup.push(item)
    }
  }
  candidates.value = dedup
}
</script>
