<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import { useTicketBadgeTone } from '@/composables/useTicketBadgeTone'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'

const { t } = useI18n()
const { locale } = useLocale()
const authStore = useAuthStore()
const store = useCustomerPortalStore()
const { statusTone } = useTicketBadgeTone()

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

    <AppAlert v-if="store.error" tone="danger" class="portal-error">
      {{ t('portal.tickets.error') }}
      <AppButton variant="secondary" size="sm" type="button" @click="loadDashboard">{{ t('portal.tickets.retry') }}</AppButton>
    </AppAlert>

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
      <AppButton :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.dashboard.submitCta') }}
      </AppButton>
      <AppButton variant="secondary" :to="{ name: 'portal-tickets-list' }">
        {{ t('portal.dashboard.viewAllCta') }}
      </AppButton>
    </div>

    <section class="surface recent-tickets">
      <h2>{{ t('portal.dashboard.recentTickets') }}</h2>
      <p v-if="!store.loading && (store.dashboard?.recentTickets.length ?? 0) === 0">
        {{ t('portal.tickets.empty') }}
      </p>
      <ul v-else class="recent-tickets-list">
        <li v-for="ticket in store.dashboard?.recentTickets ?? []" :key="ticket.id" class="recent-ticket-row">
          <div class="recent-ticket-main">
            <router-link :to="{ name: 'portal-ticket-details', params: { id: ticket.id } }">
              {{ ticket.title }}
            </router-link>
            <span class="recent-ticket-meta">{{ formatDate(ticket.updatedAtUtc) }}</span>
          </div>
          <AppBadge :tone="statusTone(ticket.status)">{{ t(`tickets.statuses.${ticket.status}`) }}</AppBadge>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.portal-dashboard-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.portal-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.metrics-strip {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-5);
  margin-bottom: var(--space-6);
}

.metric-tile {
  display: block;
  padding: var(--space-5);
  text-decoration: none;
  color: inherit;
  transition: box-shadow 0.15s ease;
}

.metric-tile:hover {
  box-shadow: var(--shadow-md);
}

.metric-label {
  display: block;
  color: var(--muted);
  text-transform: uppercase;
  font-size: var(--font-size-xs);
  letter-spacing: 0.08em;
}

.metric-tile strong {
  display: block;
  margin-top: var(--space-3);
  color: var(--navy);
  font-size: 2rem;
}

.skeleton {
  height: 5.5rem;
  background: var(--canvas);
}

.portal-cta-row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-4);
  margin-bottom: var(--space-6);
}

.recent-tickets-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.recent-ticket-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
}

.recent-ticket-main {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.recent-ticket-meta {
  display: block;
  color: var(--muted);
  font-size: var(--font-size-sm);
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: 1fr;
  }
}
</style>
