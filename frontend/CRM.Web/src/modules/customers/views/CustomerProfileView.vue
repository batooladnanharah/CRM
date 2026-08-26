<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useLocale } from '@/composables/useLocale'
import CustomerInteractionTimeline from '@/modules/customers/components/CustomerInteractionTimeline.vue'
import CustomerNotesSection from '@/modules/customers/components/CustomerNotesSection.vue'
import CustomerAttachmentsSection from '@/modules/customers/components/CustomerAttachmentsSection.vue'

const { t } = useI18n()
const { locale } = useLocale()
const route = useRoute()
const router = useRouter()
const store = useCustomersStore()

const id = (route.params.id as string | undefined)?.trim() ?? ''

type ProfileTab = 'overview' | 'interactions' | 'notes' | 'attachments'
const activeTab = ref<ProfileTab>('overview')

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string | null | undefined): string {
  if (!value) {
    return t('customers.profile.missing')
  }
  return dateFormatter.value.format(new Date(value))
}

function loadCustomer() {
  if (!id) {
    return
  }
  void store.getById(id)
}

onMounted(() => {
  loadCustomer()
})

onBeforeUnmount(() => {
  store.clearCurrent()
})

function onEdit() {
  router.push({ name: 'customer-edit', params: { id } })
}

function onBack() {
  router.push({ name: 'customers' })
}

function onRetry() {
  loadCustomer()
}
</script>

<template>
  <div class="customer-profile-view">
    <h1>{{ t('customers.profile.title') }}</h1>

    <p v-if="store.loadingCurrent">{{ t('customers.loading') }}</p>

    <div v-else-if="!id || store.notFound">
      <p>{{ t('customers.profile.notFoundTitle') }}</p>
      <p>{{ t('customers.profile.notFoundBody') }}</p>
      <button type="button" @click="onBack">{{ t('customers.profile.back') }}</button>
    </div>

    <div v-else-if="store.loadError" role="alert">
      <p>{{ t('customers.profile.errorLoadTitle') }}</p>
      <p>{{ t('customers.profile.errorLoadBody') }}</p>
      <button type="button" @click="onRetry">{{ t('customers.profile.retry') }}</button>
    </div>

    <div v-else-if="store.current">
      <header class="profile-header">
        <div>
          <h2>{{ store.current.fullName }}</h2>
          <p>{{ store.current.email }}</p>
        </div>
        <button type="button" @click="onEdit">{{ t('customers.profile.edit') }}</button>
      </header>

      <nav class="profile-tabs">
        <button
          type="button"
          :class="{ active: activeTab === 'overview' }"
          @click="activeTab = 'overview'"
        >
          {{ t('customers.profile.overview') }}
        </button>
        <button type="button" disabled :title="t('customers.profile.comingSoon')">
          {{ t('customers.profile.tabTickets') }}
        </button>
        <button
          type="button"
          :class="{ active: activeTab === 'interactions' }"
          @click="activeTab = 'interactions'"
        >
          {{ t('customers.interactions.tab') }}
        </button>
        <button
          type="button"
          :class="{ active: activeTab === 'notes' }"
          @click="activeTab = 'notes'"
        >
          {{ t('customers.profile.tabNotes') }}
        </button>
        <button
          type="button"
          :class="{ active: activeTab === 'attachments' }"
          @click="activeTab = 'attachments'"
        >
          {{ t('customers.profile.tabAttachments') }}
        </button>
      </nav>

      <template v-if="activeTab === 'overview'">
        <section class="surface profile-section">
          <h3>{{ t('customers.profile.contactInformation') }}</h3>
          <div class="profile-details"><div><span class="detail-label">{{ t('customers.profile.email') }}</span><p class="detail-value">{{ store.current.email || t('customers.profile.missing') }}</p></div>
          <div><span class="detail-label">{{ t('customers.profile.phone') }}</span><p class="detail-value">{{ store.current.phone || t('customers.profile.missing') }}</p></div>
          <div><span class="detail-label">{{ t('customers.profile.company') }}</span><p class="detail-value">{{ store.current.company || t('customers.profile.missing') }}</p></div></div>
        </section>

        <section class="surface profile-section">
          <p>{{ t('customers.profile.customerSince') }}: {{ formatDate(store.current.createdAtUtc) }}</p>
        </section>
      </template>

      <CustomerInteractionTimeline v-else-if="activeTab === 'interactions'" :customer-id="id" />

      <CustomerNotesSection v-else-if="activeTab === 'notes'" :customer-id="id" />

      <CustomerAttachmentsSection v-else-if="activeTab === 'attachments'" :customer-id="id" />

      <button type="button" @click="onBack">{{ t('customers.profile.back') }}</button>
    </div>
  </div>
</template>

<style scoped>
.customer-profile-view {
  max-width: 40rem;
  margin: 4rem auto;
}

.profile-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.profile-tabs {
  display: flex;
  gap: 0.5rem;
  margin: 1rem 0;
}

.profile-tabs .active {
  font-weight: bold;
}
</style>
