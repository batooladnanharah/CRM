<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSecurityStore } from '@/stores/security'
import type { AuditLogEntry } from '@/types/security'

const { t } = useI18n()
const store = useSecurityStore()

const actionFilter = ref('')
const actorIdFilter = ref('')
const targetIdFilter = ref('')
const fromFilter = ref('')
const toFilter = ref('')

const selectedEntry = ref<AuditLogEntry | null>(null)

const totalPages = computed(() => Math.max(1, Math.ceil(store.auditTotalCount / store.auditPageSize)))

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

function onPrev() {
  if (store.auditPage > 1) {
    store.setAuditPage(store.auditPage - 1, currentFilters())
  }
}

function onNext() {
  if (store.auditPage < totalPages.value) {
    store.setAuditPage(store.auditPage + 1, currentFilters())
  }
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
        <input id="audit-action" v-model="actionFilter" type="text" />
      </div>
      <div class="toolbar-field">
        <label for="audit-actor">{{ t('security.audit.filters.actor') }}</label>
        <input id="audit-actor" v-model="actorIdFilter" type="text" />
      </div>
      <div class="toolbar-field">
        <label for="audit-target">{{ t('security.audit.filters.target') }}</label>
        <input id="audit-target" v-model="targetIdFilter" type="text" />
      </div>
      <div class="toolbar-field">
        <label for="audit-from">{{ t('security.audit.filters.from') }}</label>
        <input id="audit-from" v-model="fromFilter" type="date" />
      </div>
      <div class="toolbar-field">
        <label for="audit-to">{{ t('security.audit.filters.to') }}</label>
        <input id="audit-to" v-model="toFilter" type="date" />
      </div>
      <button type="submit">{{ t('security.audit.filters.apply') }}</button>
    </form>

    <p v-if="store.auditLoading">{{ t('common.loading') }}</p>
    <p v-else-if="store.auditError" role="alert">{{ t('security.audit.errorLoad') }}</p>
    <div v-else-if="store.auditEntries.length === 0" class="surface empty-state">
      <p>{{ t('security.audit.empty') }}</p>
    </div>

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
              <button type="button" @click="openDetails(entry)">
                {{ t('security.audit.viewPayload') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination">
      <button type="button" :disabled="store.auditPage <= 1" @click="onPrev">
        {{ t('customers.pagination.prev') }}
      </button>
      <span>{{ t('customers.pagination.pageOf', { page: store.auditPage, totalPages }) }}</span>
      <button type="button" :disabled="store.auditPage >= totalPages" @click="onNext">
        {{ t('customers.pagination.next') }}
      </button>
    </div>

    <div v-if="selectedEntry" class="drawer-overlay" @click.self="closeDetails">
      <aside class="surface drawer">
        <div class="drawer-header">
          <h2>{{ t('security.audit.payloadDrawer.title') }}</h2>
          <button type="button" @click="closeDetails">{{ t('common.close') }}</button>
        </div>
        <pre class="payload-json">{{ selectedEntry.payloadJson ?? t('security.audit.payloadDrawer.empty') }}</pre>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.audit-log-view {
  max-width: 72rem;
  margin: 4rem auto;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: flex-end;
  padding: 1rem;
  margin-bottom: 1rem;
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

.pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1rem;
}

.drawer-overlay {
  position: fixed;
  inset: 0;
  z-index: 20;
  display: flex;
  justify-content: flex-end;
  background: rgba(24, 35, 45, 0.4);
}

.drawer {
  width: min(28rem, 90vw);
  height: 100%;
  overflow-y: auto;
  padding: 1.5rem;
}

.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.payload-json {
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
