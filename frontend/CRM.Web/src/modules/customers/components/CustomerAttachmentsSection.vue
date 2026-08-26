<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCustomerAttachmentsStore } from '@/stores/customerAttachments'
import { useLocale } from '@/composables/useLocale'
import type { CustomerAttachment } from '@/types/customers'

const props = defineProps<{ customerId: string }>()

const { t, te } = useI18n()
const { locale } = useLocale()
const store = useCustomerAttachmentsStore()

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
    return t('customers.attachments.fileSize.mb', { value: (bytes / (1024 * 1024)).toFixed(1) })
  }
  return t('customers.attachments.fileSize.kb', { value: Math.max(1, Math.round(bytes / 1024)) })
}

const errorText = computed(() => {
  if (!store.error) {
    return null
  }
  const key = `customers.attachments.errors.${store.error}`
  return te(key) ? t(key) : store.error
})

onMounted(() => {
  void store.fetchAttachments(props.customerId)
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
    await store.upload(props.customerId, selectedFile.value)
    isUploadOpen.value = false
    selectedFile.value = null
    if (fileInput.value) {
      fileInput.value.value = ''
    }
  } catch {
    // error surfaced via store.error
  }
}

async function onDownload(attachment: CustomerAttachment) {
  try {
    await store.download(props.customerId, attachment)
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(attachment: CustomerAttachment) {
  if (!window.confirm(t('customers.attachments.confirmDelete'))) {
    return
  }
  try {
    await store.remove(props.customerId, attachment.id)
  } catch {
    // error surfaced via store.error
  }
}
</script>

<template>
  <div class="customer-attachments-section">
    <header class="attachments-header">
      <h3>{{ t('customers.attachments.title') }}</h3>
      <button type="button" @click="openUpload" :disabled="isUploadOpen">
        {{ t('customers.attachments.uploadButton') }}
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
          {{ t('customers.attachments.uploadButton') }}
        </button>
        <button type="button" @click="cancelUpload">{{ t('customers.notes.cancel') }}</button>
      </div>
    </form>

    <ul v-if="store.loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="store.items.length === 0 && !isUploadOpen">
      <p>{{ t('customers.attachments.emptyState') }}</p>
    </div>

    <ul v-else class="attachments-list">
      <li v-for="attachment in store.items" :key="attachment.id" class="attachment-item">
        <p class="attachment-name">{{ attachment.originalFileName }}</p>
        <p class="attachment-meta">
          <span>{{ formatFileSize(attachment.fileSize) }}</span>
          <span>{{ t('customers.attachments.uploadedBy', { name: attachment.uploadedByDisplayName }) }}</span>
          <span>{{ formatDate(attachment.createdAtUtc) }}</span>
        </p>
        <div class="attachment-actions">
          <button
            type="button"
            :disabled="store.deletingId === attachment.id"
            @click="onDownload(attachment)"
          >
            {{ t('customers.attachments.download') }}
          </button>
          <button
            type="button"
            :disabled="store.deletingId === attachment.id"
            @click="onDelete(attachment)"
          >
            {{ t('customers.attachments.delete') }}
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
  gap: var(--space-4);
}

.attachment-item {
  border-bottom: 1px solid var(--line);
  padding-bottom: var(--space-3);
}

.attachment-name {
  font-weight: 700;
}

.attachment-meta {
  display: flex;
  gap: var(--space-4);
  color: var(--muted);
  font-size: var(--font-size-xs);
}

.upload-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin: var(--space-2) 0;
}

.upload-form-actions,
.attachment-actions {
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
