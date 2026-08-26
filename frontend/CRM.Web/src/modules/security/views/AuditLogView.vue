<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSecurityStore } from '@/stores/security'
import AppButton from '@/components/ui/AppButton.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { AuditLogEntry } from '@/types/security'

const { t } = useI18n()
const store = useSecurityStore()

const actionFilter = ref('')
const actorIdFilter = ref('')
const targetIdFilter = ref('')
const fromFilter = ref('')
const toFilter = ref('')

const selectedEntry = ref<AuditLogEntry | null>(null)

function currentFilters() {
  return {
    action: actionFilter.value || undefined,
    actorId: actorIdFilter.value || undefined,
    targetId: targetIdFilter.value || undefined,
    from: fromFilter.value ? new Date(fromFilter.value).toISOString() : undefined,
    to: toFilter.value ? new Date(toFilter.value).toISOString() : undefined,
  }
}

function applyFilters() {
  void store.fetchAuditLog({ ...currentFilters(), page: 1 })
}

function onPageChange(page: number) {
  store.setAuditPage(page, currentFilters())
}

function openDetails(entry: AuditLogEntry) {
  selectedEntry.value = entry
}

function closeDetails() {
  selectedEntry.value = null
}

onMounted(() => {
  void store.fetchAuditLog()
})
</script>

<template>
  <div class="audit-log-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('security.audit.title') }}</h1>
      </div>
    </div>

    <form class="surface toolbar" @submit.prevent="applyFilters">
      <div class="toolbar-field">
        <label for="audit-action">{{ t('security.audit.filters.action') }}</label>
        <input
          id="audit-action"
          v-model="actionFilter"
          type="text"
          :placeholder="t('security.audit.filters.actionPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-actor">{{ t('security.audit.filters.actor') }}</label>
        <input
          id="audit-actor"
          v-model="actorIdFilter"
          type="text"
          :placeholder="t('security.audit.filters.actorPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-target">{{ t('security.audit.filters.target') }}</label>
        <input
          id="audit-target"
          v-model="targetIdFilter"
          type="text"
          :placeholder="t('security.audit.filters.targetPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-from">{{ t('security.audit.filters.from') }}</label>
        <input
          id="audit-from"
          v-model="fromFilter"
          type="date"
          :placeholder="t('security.audit.filters.fromPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-to">{{ t('security.audit.filters.to') }}</label>
        <input
          id="audit-to"
          v-model="toFilter"
          type="date"
          :placeholder="t('security.audit.filters.toPlaceholder')"
        />
      </div>
      <AppButton type="submit">{{ t('security.audit.filters.apply') }}</AppButton>
    </form>

    <LoadingState v-if="store.auditLoading" />
    <ErrorState v-else-if="store.auditError" :retryable="false" :message="t('security.audit.errorLoad')" />
    <EmptyState v-else-if="store.auditEntries.length === 0" :description="t('security.audit.empty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('security.audit.columns.occurredAt') }}</th>
            <th>{{ t('security.audit.columns.actor') }}</th>
            <th>{{ t('security.audit.columns.action') }}</th>
            <th>{{ t('security.audit.columns.target') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in store.auditEntries" :key="entry.id">
            <td>{{ entry.occurredAtUtc }}</td>
            <td>{{ entry.actorEmail ?? t('security.audit.systemActor') }}</td>
            <td>{{ entry.action }}</td>
            <td>{{ entry.targetType }}: {{ entry.targetId }}</td>
            <td>
              <AppButton variant="ghost" size="sm" type="button" @click="openDetails(entry)">
                {{ t('security.audit.viewPayload') }}
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppPagination
      v-if="store.auditEntries.length > 0"
      :page="store.auditPage"
      :page-size="store.auditPageSize"
      :total-count="store.auditTotalCount"
      @update:page="onPageChange"
    />

    <div v-if="selectedEntry" class="drawer-overlay" @click.self="closeDetails">
      <aside class="surface drawer">
        <div class="drawer-header">
          <h2>{{ t('security.audit.payloadDrawer.title') }}</h2>
          <AppButton variant="ghost" size="sm" type="button" @click="closeDetails">{{ t('common.close') }}</AppButton>
        </div>
        <pre class="payload-json">{{ selectedEntry.payloadJson ?? t('security.audit.payloadDrawer.empty') }}</pre>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.audit-log-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.drawer-overlay {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  justify-content: flex-end;
  background: rgba(24, 35, 45, 0.4);
}

.drawer {
  width: min(28rem, 90vw);
  height: 100%;
  overflow-y: auto;
  padding: var(--space-6);
}

.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-4);
}

.payload-json {
  white-space: pre-wrap;
  word-break: break-word;
  font-size: var(--font-size-sm);
}
</style>
