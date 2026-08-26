<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useDashboardStore } from '@/stores/dashboard'
import MetricsStrip from '@/modules/dashboard/components/MetricsStrip.vue'
import MyTicketsWidget from '@/modules/dashboard/components/MyTicketsWidget.vue'
import MyTasksWidget from '@/modules/dashboard/components/MyTasksWidget.vue'
import RecentCustomersWidget from '@/modules/dashboard/components/RecentCustomersWidget.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'

const { t } = useI18n()
const authStore = useAuthStore()
const store = useDashboardStore()

onMounted(() => {
  void store.loadAll()
})

function onRefresh() {
  void store.refresh()
}
</script>

<template>
  <div class="dashboard-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('dashboard.overline') }}</p>
        <h1>{{ t('dashboard.title') }}</h1>
        <p>{{ t('dashboard.greeting', { name: authStore.user?.name ?? '' }) }}</p>
      </div>
      <AppButton variant="secondary" @click="onRefresh">{{ t('dashboard.refresh') }}</AppButton>
    </div>

    <AppAlert v-if="store.error" tone="danger" class="dashboard-error">
      {{ t('dashboard.errors.loadFailed') }}
    </AppAlert>

    <MetricsStrip :summary="store.summary" :loading="store.loading" />

    <section class="dashboard-row">
      <MyTicketsWidget class="widget-main" :tickets="store.myOpenTickets" :loading="store.loading" />
      <RecentCustomersWidget
        class="widget-side"
        :customers="store.recentCustomers"
        :loading="store.loading"
      />
    </section>

    <MyTasksWidget :tickets="store.myTasks" :loading="store.loading" />
  </div>
</template>

<style scoped>
.dashboard-view {
  max-width: 1200px;
}

.dashboard-error {
  margin-bottom: var(--space-5);
}

.dashboard-row {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 18px;
  margin-bottom: 24px;
}

@media (max-width: 900px) {
  .dashboard-row {
    grid-template-columns: 1fr;
  }
}
</style>
