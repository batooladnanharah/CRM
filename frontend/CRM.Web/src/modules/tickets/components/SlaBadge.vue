<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRelativeTime } from '@/composables/useRelativeTime'
import AppBadge from '@/components/ui/AppBadge.vue'
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

const statusTones: Record<SlaStatus, 'neutral' | 'success' | 'warning' | 'danger'> = {
  NotApplicable: 'neutral',
  OnTrack: 'success',
  AtRisk: 'warning',
  Breached: 'danger',
  Met: 'success',
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
  <AppBadge class="sla-badge" :tone="statusTones[status]" :title="label">
    {{ label }}: {{ statusLabel }}
    <span v-if="dueLabel" class="sla-badge__due">({{ dueLabel }})</span>
  </AppBadge>
</template>

<style scoped>
.sla-badge__due {
  opacity: 0.8;
}
</style>
