<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useReportsStore } from '@/stores/reports'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import type { TicketStatus } from '@/types/tickets'

// Mirrors backend/CRM.Api/Tickets/TicketStatusRules.cs's declaration order —
// keep both in sync.
const STATUS_ORDER: TicketStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed']

const { t } = useI18n()
const store = useReportsStore()

function loadSummary() {
  void store.fetchSummary()
}

onMounted(loadSummary)

const orderedStatusDistribution = computed(() => {
  const byStatus = new Map(store.summary?.statusDistribution.map((s) => [s.status, s.count]) ?? [])
  return STATUS_ORDER.map((status) => ({ status, count: byStatus.get(status) ?? 0 }))
})

// Handles > 24h (Xd Xh Xm) and falls back defensively for negative/NaN input.
function formatResolutionTime(minutes: number | null): string {
  if (minutes === null || !Number.isFinite(minutes) || minutes < 0) {
    return t('reports.resolution.none')
  }

  const totalMinutes = Math.round(minutes)
  const days = Math.floor(totalMinutes / (24 * 60))
  const hours = Math.floor((totalMinutes % (24 * 60)) / 60)
  const mins = totalMinutes % 60

  if (days > 0) {
    return `${days}d ${hours}h ${mins}m`
  }
  return `${hours}h ${mins}m`
}
</script>

<template>
  <div class="reports-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('reports.title') }}</h1>
      </div>
      <AppButton variant="secondary" type="button" :loading="store.loading" @click="loadSummary">
        {{ store.loading ? t('common.loading') : t('reports.refresh') }}
      </AppButton>
    </div>

    <AppAlert v-if="store.error" tone="danger" class="reports-error">
      {{ t('reports.errors.loadFailed') }}
    </AppAlert>

    <template v-if="store.summary">
      <section class="metrics-strip">
        <div class="surface metric-tile">
          <span class="metric-label">{{ t('reports.volume.total') }}</span>
          <strong>{{ store.summary.ticketVolume.total }}</strong>
        </div>
        <div class="surface metric-tile">
          <span class="metric-label">{{ t('reports.volume.open') }}</span>
          <strong>{{ store.summary.ticketVolume.open }}</strong>
        </div>
        <div class="surface metric-tile">
          <span class="metric-label">{{ t('reports.volume.resolved') }}</span>
          <strong>{{ store.summary.ticketVolume.resolved }}</strong>
        </div>
      </section>

      <section class="surface report-section">
        <h2>{{ t('tickets.list.columns.status') }}</h2>
        <table>
          <thead>
            <tr>
              <th>{{ t('tickets.list.columns.status') }}</th>
              <th>{{ t('reports.agents.headers.count') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in orderedStatusDistribution" :key="row.status">
              <td>{{ t(`tickets.statuses.${row.status}`) }}</td>
              <td>{{ row.count }}</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section class="surface report-section">
        <h2>{{ t('reports.agents.title') }}</h2>
        <p v-if="store.summary.agentPerformance.length === 0">{{ t('reports.agents.empty') }}</p>
        <table v-else>
          <thead>
            <tr>
              <th>{{ t('reports.agents.headers.name') }}</th>
              <th>{{ t('reports.agents.headers.count') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="agent in store.summary.agentPerformance" :key="agent.agentId">
              <td>{{ agent.displayName }}</td>
              <td>{{ agent.ticketCount }}</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section class="surface report-section">
        <h2>{{ t('reports.sla.title') }}</h2>
        <ul class="sla-rows">
          <li><AppBadge tone="success">
            {{ t('reports.sla.withinSla') }}: {{ store.summary.slaPerformance.withinSla }}
            ({{ store.summary.slaPerformance.withinSlaPercent }}%)
          </AppBadge></li>
          <li><AppBadge tone="warning">
            {{ t('reports.sla.atRisk') }}: {{ store.summary.slaPerformance.atRisk }}
            ({{ store.summary.slaPerformance.atRiskPercent }}%)
          </AppBadge></li>
          <li><AppBadge tone="danger">
            {{ t('reports.sla.breached') }}: {{ store.summary.slaPerformance.breached }}
            ({{ store.summary.slaPerformance.breachedPercent }}%)
          </AppBadge></li>
        </ul>
      </section>

      <section class="surface report-section">
        <h2>{{ t('reports.resolution.title') }}</h2>
        <p>
          {{ t('reports.resolution.averageLabel') }}:
          {{ formatResolutionTime(store.summary.resolution.averageResolutionMinutes) }}
        </p>
      </section>
    </template>

    <LoadingState v-else-if="store.loading" />
  </div>
</template>

<style scoped>
.reports-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.reports-error {
  margin-bottom: var(--space-5);
}

.metrics-strip {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-5);
  margin-bottom: var(--space-6);
}

.metric-tile {
  padding: var(--space-5);
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

.report-section {
  padding: var(--space-5);
  margin-bottom: var(--space-6);
}

.sla-rows {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  align-items: flex-start;
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: 1fr;
  }
}
</style>
