<template>
  <div class="form-field">
    <label>
      {{ label }}
      <span v-if="required" style="color:#f87171;">*</span>
    </label>
    <slot />
    <div v-if="help" class="help">{{ help }}</div>
    <div v-if="error" style="color:#fca5a5;font-size:12px;line-height:1.5;">{{ error }}</div>
  </div>
</template>

<script setup>
defineProps({
  label: { type: String, required: true },
  help: { type: String, default: '' },
  required: { type: Boolean, default: false },
  error: { type: String, default: '' }
})
</script>
