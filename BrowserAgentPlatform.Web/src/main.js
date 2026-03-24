import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import LoginView from './views/LoginView.vue'
import DashboardView from './views/DashboardView.vue'
import WorkflowBuilderView from './views/WorkflowBuilderView.vue'
import TemplatesView from './views/TemplatesView.vue'
import ProfilesView from './views/ProfilesView.vue'
import FingerprintsView from './views/FingerprintsView.vue'
import TasksView from './views/TasksView.vue'
import LiveView from './views/LiveView.vue'
import { auth } from './services/auth'

const routes = [
  { path: '/login', component: LoginView },
  { path: '/', component: DashboardView, meta: { auth: true } },
  { path: '/builder', component: WorkflowBuilderView, meta: { auth: true } },
  { path: '/templates', component: TemplatesView, meta: { auth: true } },
  { path: '/profiles', component: ProfilesView, meta: { auth: true } },
  { path: '/fingerprints', component: FingerprintsView, meta: { auth: true } },
  { path: '/tasks', component: TasksView, meta: { auth: true } },
  { path: '/live/:runId?', component: LiveView, meta: { auth: true } }
]

const router = createRouter({ history: createWebHistory(), routes })
router.beforeEach((to) => {
  if (to.meta.auth && !auth.token()) return '/login'
})
createApp(App).use(router).mount('#app')
