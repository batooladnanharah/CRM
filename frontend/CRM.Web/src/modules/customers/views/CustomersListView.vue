<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useAuthStore } from '@/stores/auth'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { CustomerListQuery } from '@/types/customers'

const { t } = useI18n()
const store = useCustomersStore()
const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const columns: Array<{ key: NonNullable<CustomerListQuery['sortBy']>; labelKey: string }> = [
  { key: 'fullName', labelKey: 'customers.columns.fullName' },
  { key: 'email', labelKey: 'customers.columns.email' },
  { key: 'company', labelKey: 'customers.columns.company' },
  { key: 'createdAtUtc', labelKey: 'customers.columns.createdAt' },
]

const companyOptions = computed(() => {
  const companies = new Set(store.items.map((customer) => customer.company).filter(Boolean))
  if (store.company) companies.add(store.company)
  return [...companies].sort((first, second) => first!.localeCompare(second!)) as string[]
})

function sortIndicator(column: NonNullable<CustomerListQuery['sortBy']>): string {
  if (store.sortBy !== column) {
    return ''
  }
  return store.sortDir === 'asc' ? '▲' : '▼'
}

function onCompanyChange(event: Event) {
  store.setCompany((event.target as HTMLSelectElement).value)
}

function queryStringValue(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

onMounted(() => {
  store.search = queryStringValue(route.query.search)
  store.company = queryStringValue(route.query.company)
  void store.fetch()
})

watch(
  () => [store.search, store.company],
  ([search, company]) => {
    const normalizedSearch = typeof search === 'string' ? search.trim() : ''
    const normalizedCompany = typeof company === 'string' ? company.trim() : ''

    void router.replace({
      query: {
        ...route.query,
        search: normalizedSearch || undefined,
        company: normalizedCompany || undefined,
      },
    })
  },
)
</script>

<template>
  <div class="customers-list-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('customers.title') }}</h1>
        <p>{{ t('customers.list.subtitle') }}</p>
      </div>
      <AppButton
        v-if="authStore.isAdmin || authStore.isAgent"
        :to="{ name: 'customer-create' }"
      >
        {{ t('customers.list.addButton') }}
      </AppButton>
    </div>

    <div class="surface toolbar">
      <div class="toolbar-field">
        <AppInput
          id="customer-search"
          :label="t('common.search')"
          type="search"
          :placeholder="t('customers.search.placeholder')"
          :model-value="store.search"
          @update:model-value="store.setSearch"
        />
      </div>

      <div class="toolbar-field">
        <label for="company-filter">{{ t('customers.filters.company.label') }}</label>
        <select id="company-filter" :value="store.company" @change="onCompanyChange">
          <option value="">{{ t('customers.filters.company.options.all') }}</option>
          <option v-for="option in companyOptions" :key="option" :value="option">{{ option }}</option>
        </select>
      </div>
    </div>

    <LoadingState v-if="store.loading" :label="t('customers.loading')" />
    <ErrorState v-else-if="store.error" :retryable="false" :message="t('customers.errorLoad')" />
    <EmptyState
      v-else-if="store.items.length === 0"
      :description="store.search.trim() || store.company ? t('customers.empty.noResults') : t('customers.empty.default')"
    />

    <div v-else class="surface table-wrap"><table>
      <thead>
        <tr>
          <th v-for="column in columns" :key="column.key">
            <button type="button" @click="store.setSort(column.key)">
              {{ t(column.labelKey) }} {{ sortIndicator(column.key) }}
            </button>
          </th>
          <th>{{ t('customers.columns.phone') }}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="customer in store.items" :key="customer.id">
          <td>{{ customer.fullName }}</td>
          <td>{{ customer.email }}</td>
          <td>{{ customer.company }}</td>
          <td>{{ customer.createdAtUtc }}</td>
          <td>{{ customer.phone }}</td>
          <td class="row-actions">
            <AppButton
              variant="ghost"
              size="sm"
              :to="{ name: 'customer-profile', params: { id: customer.id } }"
            >
              {{ t('customers.list.viewButton') }}
            </AppButton>
            <AppButton
              v-if="authStore.isAdmin || authStore.isAgent"
              variant="ghost"
              size="sm"
              :to="{ name: 'customer-edit', params: { id: customer.id } }"
            >
              {{ t('customers.list.editButton') }}
            </AppButton>
          </td>
        </tr>
      </tbody>
    </table></div>

    <AppPagination
      v-if="store.items.length > 0"
      :page="store.page"
      :page-size="store.pageSize"
      :total-count="store.totalCount"
      @update:page="store.setPage"
    />
  </div>
</template>

<style scoped>
.row-actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
</style>
