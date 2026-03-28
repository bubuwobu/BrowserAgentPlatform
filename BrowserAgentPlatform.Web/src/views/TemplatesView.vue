<template>
  <div>
    <div class="page-title">模板中心</div>
    <div class="page-subtitle">模板列表优先展示，支持编辑与删除。</div>

    <div class="card" style="margin-bottom:12px;">
      <div class="section-actions">
        <button class="btn" @click="openCreate">新增模板</button>
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
            <button class="btn warn" @click="remove(item.id)">删除</button>
          </div>
        </div>
        <pre style="white-space:pre-wrap;margin-top:8px;">{{ item.definitionJson }}</pre>
      </div>
    </div>

    <div v-if="editorOpen" class="modal-mask" @click.self="editorOpen = false">
      <div class="modal-panel card">
        <div class="toolbar">
          <div style="font-weight:700;">{{ editingId ? '编辑模板' : '新增模板' }}</div>
          <button class="btn secondary" @click="editorOpen = false">关闭</button>
        </div>
        <div class="form-field" style="margin-top:12px;">
          <label>模板名称</label>
          <input class="input" v-model="form.name" placeholder="例如：日常浏览模板" />
        </div>
        <div class="form-field" style="margin-top:12px;">
          <label>模板定义 JSON</label>
          <textarea class="input" v-model="form.definitionJson" rows="14"></textarea>
        </div>
        <div class="section-actions" style="margin-top:12px;">
          <button class="btn" @click="save">{{ editingId ? '保存修改' : '创建模板' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted, reactive } from 'vue'
import { api } from '../services/api'
const items = ref([]), message = ref(''), editorOpen = ref(false), editingId = ref(null)
const form = reactive({ name:'示例模板', definitionJson:'{"steps":[],"edges":[]}' })
async function load(){ items.value = await api.templates() }
function openCreate(){ editingId.value = null; form.name='示例模板'; form.definitionJson='{"steps":[],"edges":[]}'; editorOpen.value=true }
function openEdit(item){ editingId.value=item.id; form.name=item.name; form.definitionJson=item.definitionJson; editorOpen.value=true }
async function save(){ try{ editingId.value ? await api.updateTemplate(editingId.value, form) : await api.createTemplate(form); message.value='模板保存成功'; editorOpen.value=false; await load() } catch(err){ message.value = err.message || '保存失败' } }
async function remove(id){ try{ await api.deleteTemplate(id); message.value='模板已删除'; await load() } catch(err){ message.value=err.message || '删除失败' } }
onMounted(load)
</script>
