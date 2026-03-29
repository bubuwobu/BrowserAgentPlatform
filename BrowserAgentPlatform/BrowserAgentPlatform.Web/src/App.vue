<template>
  <div class="layout">
    <aside v-if="loggedIn" class="sidebar">
      <div class="brand">
        <div class="brand-title">BrowserAgentPlatform</div>
        <div class="brand-subtitle">任务编排 + 调度 + 账号绑定</div>
      </div>

      <nav class="nav">
        <RouterLink to="/" class="nav-item">Dashboard</RouterLink>
        <RouterLink to="/builder" class="nav-item">编排器</RouterLink>
        <RouterLink to="/templates" class="nav-item">模板中心</RouterLink>
        <RouterLink to="/profiles" class="nav-item">Profiles</RouterLink>
        <RouterLink to="/fingerprints" class="nav-item">指纹模板</RouterLink>
        <RouterLink to="/accounts" class="nav-item">账号中心</RouterLink>
        <RouterLink to="/workbench" class="nav-item">闭环工作台</RouterLink>
        <RouterLink to="/tasks" class="nav-item">任务中心</RouterLink>
        <RouterLink to="/live" class="nav-item">Live 调试</RouterLink>
      </nav>

      <button class="logout-btn" @click="logout">退出登录</button>
    </aside>

    <main class="main">
      <header v-if="loggedIn" class="topbar">
        <div>
          <div class="topbar-title">控制台</div>
          <div class="topbar-subtitle">第三阶段：继续补交互、确认、调度稳态和编排器易用性</div>
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
label { display:block; font-size:13px; color:#cbd5e1; margin-bottom:6px; }
.help { color:#94a3b8; font-size:12px; margin-top:4px; line-height:1.5; }
.form-field { display:grid; gap:6px; }
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
.btn.ghost { background: transparent; border: 1px solid #334155; }
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
.badge.running, .badge.online, .badge.completed, .badge.info, .badge.active {
  background: rgba(34,197,94,.15);
  color: #4ade80;
}
.badge.queued, .badge.leased, .badge.idle, .badge.warn {
  background: rgba(59,130,246,.15);
  color: #60a5fa;
}
.badge.failed, .badge.offline, .badge.cancelled, .badge.error, .badge.disabled {
  background: rgba(239,68,68,.15);
  color: #f87171;
}
.section-actions { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.muted { color: #94a3b8; font-size: 13px; }
.toolbar { display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap; }
.modal-mask {
  position: fixed;
  inset: 0;
  background: rgba(2, 6, 23, 0.72);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 40;
}
.modal-panel {
  width: min(1000px, 96vw);
  max-height: 88vh;
  overflow: auto;
}
.kv-grid {
  display:grid;
  grid-template-columns:repeat(2,minmax(0,1fr));
  gap:10px 14px;
}
@media (max-width: 900px) {
  .kv-grid { grid-template-columns:1fr; }
}
</style>
