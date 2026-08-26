<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuickRepliesStore } from '@/stores/quickReplies'
import type { QuickReply } from '@/types/tickets'

const { t } = useI18n()
const store = useQuickRepliesStore()

const isAdding = ref(false)
const editingId = ref<string | null>(null)
const draftTitle = ref('')
const draftContent = ref('')
const draftIsActive = ref(true)

onMounted(() => {
  void store.fetch()
})

function openAddForm() {
  isAdding.value = true
  editingId.value = null
  draftTitle.value = ''
  draftContent.value = ''
  draftIsActive.value = true
}

function startEdit(reply: QuickReply) {
  isAdding.value = false
  editingId.value = reply.id
  draftTitle.value = reply.title
  draftContent.value = reply.content
  draftIsActive.value = reply.isActive
}

function cancelForm() {
  isAdding.value = false
  editingId.value = null
}

async function submitAdd() {
  const title = draftTitle.value.trim()
  const content = draftContent.value.trim()
  if (!title || !content) {
    return
  }
  try {
    await store.create({ title, content })
    isAdding.value = false
  } catch {
    // error surfaced via store.error
  }
}

async function submitEdit() {
  if (!editingId.value) {
    return
  }
  const title = draftTitle.value.trim()
  const content = draftContent.value.trim()
  if (!title || !content) {
    return
  }
  try {
    await store.update(editingId.value, { title, content, isActive: draftIsActive.value })
    editingId.value = null
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(reply: QuickReply) {
  if (!window.confirm(t('quickReplies.deleteConfirm'))) {
    return
  }
  try {
    await store.remove(reply.id)
  } catch {
    // error surfaced via store.error
  }
}
</script>

<template>
  <div class="quick-replies-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('quickReplies.title') }}</h1>
      </div>
      <button type="button" @click="openAddForm" :disabled="isAdding">
        {{ t('quickReplies.new') }}
      </button>
    </div>

    <p v-if="store.error" role="alert">{{ t(`quickReplies.errors.${store.error}`) }}</p>

    <form v-if="isAdding" class="surface quick-reply-form" @submit.prevent="submitAdd">
      <div class="field">
        <label for="quick-reply-title">{{ t('quickReplies.fields.title') }}</label>
        <input id="quick-reply-title" v-model="draftTitle" type="text" maxlength="120" />
      </div>
      <div class="field">
        <label for="quick-reply-content">{{ t('quickReplies.fields.content') }}</label>
        <textarea id="quick-reply-content" v-model="draftContent" maxlength="4000" rows="4"></textarea>
      </div>
      <div class="form-actions">
        <button type="submit" :disabled="store.saving || !draftTitle.trim() || !draftContent.trim()">
          {{ store.saving ? t('quickReplies.saving') : t('common.save') }}
        </button>
        <button type="button" @click="cancelForm">{{ t('common.cancel') }}</button>
      </div>
    </form>

    <p v-if="store.loading">{{ t('common.loading') }}</p>
    <div v-else-if="store.items.length === 0 && !isAdding" class="surface empty-state">
      <p>{{ t('quickReplies.empty') }}</p>
    </div>

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('quickReplies.fields.title') }}</th>
            <th>{{ t('quickReplies.fields.content') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="reply in store.items" :key="reply.id">
            <tr v-if="editingId === reply.id">
              <td colspan="3">
                <form class="quick-reply-inline-form" @submit.prevent="submitEdit">
                  <input v-model="draftTitle" type="text" maxlength="120" />
                  <textarea v-model="draftContent" maxlength="4000" rows="3"></textarea>
                  <label class="active-toggle">
                    <input type="checkbox" v-model="draftIsActive" />
                    {{ t('quickReplies.fields.isActive') }}
                  </label>
                  <div class="form-actions">
                    <button type="submit" :disabled="store.saving">
                      {{ store.saving ? t('quickReplies.saving') : t('common.save') }}
                    </button>
                    <button type="button" @click="cancelForm">{{ t('common.cancel') }}</button>
                  </div>
                </form>
              </td>
            </tr>
            <tr v-else>
              <td>{{ reply.title }}</td>
              <td class="truncate" :title="reply.content">{{ reply.content }}</td>
              <td>
                <button type="button" @click="startEdit(reply)">{{ t('quickReplies.edit') }}</button>
                <button type="button" @click="onDelete(reply)">{{ t('quickReplies.delete') }}</button>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.quick-replies-view {
  max-width: 60rem;
  margin: 4rem auto;
}

.quick-reply-form,
.quick-reply-inline-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 20px;
  margin-bottom: 18px;
}

.active-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.form-actions {
  display: flex;
  gap: 0.5rem;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  text-align: start;
  padding: 0.5rem;
}

.truncate {
  max-width: 24rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
