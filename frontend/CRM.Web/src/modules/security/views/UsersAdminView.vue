<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useSecurityStore } from '@/stores/security'
import type { AdminRole, AdminUserListItem } from '@/types/security'

const ROLES: AdminRole[] = ['admin', 'agent', 'customer']

const { t } = useI18n()
const authStore = useAuthStore()
const store = useSecurityStore()

const totalPages = computed(() => Math.max(1, Math.ceil(store.usersTotalCount / store.usersPageSize)))

function isSelf(user: AdminUserListItem): boolean {
  return user.id === authStore.user?.id
}

function onSearchInput(event: Event) {
  store.setUsersSearch((event.target as HTMLInputElement).value)
}

function onRoleFilterChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as AdminRole | ''
  store.setUsersFilters({ role: value })
}

function onDisabledFilterChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  store.setUsersFilters({ disabled: value === '' ? undefined : value === 'true' })
}

async function onRoleChange(user: AdminUserListItem, event: Event) {
  const role = (event.target as HTMLSelectElement).value
  if (role === user.role) {
    return
  }
  try {
    await store.changeRole(user.id, role)
  } catch {
    // store.mutateError already carries the error code to display.
  }
}

async function onDisable(user: AdminUserListItem) {
  if (!window.confirm(t('security.users.confirmDisable'))) {
    return
  }
  try {
    await store.disable(user.id)
  } catch {
    // store.mutateError already carries the error code to display.
  }
}

async function onEnable(user: AdminUserListItem) {
  try {
    await store.enable(user.id)
  } catch {
    // store.mutateError already carries the error code to display.
  }
}

function onPrev() {
  if (store.usersPage > 1) {
    store.setUsersPage(store.usersPage - 1)
  }
}

function onNext() {
  if (store.usersPage < totalPages.value) {
    store.setUsersPage(store.usersPage + 1)
  }
}

onMounted(() => {
  void store.fetchUsers()
})
</script>

<template>
  <div class="users-admin-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('security.users.title') }}</h1>
      </div>
    </div>

    <div class="surface toolbar">
      <div class="toolbar-field">
        <label for="users-search">{{ t('common.search') }}</label>
        <input
          id="users-search"
          type="search"
          :value="store.usersSearch"
          @input="onSearchInput"
        />
      </div>
      <div class="toolbar-field">
        <label for="users-role-filter">{{ t('security.users.filters.role') }}</label>
        <select id="users-role-filter" :value="store.usersRoleFilter" @change="onRoleFilterChange">
          <option value="">{{ t('security.users.filters.allRoles') }}</option>
          <option v-for="role in ROLES" :key="role" :value="role">{{ role }}</option>
        </select>
      </div>
      <div class="toolbar-field">
        <label for="users-disabled-filter">{{ t('security.users.filters.status') }}</label>
        <select id="users-disabled-filter" @change="onDisabledFilterChange">
          <option value="">{{ t('security.users.filters.allStatuses') }}</option>
          <option value="false">{{ t('security.users.filters.enabledOnly') }}</option>
          <option value="true">{{ t('security.users.filters.disabledOnly') }}</option>
        </select>
      </div>
    </div>

    <p v-if="store.mutateError" role="alert">
      {{ store.mutateError === 'cannot_modify_self' ? t('security.users.confirmSelf') : store.mutateError }}
    </p>

    <p v-if="store.usersLoading">{{ t('common.loading') }}</p>
    <p v-else-if="store.usersError" role="alert">{{ t('security.users.errorLoad') }}</p>
    <div v-else-if="store.users.length === 0" class="surface empty-state">
      <p>{{ t('security.users.empty') }}</p>
    </div>

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('security.users.columns.name') }}</th>
            <th>{{ t('security.users.columns.email') }}</th>
            <th>{{ t('security.users.columns.role') }}</th>
            <th>{{ t('security.users.columns.status') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in store.users" :key="user.id">
            <td>{{ user.name }}</td>
            <td>{{ user.email }}</td>
            <td>
              <select
                :value="user.role"
                :disabled="isSelf(user) || store.mutating"
                @change="onRoleChange(user, $event)"
              >
                <option v-for="role in ROLES" :key="role" :value="role">{{ role }}</option>
              </select>
            </td>
            <td>{{ user.isDisabled ? t('security.users.disabled') : t('security.users.enabled') }}</td>
            <td>
              <button
                v-if="!user.isDisabled"
                type="button"
                :disabled="isSelf(user) || store.mutating"
                @click="onDisable(user)"
              >
                {{ t('security.users.actions.disable') }}
              </button>
              <button
                v-else
                type="button"
                :disabled="store.mutating"
                @click="onEnable(user)"
              >
                {{ t('security.users.actions.enable') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination">
      <button type="button" :disabled="store.usersPage <= 1" @click="onPrev">
        {{ t('customers.pagination.prev') }}
      </button>
      <span>{{ t('customers.pagination.pageOf', { page: store.usersPage, totalPages }) }}</span>
      <button type="button" :disabled="store.usersPage >= totalPages" @click="onNext">
        {{ t('customers.pagination.next') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.users-admin-view {
  max-width: 60rem;
  margin: 4rem auto;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
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
</style>
