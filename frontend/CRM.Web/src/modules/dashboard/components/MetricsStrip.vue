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

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('dashboard.metrics.needsAction') }}</span>
        <strong>{{ summary?.needsActionCount ?? 0 }}</strong>
      </router-link>

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile">
        <span class="metric-label">{{ t('dashboard.metrics.resolvedLast7Days') }}</span>
        <strong>{{ summary?.resolvedLast7DaysCount ?? 0 }}</strong>
      </router-link>

      <router-link :to="{ name: 'tickets-list' }" class="surface metric-tile">
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
  color: var(--muted);
  font: 700 0.7rem Arial, sans-serif;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.metric-tile strong {
  display: block;
  margin-top: 10px;
  color: var(--navy);
  font-size: 2rem;
}

.skeleton {
  height: 5.5rem;
  background: #eee;
}

@media (max-width: 700px) {
  .metrics-strip {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
