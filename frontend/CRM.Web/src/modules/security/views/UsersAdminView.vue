<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useSecurityStore } from '@/stores/security'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { AdminRole, AdminUserListItem } from '@/types/security'

const ROLES: AdminRole[] = ['admin', 'agent', 'customer']

const { t } = useI18n()
const authStore = useAuthStore()
const store = useSecurityStore()

function isSelf(user: AdminUserListItem): boolean {
  return user.id === authStore.user?.id
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
        <AppInput
          id="users-search"
          :label="t('common.search')"
          type="search"
          :model-value="store.usersSearch"
          @update:model-value="store.setUsersSearch"
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

    <AppAlert v-if="store.mutateError" tone="danger" role="alert">
      {{ store.mutateError === 'cannot_modify_self' ? t('security.users.confirmSelf') : store.mutateError }}
    </AppAlert>

    <LoadingState v-if="store.usersLoading" />
    <ErrorState v-else-if="store.usersError" :retryable="false" :message="t('security.users.errorLoad')" />
    <EmptyState v-else-if="store.users.length === 0" :description="t('security.users.empty')" />

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
            <td>
              <AppBadge :tone="user.isDisabled ? 'danger' : 'success'">
                {{ user.isDisabled ? t('security.users.disabled') : t('security.users.enabled') }}
              </AppBadge>
            </td>
            <td>
              <AppButton
                v-if="!user.isDisabled"
                variant="ghost"
                size="sm"
                type="button"
                :disabled="isSelf(user) || store.mutating"
                @click="onDisable(user)"
              >
                {{ t('security.users.actions.disable') }}
              </AppButton>
              <AppButton
                v-else
                variant="ghost"
                size="sm"
                type="button"
                :disabled="store.mutating"
                @click="onEnable(user)"
              >
                {{ t('security.users.actions.enable') }}
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppPagination
      v-if="store.users.length > 0"
      :page="store.usersPage"
      :page-size="store.usersPageSize"
      :total-count="store.usersTotalCount"
      @update:page="store.setUsersPage"
    />
  </div>
</template>

<style scoped>
.users-admin-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}
</style>
