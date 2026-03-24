<template>
  <div style="display:flex;min-height:100vh;">
    <aside v-if="loggedIn" style="width:240px;background:#020617;padding:18px;border-right:1px solid #1f2937;">
      <div style="font-weight:700;font-size:20px;margin-bottom:20px;">BrowserAgentPlatform</div>
      <nav class="grid">
        <RouterLink to="/">Dashboard</RouterLink>
        <RouterLink to="/builder">编排器</RouterLink>
        <RouterLink to="/templates">模板中心</RouterLink>
        <RouterLink to="/profiles">Profiles</RouterLink>
        <RouterLink to="/fingerprints">指纹模板</RouterLink>
        <RouterLink to="/tasks">任务中心</RouterLink>
        <RouterLink to="/live">Live 调试</RouterLink>
      </nav>
    </aside>
    <main style="flex:1;padding:24px;">
      <RouterView />
    </main>
  </div>
</template>
<script setup>
import { computed } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { auth } from './services/auth'
const loggedIn = computed(() => !!auth.token())
</script>
