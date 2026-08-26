<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'

const { t } = useI18n()
const { locale } = useLocale()
const authStore = useAuthStore()
const store = useCustomerPortalStore()

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function loadDashboard() {
  void store.fetchDashboard()
}

onMounted(loadDashboard)
</script>

<template>
  <div class="portal-dashboard-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.dashboard.welcome', { name: authStore.user?.name ?? '' }) }}</h1>
      </div>
    </div>

    <p v-if="store.error" role="alert" class="portal-error">
      {{ t('portal.tickets.error') }}
      <button type="button" @click="loadDashboard">{{ t('portal.tickets.retry') }}</button>
    </p>

    <div v-if="store.loading" class="surface metrics-strip">
      <div class="metric-tile skeleton"></div>
      <div class="metric-tile skeleton"></div>
      <div class="metric-tile skeleton"></div>
    </div>

    <div v-else class="metrics-strip">
      <router-link :to="{ name: 'portal-tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('portal.dashboard.metrics.open') }}</span>
        <strong>{{ store.dashboard?.openCount ?? 0 }}</strong>
      </router-link>
      <router-link :to="{ name: 'portal-tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('portal.dashboard.metrics.pending') }}</span>
        <strong>{{ store.dashboard?.pendingCount ?? 0 }}</strong>
      </router-link>
      <router-link :to="{ name: 'portal-tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('portal.dashboard.metrics.resolved') }}</span>
        <strong>{{ store.dashboard?.resolvedCount ?? 0 }}</strong>
      </router-link>
    </div>

    <div class="portal-cta-row">
      <router-link class="button" :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.dashboard.submitCta') }}
      </router-link>
      <router-link class="button" :to="{ name: 'portal-tickets-list' }">
        {{ t('portal.dashboard.viewAllCta') }}
      </router-link>
    </div>

    <section class="surface recent-tickets">
      <h2>{{ t('portal.dashboard.recentTickets') }}</h2>
      <p v-if="!store.loading && (store.dashboard?.recentTickets.length ?? 0) === 0">
        {{ t('portal.tickets.empty') }}
      </p>
      <ul v-else class="recent-tickets-list">
        <li v-for="ticket in store.dashboard?.recentTickets ?? []" :key="ticket.id">
          <router-link :to="{ name: 'portal-ticket-details', params: { id: ticket.id } }">
            {{ ticket.title }}
          </router-link>
          <span class="recent-ticket-meta">
            {{ t(`tickets.statuses.${ticket.status}`) }} · {{ formatDate(ticket.updatedAtUtc) }}
          </span>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.portal-dashboard-view {
  max-width: 60rem;
  margin: 4rem auto;
}

.portal-error {
  padding: 12px 16px;
  margin-bottom: 18px;
  color: #b00020;
  background: #fdecea;
  border-radius: 6px;
}

.metrics-strip {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 18px;
  margin-bottom: 24px;
}

.metric-tile {
  display: block;
  padding: 20px;
  text-decoration: none;
  color: inherit;
}

.metric-label {
  display: block;
  text-transform: uppercase;
  font-size: 0.7rem;
  letter-spacing: 0.08em;
}

.metric-tile strong {
  display: block;
  margin-top: 10px;
  font-size: 2rem;
}

.skeleton {
  height: 5.5rem;
  background: #eee;
}

.portal-cta-row {
  display: flex;
  gap: 1rem;
  margin-bottom: 24px;
}

.recent-tickets-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.recent-ticket-meta {
  display: block;
  opacity: 0.75;
  font-size: 0.85rem;
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: 1fr;
  }
}
</style>
