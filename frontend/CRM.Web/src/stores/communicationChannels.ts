import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createChannel,
  deleteChannel,
  ingestChannelEmail,
  listChannelEmails,
  listChannels,
  updateChannel,
} from '@/api/communicationChannels'
import type {
  Channel,
  CreateChannelPayload,
  EmailMessage,
  IngestEmailPayload,
  UpdateChannelPayload,
} from '@/types/communicationChannels'

export const useCommunicationChannelsStore = defineStore('communicationChannels', () => {
  const channels = ref<Channel[]>([])
  const selectedChannelId = ref<string | null>(null)
  const emails = ref<EmailMessage[]>([])
  const loading = ref(false)
  const emailsLoading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchChannels() {
    loading.value = true
    error.value = null

    try {
      channels.value = await listChannels()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateChannelPayload) {
    saving.value = true
    error.value = null

    try {
      const created = await createChannel(payload)
      channels.value = [...channels.value, created].sort((a, b) => a.name.localeCompare(b.name))
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateChannelPayload) {
    saving.value = true
    error.value = null

    try {
      const updated = await updateChannel(id, payload)
      channels.value = channels.value.map((c) => (c.id === id ? updated : c))
      return updated
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    saving.value = true
    error.value = null

    try {
      await deleteChannel(id)
      channels.value = channels.value.filter((c) => c.id !== id)
      if (selectedChannelId.value === id) {
        selectedChannelId.value = null
        emails.value = []
      }
    } catch (err) {
      error.value = 'errorDelete'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function selectChannel(id: string) {
    selectedChannelId.value = id
    emailsLoading.value = true
    error.value = null

    try {
      emails.value = await listChannelEmails(id)
    } catch {
      error.value = 'errorLoadEmails'
    } finally {
      emailsLoading.value = false
    }
  }

  async function ingestEmail(id: string, payload: IngestEmailPayload) {
    error.value = null

    try {
      const created = await ingestChannelEmail(id, payload)
      if (selectedChannelId.value === id) {
        emails.value = [created, ...emails.value]
      }
      return created
    } catch (err) {
      error.value = 'errorIngest'
      throw err
    }
  }

  return {
    channels,
    selectedChannelId,
    emails,
    loading,
    emailsLoading,
    saving,
    error,
    fetchChannels,
    create,
    update,
    remove,
    selectChannel,
    ingestEmail,
  }
})
