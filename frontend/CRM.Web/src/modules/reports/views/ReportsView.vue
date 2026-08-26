<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useReportsStore } from '@/stores/reports'
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
      <button type="button" :disabled="store.loading" @click="loadSummary">
        {{ store.loading ? t('common.loading') : t('reports.refresh') }}
      </button>
    </div>

    <p v-if="store.error" role="alert" class="reports-error">
      {{ t('reports.errors.loadFailed') }}
    </p>

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
          <li class="sla-badge sla-badge--ok">
            {{ t('reports.sla.withinSla') }}: {{ store.summary.slaPerformance.withinSla }}
            ({{ store.summary.slaPerformance.withinSlaPercent }}%)
          </li>
          <li class="sla-badge sla-badge--warn">
            {{ t('reports.sla.atRisk') }}: {{ store.summary.slaPerformance.atRisk }}
            ({{ store.summary.slaPerformance.atRiskPercent }}%)
          </li>
          <li class="sla-badge sla-badge--danger">
            {{ t('reports.sla.breached') }}: {{ store.summary.slaPerformance.breached }}
            ({{ store.summary.slaPerformance.breachedPercent }}%)
          </li>
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

    <p v-else-if="store.loading">{{ t('common.loading') }}</p>
  </div>
</template>

<style scoped>
.reports-view {
  max-width: 60rem;
  margin: 4rem auto;
}

.reports-error {
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
  padding: 20px;
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

.report-section {
  padding: 1rem;
  margin-bottom: 1.5rem;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  text-align: start;
  padding: 0.5rem;
}

.sla-rows {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.sla-badge {
  display: inline-flex;
  padding: 0.4rem 0.8rem;
  border-radius: 999px;
  font-size: 0.9rem;
  width: fit-content;
}

.sla-badge--ok {
  background: #e3f6e8;
  color: #1a7a3a;
}

.sla-badge--warn {
  background: #fff4e0;
  color: #9a6400;
}

.sla-badge--danger {
  background: #fde2e1;
  color: #a3231e;
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: 1fr;
  }
}
</style>
