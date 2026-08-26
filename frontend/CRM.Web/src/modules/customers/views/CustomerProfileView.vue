<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useLocale } from '@/composables/useLocale'
import CustomerInteractionTimeline from '@/modules/customers/components/CustomerInteractionTimeline.vue'
import CustomerNotesSection from '@/modules/customers/components/CustomerNotesSection.vue'
import CustomerAttachmentsSection from '@/modules/customers/components/CustomerAttachmentsSection.vue'
import AppButton from '@/components/ui/AppButton.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

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

    <LoadingState v-if="store.loadingCurrent" :label="t('customers.loading')" />

    <div v-else-if="!id || store.notFound" class="surface state-card">
      <p class="text-heading-3">{{ t('customers.profile.notFoundTitle') }}</p>
      <p class="text-body">{{ t('customers.profile.notFoundBody') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('customers.profile.back') }}</AppButton>
    </div>

    <div v-else-if="store.loadError" class="surface state-card" role="alert">
      <p class="text-heading-3">{{ t('customers.profile.errorLoadTitle') }}</p>
      <p class="text-body">{{ t('customers.profile.errorLoadBody') }}</p>
      <AppButton variant="secondary" type="button" @click="onRetry">{{ t('customers.profile.retry') }}</AppButton>
    </div>

    <div v-else-if="store.current">
      <header class="profile-header">
        <div>
          <h2>{{ store.current.fullName }}</h2>
          <p>{{ store.current.email }}</p>
        </div>
        <AppButton type="button" @click="onEdit">{{ t('customers.profile.edit') }}</AppButton>
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

      <AppButton variant="ghost" type="button" @click="onBack">{{ t('customers.profile.back') }}</AppButton>
    </div>
  </div>
</template>

<style scoped>
.customer-profile-view {
  max-width: 40rem;
  margin: var(--space-8) auto;
}

.state-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-6);
}

.profile-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}
</style>
