<template>
  <div v-if="open" class="modal-mask" @click.self="$emit('cancel')">
    <div class="modal-panel card" style="width:min(520px,96vw);">
      <div style="font-size:18px;font-weight:700;">{{ title }}</div>
      <div class="muted" style="margin-top:8px; white-space:pre-wrap; line-height:1.6;">{{ message }}</div>
      <div class="section-actions" style="margin-top:16px; justify-content:flex-end;">
        <button class="btn secondary" @click="$emit('cancel')">取消</button>
        <button class="btn warn" @click="$emit('confirm')">{{ confirmText }}</button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: '确认操作' },
  message: { type: String, default: '' },
  confirmText: { type: String, default: '确认' }
})
defineEmits(['cancel', 'confirm'])
</script>
