<template>
  <div>
    <div class="page-title">指纹模板</div>
    <div class="page-subtitle">支持新增、编辑、删除，并补全表单 label。</div>

    <div class="card" style="margin-bottom:12px;">
      <div class="section-actions">
        <button class="btn" @click="openCreate">新增指纹模板</button>
        <button class="btn secondary" @click="load">刷新</button>
      </div>
      <div v-if="message" class="muted" style="margin-top:8px;">{{ message }}</div>
    </div>

    <div class="card">
      <div style="font-weight:700;margin-bottom:10px;">模板列表</div>
      <div v-if="!items.length" class="muted">暂无模板</div>
      <div v-for="item in items" :key="item.id" class="card card-dark" style="margin-bottom:12px;">
        <div class="toolbar">
          <div style="font-weight:700;">{{ item.name }}</div>
          <div class="section-actions">
            <button class="btn secondary" @click="openEdit(item)">编辑</button>
            <button class="btn warn" @click="askDelete(item.id)">删除</button>
          </div>
        </div>
        <pre style="white-space:pre-wrap;margin-top:8px;">{{ item.configJson }}</pre>
      </div>
    </div>

    <div v-if="editorOpen" class="modal-mask" @click.self="editorOpen = false">
      <div class="modal-panel card">
        <div class="toolbar">
          <div style="font-weight:700;">{{ editingId ? '编辑指纹模板' : '新增指纹模板' }}</div>
          <button class="btn secondary" @click="editorOpen = false">关闭</button>
        </div>
        <div class="form-field" style="margin-top:12px;">
          <label>模板名称</label>
          <input class="input" v-model="form.name" />
        </div>
        <div class="form-field" style="margin-top:12px;">
          <label>指纹配置 JSON</label>
          <textarea class="input" v-model="form.configJson" rows="14"></textarea>
        </div>
        <div class="section-actions" style="margin-top:12px;">
          <button class="btn" @click="save">{{ editingId ? '保存修改' : '创建模板' }}</button>
        </div>
      </div>
    </div>

    <ConfirmDialog
      :open="deleteOpen"
      title="删除指纹模板"
      message="删除后无法恢复，且已绑定该模板的 Profile 可能受影响。"
      confirm-text="确认删除"
      @cancel="deleteOpen = false"
      @confirm="removeConfirmed"
    />
  </div>
</template>
<script setup>
import { reactive, ref, onMounted } from 'vue'
import { api } from '../services/api'
import ConfirmDialog from '../components/ConfirmDialog.vue'

const items = ref([]), message = ref(''), editorOpen = ref(false), editingId = ref(null), deleteOpen = ref(false), deleteId = ref(null)
const defaultJson = '{\n  "userAgent":"Mozilla/5.0",\n  "viewport":{"width":1366,"height":768},\n  "locale":"zh-CN",\n  "timezoneId":"Asia/Singapore"\n}'
const form = reactive({ name:'默认桌面', configJson: defaultJson })
async function load(){ items.value = await api.fingerprints() }
function openCreate(){ editingId.value = null; form.name='默认桌面'; form.configJson=defaultJson; editorOpen.value=true }
function openEdit(item){ editingId.value=item.id; form.name=item.name; form.configJson=item.configJson; editorOpen.value=true }
function askDelete(id){ deleteId.value = id; deleteOpen.value = true }
async function save(){ try{ editingId.value ? await api.updateFingerprint(editingId.value, form) : await api.createFingerprint(form); message.value='保存成功'; editorOpen.value=false; await load() } catch(err){ message.value = err.message || '保存失败' } }
async function removeConfirmed(){ try{ if (!deleteId.value) return; await api.deleteFingerprint(deleteId.value); message.value='模板已删除'; deleteOpen.value = false; deleteId.value = null; await load() } catch(err){ message.value = err.message || '删除失败' } }
onMounted(load)
</script>
