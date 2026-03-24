<template>
  <div class="layout">
    <aside v-if="loggedIn" class="sidebar">
      <div class="brand">
        <div class="brand-title">BrowserAgentPlatform</div>
        <div class="brand-subtitle">Web 控制 + Agent 执行</div>
      </div>

      <nav class="nav">
        <RouterLink to="/" class="nav-item">Dashboard</RouterLink>
        <RouterLink to="/builder" class="nav-item">编排器</RouterLink>
        <RouterLink to="/templates" class="nav-item">模板中心</RouterLink>
        <RouterLink to="/profiles" class="nav-item">Profiles</RouterLink>
        <RouterLink to="/fingerprints" class="nav-item">指纹模板</RouterLink>
        <RouterLink to="/tasks" class="nav-item">任务中心</RouterLink>
        <RouterLink to="/live" class="nav-item">Live 调试</RouterLink>
      </nav>

      <button class="logout-btn" @click="logout">退出登录</button>
    </aside>

    <main class="main">
      <header v-if="loggedIn" class="topbar">
        <div>
          <div class="topbar-title">控制台</div>
          <div class="topbar-subtitle">当前版本先以闭环跑通和可视化为第一优先级</div>
        </div>
      </header>

      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { auth } from './services/auth'

const router = useRouter()
const loggedIn = computed(() => !!auth.token())

function logout() {
  auth.clear()
  router.push('/login')
}
</script>

<style>
:root { color-scheme: dark; }
* { box-sizing: border-box; }
body {
  margin: 0;
  background: #020617;
  color: #e2e8f0;
  font-family: Inter, "Microsoft YaHei", sans-serif;
}
a { color: inherit; text-decoration: none; }
.layout { display: flex; min-height: 100vh; }
.sidebar {
  width: 250px;
  background: #0f172a;
  border-right: 1px solid #1e293b;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.brand { padding-bottom: 12px; border-bottom: 1px solid #1e293b; }
.brand-title { font-size: 20px; font-weight: 700; }
.brand-subtitle { margin-top: 6px; font-size: 12px; color: #94a3b8; }
.nav { display: grid; gap: 8px; }
.nav-item {
  padding: 10px 12px;
  border-radius: 10px;
  color: #cbd5e1;
  background: transparent;
}
.nav-item.router-link-active {
  background: #1d4ed8;
  color: white;
}
.logout-btn {
  margin-top: auto;
  border: 0;
  border-radius: 10px;
  background: #334155;
  color: white;
  padding: 10px 12px;
  cursor: pointer;
}
.main { flex: 1; padding: 24px; }
.topbar { margin-bottom: 20px; }
.topbar-title { font-size: 24px; font-weight: 700; }
.topbar-subtitle { margin-top: 6px; font-size: 13px; color: #94a3b8; }
.page-title { font-size: 24px; font-weight: 700; margin-bottom: 8px; }
.page-subtitle { font-size: 13px; color: #94a3b8; margin-bottom: 20px; }
.grid { display: grid; gap: 16px; }
.card {
  background: #0f172a;
  border: 1px solid #1e293b;
  border-radius: 16px;
  padding: 16px;
}
.card-dark { background: #020617; }
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 0;
  border-radius: 10px;
  background: #2563eb;
  color: white;
  padding: 10px 14px;
  cursor: pointer;
}
.btn.secondary { background: #334155; }
.btn.warn { background: #ea580c; }
.btn.success { background: #16a34a; }
.input, textarea, select {
  width: 100%;
  background: #020617;
  border: 1px solid #334155;
  color: white;
  border-radius: 10px;
  padding: 10px 12px;
}
.badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
}
.badge.running, .badge.online, .badge.completed, .badge.info {
  background: rgba(34,197,94,.15);
  color: #4ade80;
}
.badge.queued, .badge.leased, .badge.idle, .badge.warn {
  background: rgba(59,130,246,.15);
  color: #60a5fa;
}
.badge.failed, .badge.offline, .badge.cancelled, .badge.error {
  background: rgba(239,68,68,.15);
  color: #f87171;
}
.section-actions { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.muted { color: #94a3b8; font-size: 13px; }
</style>
