<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTicketAttachmentsStore } from '@/stores/ticketAttachments'
import { useLocale } from '@/composables/useLocale'
import type { TicketAttachment } from '@/types/tickets'

const props = defineProps<{ ticketId: string }>()

const { t, te } = useI18n()
const { locale } = useLocale()
const store = useTicketAttachmentsStore()

const isUploadOpen = ref(false)
const selectedFile = ref<File | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) {
    return t('tickets.attachments.fileSize.mb', { value: (bytes / (1024 * 1024)).toFixed(1) })
  }
  return t('tickets.attachments.fileSize.kb', { value: Math.max(1, Math.round(bytes / 1024)) })
}

const errorText = computed(() => {
  if (!store.error) {
    return null
  }
  const key = `tickets.attachments.errors.${store.error}`
  return te(key) ? t(key) : store.error
})

onMounted(() => {
  void store.fetchAttachments(props.ticketId)
})

function openUpload() {
  isUploadOpen.value = true
  selectedFile.value = null
}

function cancelUpload() {
  isUploadOpen.value = false
  selectedFile.value = null
}

function onFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  selectedFile.value = target.files?.[0] ?? null
}

async function submitUpload() {
  if (!selectedFile.value) {
    return
  }
  try {
    await store.upload(props.ticketId, selectedFile.value)
    isUploadOpen.value = false
    selectedFile.value = null
    if (fileInput.value) {
      fileInput.value.value = ''
    }
  } catch {
    // error surfaced via store.error
  }
}

async function onDownload(attachment: TicketAttachment) {
  try {
    await store.download(props.ticketId, attachment)
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(attachment: TicketAttachment) {
  if (!window.confirm(t('tickets.attachments.confirmDelete'))) {
    return
  }
  try {
    await store.remove(props.ticketId, attachment.id)
  } catch {
    // error surfaced via store.error
  }
}
</script>

<template>
  <div class="ticket-attachments-section">
    <header class="attachments-header">
      <h3>{{ t('tickets.attachments.title') }}</h3>
      <button type="button" @click="openUpload" :disabled="isUploadOpen">
        {{ t('tickets.attachments.uploadButton') }}
      </button>
    </header>

    <p v-if="errorText" role="alert">{{ errorText }}</p>

    <form v-if="isUploadOpen" class="upload-form" @submit.prevent="submitUpload">
      <input ref="fileInput" type="file" @change="onFileChange" />
      <p v-if="selectedFile">
        {{ selectedFile.name }} — {{ formatFileSize(selectedFile.size) }} ({{ selectedFile.type || '—' }})
      </p>
      <div class="upload-form-actions">
        <button type="submit" :disabled="store.uploading || !selectedFile">
          {{ t('tickets.attachments.uploadButton') }}
        </button>
        <button type="button" @click="cancelUpload">{{ t('tickets.messages.cancel') }}</button>
      </div>
    </form>

    <ul v-if="store.loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="store.items.length === 0 && !isUploadOpen">
      <p>{{ t('tickets.attachments.emptyState') }}</p>
    </div>

    <ul v-else class="attachments-list">
      <li v-for="attachment in store.items" :key="attachment.id" class="attachment-item">
        <p class="attachment-name">{{ attachment.originalFileName }}</p>
        <p class="attachment-meta">
          <span>{{ formatFileSize(attachment.fileSize) }}</span>
          <span>{{ t('tickets.attachments.uploadedBy', { name: attachment.uploadedByDisplayName }) }}</span>
          <span>{{ formatDate(attachment.createdAtUtc) }}</span>
        </p>
        <div class="attachment-actions">
          <button
            type="button"
            :disabled="store.deletingId === attachment.id"
            @click="onDownload(attachment)"
          >
            {{ t('tickets.attachments.download') }}
          </button>
          <button
            type="button"
            :disabled="store.deletingId === attachment.id"
            @click="onDelete(attachment)"
          >
            {{ t('tickets.attachments.delete') }}
          </button>
        </div>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.attachments-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.attachments-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.attachment-item {
  border-bottom: 1px solid #eee;
  padding-bottom: 0.75rem;
}

.attachment-name {
  font-weight: bold;
}

.attachment-meta {
  display: flex;
  gap: 1rem;
}

.upload-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0.5rem 0;
}

.upload-form-actions,
.attachment-actions {
  display: flex;
  gap: 0.5rem;
}

.skeleton {
  list-style: none;
  padding: 0;
}

.skeleton li {
  height: 2rem;
  margin-bottom: 0.5rem;
  background: #eee;
}
</style>
