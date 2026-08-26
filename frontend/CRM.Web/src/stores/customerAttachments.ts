import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  deleteCustomerAttachment,
  downloadCustomerAttachment,
  listCustomerAttachments,
  uploadCustomerAttachment,
} from '@/api/customerAttachments'
import { ApiError } from '@/api/http'
import type { CustomerAttachment } from '@/types/customers'

export const useCustomerAttachmentsStore = defineStore('customerAttachments', () => {
  const items = ref<CustomerAttachment[]>([])
  const loading = ref(false)
  const uploading = ref(false)
  const deletingId = ref<string | null>(null)
  const error = ref<string | null>(null)

  async function fetchAttachments(customerId: string) {
    loading.value = true
    error.value = null

    try {
      items.value = await listCustomerAttachments(customerId)
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function upload(customerId: string, file: File) {
    if (uploading.value) {
      return
    }

    uploading.value = true
    error.value = null

    try {
      const created = await uploadCustomerAttachment(customerId, file)
      items.value = [created, ...items.value]
      return created
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        error.value = err.message
      } else {
        error.value = 'errorUpload'
      }
      throw err
    } finally {
      uploading.value = false
    }
  }

  async function remove(customerId: string, attachmentId: string) {
    deletingId.value = attachmentId
    error.value = null

    try {
      await deleteCustomerAttachment(customerId, attachmentId)
      items.value = items.value.filter((a) => a.id !== attachmentId)
    } catch (err) {
      error.value = 'errorDelete'
      throw err
    } finally {
      deletingId.value = null
    }
  }

  async function download(customerId: string, attachment: CustomerAttachment) {
    error.value = null

    try {
      const blob = await downloadCustomerAttachment(customerId, attachment.id)
      const url = URL.createObjectURL(blob)
      try {
        const link = document.createElement('a')
        link.href = url
        link.download = attachment.originalFileName
        link.click()
      } finally {
        URL.revokeObjectURL(url)
      }
    } catch (err) {
      error.value = 'errorDownload'
      throw err
    }
  }

  return { items, loading, uploading, deletingId, error, fetchAttachments, upload, remove, download }
})
