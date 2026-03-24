<template>
  <div style="max-width:420px;margin:80px auto;" class="card">
    <h2>登录</h2>
    <div class="grid">
      <input v-model="form.username" placeholder="用户名" />
      <input v-model="form.password" type="password" placeholder="密码" />
      <button class="btn" @click="submit">登录</button>
      <div style="color:#93c5fd;">默认：admin / Admin@123456</div>
      <div v-if="error" style="color:#fca5a5;">{{ error }}</div>
    </div>
  </div>
</template>
<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../services/api'
import { auth } from '../services/auth'
const router = useRouter()
const form = reactive({ username: 'admin', password: 'Admin@123456' })
const error = ref('')
async function submit() {
  try {
    const res = await api.login(form)
    auth.set(res.token)
    router.push('/')
  } catch (e) {
    error.value = String(e)
  }
}
</script>
