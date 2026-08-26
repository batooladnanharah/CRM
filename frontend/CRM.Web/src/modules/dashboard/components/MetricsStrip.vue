<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { DashboardSummary } from '@/types/dashboard'

defineProps<{ summary: DashboardSummary | null; loading: boolean }>()

const { t } = useI18n()
</script>

<template>
  <section class="metrics-strip">
    <div v-for="n in loading ? 4 : 0" :key="n" class="surface metric-tile skeleton"></div>

    <template v-if="!loading">
      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('dashboard.metrics.openAssigned') }}</span>
        <strong>{{ summary?.openAssignedCount ?? 0 }}</strong>
      </router-link>

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile" :class="{ 'metric-tile--warning': (summary?.needsActionCount ?? 0) > 0 }">
        <span class="metric-label">{{ t('dashboard.metrics.needsAction') }}</span>
        <strong>{{ summary?.needsActionCount ?? 0 }}</strong>
      </router-link>

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile metric-tile--success">
        <span class="metric-label">{{ t('dashboard.metrics.resolvedLast7Days') }}</span>
        <strong>{{ summary?.resolvedLast7DaysCount ?? 0 }}</strong>
      </router-link>

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile" :class="{ 'metric-tile--danger': (summary?.slaAtRiskCount ?? 0) > 0 }">
        <span class="metric-label">{{ t('dashboard.metrics.slaAtRisk') }}</span>
        <strong>{{ summary?.slaAtRiskCount ?? 0 }}</strong>
      </router-link>
    </template>
  </section>
</template>

<style scoped>
.metrics-strip {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-5);
  margin-bottom: var(--space-6);
}

.metric-tile {
  display: block;
  padding: var(--space-5);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  text-decoration: none;
  color: inherit;
  transition: box-shadow 0.15s ease, border-color 0.15s ease;
}

.metric-tile:hover {
  box-shadow: var(--shadow-md);
}

.metric-label {
  display: block;
  color: var(--color-text-muted);
  font: 500 11px var(--font-sans);
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.metric-tile strong {
  display: block;
  margin-top: var(--space-3);
  color: var(--color-text-primary);
  font: 500 24px var(--font-sans);
}

.metric-tile--warning { border-color: var(--color-status-warning-bg); background: var(--color-status-warning-bg); }
.metric-tile--warning strong { color: var(--color-status-warning); }
.metric-tile--danger { border-color: var(--color-status-danger-bg); background: var(--color-status-danger-bg); }
.metric-tile--danger strong { color: var(--color-status-danger); }
.metric-tile--success strong { color: var(--color-status-success); }

.skeleton {
  height: 5.5rem;
  background: var(--canvas);
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: repeat(2, 1fr);
    gap: var(--space-3);
  }
}
</style>
