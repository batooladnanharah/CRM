<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useAuthStore } from '@/stores/auth'
import type { CustomerListQuery } from '@/types/customers'

const { t } = useI18n()
const store = useCustomersStore()
const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const totalPages = computed(() => Math.max(1, Math.ceil(store.totalCount / store.pageSize)))

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

function onSearchInput(event: Event) {
  const value = (event.target as HTMLInputElement).value
  store.setSearch(value)
}

function onCompanyChange(event: Event) {
  store.setCompany((event.target as HTMLSelectElement).value)
}

function queryStringValue(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

function onPrev() {
  if (store.page > 1) {
    store.setPage(store.page - 1)
  }
}

function onNext() {
  if (store.page < totalPages.value) {
    store.setPage(store.page + 1)
  }
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
    <div class="page-heading"><div><p class="eyebrow">{{ t('navigation.workspace') }}</p><h1>{{ t('customers.title') }}</h1><p>{{ t('customers.list.subtitle') }}</p></div>
    <router-link class="button"
      v-if="authStore.isAdmin || authStore.isAgent"
      :to="{ name: 'customer-create' }"
    >
      {{ t('customers.list.addButton') }}
    >{{ t('customers.list.addButton') }}</router-link></div>

    <div class="surface toolbar"><div class="toolbar-field"><label for="customer-search">{{ t('common.search') }}</label><input id="customer-search"
      type="search"
      :placeholder="t('customers.search.placeholder')"
      :value="store.search"
      @input="onSearchInput"
    /></div>

    <div class="toolbar-field"><label for="company-filter">
      {{ t('customers.filters.company.label') }}
      </label><select id="company-filter" :value="store.company" @change="onCompanyChange">
        <option value="">{{ t('customers.filters.company.options.all') }}</option>
        <option v-for="option in companyOptions" :key="option" :value="option">{{ option }}</option>
      </select></div></div>

    <p v-if="store.loading">{{ t('customers.loading') }}</p>
    <p v-else-if="store.error" role="alert">{{ t('customers.errorLoad') }}</p>
    <div v-else-if="store.items.length === 0" class="surface empty-state"><p>{{ store.search.trim() || store.company ? t('customers.empty.noResults') : t('customers.empty.default') }}</p></div>

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
          <td>
            <router-link :to="{ name: 'customer-profile', params: { id: customer.id } }">
              {{ t('customers.list.viewButton') }}
            </router-link>
            <router-link
              v-if="authStore.isAdmin || authStore.isAgent"
              :to="{ name: 'customer-edit', params: { id: customer.id } }"
            >
              {{ t('customers.list.editButton') }}
            </router-link>
          </td>
        </tr>
      </tbody>
    </table></div>

    <div class="pagination">
      <button type="button" :disabled="store.page <= 1" @click="onPrev">
        {{ t('customers.pagination.prev') }}
      </button>
      <span>{{ t('customers.pagination.pageOf', { page: store.page, totalPages }) }}</span>
      <button type="button" :disabled="store.page >= totalPages" @click="onNext">
        {{ t('customers.pagination.next') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.customers-list-view {
  max-width: 60rem;
  margin: 4rem auto;
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
</style>
