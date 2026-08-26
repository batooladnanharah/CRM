<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCustomerInteractionsStore } from '@/stores/customerInteractions'
import { useLocale } from '@/composables/useLocale'

const props = defineProps<{ customerId: string }>()

const { t } = useI18n()
const { locale } = useLocale()
const store = useCustomerInteractionsStore()

const totalPages = computed(() => Math.max(1, Math.ceil(store.totalCount / store.pageSize)))

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

onMounted(() => {
  void store.fetch(props.customerId, 1)
})

function onRetry() {
  store.retry()
}

function onPrev() {
  if (store.page > 1) {
    void store.fetch(props.customerId, store.page - 1)
  }
}

function onNext() {
  if (store.page < totalPages.value) {
    void store.fetch(props.customerId, store.page + 1)
  }
}
</script>

<template>
  <div class="interaction-timeline">
    <ul v-if="store.loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="store.error" role="alert">
      <p>{{ t('customers.interactions.errorLoad') }}</p>
      <button type="button" @click="onRetry">{{ t('customers.interactions.retry') }}</button>
    </div>

    <div v-else-if="store.items.length === 0">
      <p>{{ t('customers.interactions.empty') }}</p>
      <p>{{ t('customers.interactions.emptyHint') }}</p>
    </div>

    <ol v-else class="timeline">
      <li v-for="item in store.items" :key="item.id">
        <p class="timeline-meta">
          <span>{{ t(`customers.interactions.types.${item.type}`) }}</span>
          <span>{{ formatDate(item.occurredAt) }}</span>
        </p>
        <p>{{ item.summary }}</p>
        <p v-if="item.actorName">{{ t('customers.interactions.by', { name: item.actorName }) }}</p>
        <!-- TODO: link to the ticket once a ticket module/route exists. -->
        <p v-if="item.ticketId">{{ item.ticketId }}</p>
      </li>
    </ol>

    <div v-if="!store.loading && !store.error && store.totalCount > store.pageSize" class="pagination">
      <button type="button" :disabled="store.page <= 1" @click="onPrev">
        {{ t('customers.interactions.previous') }}
      </button>
      <span>{{ t('customers.interactions.pageOf', { page: store.page, total: totalPages }) }}</span>
      <button type="button" :disabled="store.page >= totalPages" @click="onNext">
        {{ t('customers.interactions.next') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.timeline {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.timeline-meta {
  display: flex;
  gap: 1rem;
  font-weight: bold;
}

.skeleton {
  list-style: none;
  padding: 0;
}

.skeleton li {
  height: 2rem;
  margin-bottom: 0.5rem;
  background: #eee;
}

.pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1rem;
}
</style>
