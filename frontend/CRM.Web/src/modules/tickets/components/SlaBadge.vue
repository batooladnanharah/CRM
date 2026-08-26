<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRelativeTime } from '@/composables/useRelativeTime'
import type { SlaStatus, TicketSla } from '@/types/tickets'

const props = defineProps<{
  sla: TicketSla
  kind: 'firstResponse' | 'resolution'
}>()

const { t } = useI18n()
const { formatRelativeTime } = useRelativeTime()

const status = computed<SlaStatus>(() =>
  props.kind === 'firstResponse' ? props.sla.firstResponseStatus : props.sla.resolutionStatus,
)

const dueAtUtc = computed(() =>
  props.kind === 'firstResponse' ? props.sla.firstResponseDueAtUtc : props.sla.resolutionDueAtUtc,
)

const breachedAtUtc = computed(() =>
  props.kind === 'firstResponse'
    ? props.sla.firstResponseBreachedAtUtc
    : props.sla.resolutionBreachedAtUtc,
)

const statusKeys: Record<SlaStatus, string> = {
  NotApplicable: 'sla.status.notApplicable',
  OnTrack: 'sla.status.onTrack',
  AtRisk: 'sla.status.atRisk',
  Breached: 'sla.status.breached',
  Met: 'sla.status.met',
}

const statusClasses: Record<SlaStatus, string> = {
  NotApplicable: 'sla-badge--muted',
  OnTrack: 'sla-badge--ok',
  AtRisk: 'sla-badge--warn',
  Breached: 'sla-badge--danger',
  Met: 'sla-badge--ok',
}

const label = computed(() => t(props.kind === 'firstResponse' ? 'sla.firstResponse' : 'sla.resolution'))
const statusLabel = computed(() => t(statusKeys[status.value]))

// Once breached, prefer the persisted breach timestamp (when the automation
// worker — or an on-demand evaluate-now — actually recorded it) over the due
// date, which by definition is already in the past for a breached ticket.
const dueLabel = computed(() => {
  if (status.value === 'Breached' && breachedAtUtc.value) {
    return formatRelativeTime(breachedAtUtc.value)
  }
  return dueAtUtc.value ? formatRelativeTime(dueAtUtc.value) : null
})
</script>

<template>
  <span class="sla-badge" :class="statusClasses[status]" :title="label">
    {{ label }}: {{ statusLabel }}
    <span v-if="dueLabel" class="sla-badge__due">({{ dueLabel }})</span>
  </span>
</template>

<style scoped>
.sla-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  font-size: 0.8rem;
  white-space: nowrap;
}

.sla-badge--muted {
  background: var(--color-surface-muted, #eee);
  color: var(--color-text-muted, #666);
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

.sla-badge__due {
  opacity: 0.8;
}
</style>
