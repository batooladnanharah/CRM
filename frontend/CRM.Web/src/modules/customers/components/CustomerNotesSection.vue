<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCustomerNotesStore } from '@/stores/customerNotes'
import { useAuthStore } from '@/stores/auth'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import type { CustomerNote } from '@/types/customers'

const props = defineProps<{ customerId: string }>()

const { t } = useI18n()
const { locale } = useLocale()
const store = useCustomerNotesStore()
const authStore = useAuthStore()

const isAdding = ref(false)
const draftContent = ref('')
const editingNoteId = ref<string | null>(null)
const editContent = ref('')

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function canModify(note: CustomerNote): boolean {
  return authStore.isAdmin || authStore.user?.id === note.authorId
}

onMounted(() => {
  void store.fetchNotes(props.customerId)
})

function openAddForm() {
  isAdding.value = true
  draftContent.value = ''
}

function cancelAdd() {
  isAdding.value = false
  draftContent.value = ''
}

async function submitAdd() {
  const content = draftContent.value.trim()
  if (!content) {
    return
  }
  try {
    await store.addNote(props.customerId, content)
    isAdding.value = false
    draftContent.value = ''
  } catch {
    // error surfaced via store.error
  }
}

function startEdit(note: CustomerNote) {
  editingNoteId.value = note.id
  editContent.value = note.content
}

function cancelEdit() {
  editingNoteId.value = null
  editContent.value = ''
}

async function submitEdit(noteId: string) {
  const content = editContent.value.trim()
  if (!content) {
    return
  }
  try {
    await store.editNote(props.customerId, noteId, content)
    editingNoteId.value = null
    editContent.value = ''
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(noteId: string) {
  if (!window.confirm(t('customers.notes.deleteConfirm'))) {
    return
  }
  try {
    await store.removeNote(props.customerId, noteId)
  } catch {
    // error surfaced via store.error
  }
}
</script>

<template>
  <div class="customer-notes-section">
    <header class="notes-header">
      <h3>{{ t('customers.notes.title') }}</h3>
      <AppButton type="button" size="sm" @click="openAddForm" :disabled="isAdding">
        {{ t('customers.notes.addButton') }}
      </AppButton>
    </header>

    <p v-if="store.error" role="alert">{{ t(`customers.notes.errors.${store.error}`) }}</p>

    <form v-if="isAdding" class="note-form" @submit.prevent="submitAdd">
      <label :for="`new-note-content`">{{ t('customers.notes.contentLabel') }}</label>
      <textarea
        id="new-note-content"
        v-model="draftContent"
        maxlength="4000"
        rows="3"
      ></textarea>
      <div class="note-form-actions">
        <AppButton type="submit" size="sm" :disabled="store.saving || !draftContent.trim()">
          {{ t('customers.notes.save') }}
        </AppButton>
        <AppButton type="button" variant="secondary" size="sm" @click="cancelAdd">
          {{ t('customers.notes.cancel') }}
        </AppButton>
      </div>
    </form>

    <ul v-if="store.loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="store.notes.length === 0 && !isAdding">
      <p>{{ t('customers.notes.empty.title') }}</p>
      <p>{{ t('customers.notes.empty.hint') }}</p>
    </div>

    <ul v-else class="notes-list">
      <li v-for="note in store.notes" :key="note.id" class="note-item">
        <template v-if="editingNoteId === note.id">
          <form class="note-form" @submit.prevent="submitEdit(note.id)">
            <label :for="`edit-note-${note.id}`">{{ t('customers.notes.contentLabel') }}</label>
            <textarea
              :id="`edit-note-${note.id}`"
              v-model="editContent"
              maxlength="4000"
              rows="3"
            ></textarea>
            <div class="note-form-actions">
              <AppButton type="submit" size="sm" :disabled="store.saving || !editContent.trim()">
                {{ t('customers.notes.save') }}
              </AppButton>
              <AppButton type="button" variant="secondary" size="sm" @click="cancelEdit">
                {{ t('customers.notes.cancel') }}
              </AppButton>
            </div>
          </form>
        </template>
        <template v-else>
          <p class="note-meta">
            <span>{{ t('customers.notes.authorLine', { name: note.authorDisplayName }) }}</span>
            <span>{{ formatDate(note.createdAtUtc) }}</span>
            <span v-if="note.updatedAtUtc">{{ t('customers.notes.editedSuffix') }}</span>
          </p>
          <p class="note-content">{{ note.content }}</p>
          <div v-if="canModify(note)" class="note-actions">
            <AppButton type="button" variant="ghost" size="sm" @click="startEdit(note)">
              {{ t('customers.notes.edit') }}
            </AppButton>
            <AppButton type="button" variant="danger" size="sm" @click="onDelete(note.id)">
              {{ t('customers.notes.delete') }}
            </AppButton>
          </div>
        </template>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.notes-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notes-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.note-item {
  border-bottom: 1px solid var(--line);
  padding-bottom: var(--space-3);
}

.note-meta {
  display: flex;
  gap: var(--space-4);
  color: var(--muted);
  font-size: var(--font-size-xs);
  font-weight: 700;
}

.note-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin: var(--space-2) 0;
}

.note-form-actions,
.note-actions {
  display: flex;
  gap: var(--space-2);
}

.skeleton {
  list-style: none;
  padding: 0;
}

.skeleton li {
  height: 2rem;
  margin-bottom: var(--space-2);
  background: var(--canvas);
  border-radius: var(--radius-sm);
}
</style>
