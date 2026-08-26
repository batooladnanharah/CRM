<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useSecurityStore } from '@/stores/security'
import { useCustomersStore } from '@/stores/customers'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppDialog from '@/components/ui/AppDialog.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { AdminRole, AdminUserListItem } from '@/types/security'

const ROLES: AdminRole[] = ['admin', 'agent', 'customer']

const { t } = useI18n()
const authStore = useAuthStore()
const store = useSecurityStore()
const customersStore = useCustomersStore()

function isSelf(user: AdminUserListItem): boolean {
  return user.id === authStore.user?.id
}

const isCreateOpen = ref(false)
const createForm = reactive({
  email: '',
  password: '',
  name: '',
  role: 'agent' as AdminRole,
  customerId: '',
})
const createCustomerSearch = ref('')
const createCustomerName = ref('')
const createFormError = ref<string | null>(null)

const isEditOpen = ref(false)
const editingUserId = ref<string | null>(null)
const editingUserRole = ref<AdminRole | ''>('')
const editForm = reactive({ email: '', name: '', customerId: '' })
const editCustomerSearch = ref('')
const editCustomerName = ref('')
const editFormError = ref<string | null>(null)

let customerSearchDebounceHandle: ReturnType<typeof setTimeout> | null = null

function debouncedCustomerSearch(term: string) {
  if (customerSearchDebounceHandle) {
    clearTimeout(customerSearchDebounceHandle)
  }
  customerSearchDebounceHandle = setTimeout(() => {
    customerSearchDebounceHandle = null
    void customersStore.fetch({ search: term, page: 1, pageSize: 10 })
  }, 300)
}

watch(createCustomerSearch, (term) => {
  if (!createForm.customerId) {
    debouncedCustomerSearch(term)
  }
})

watch(editCustomerSearch, (term) => {
  if (!editForm.customerId) {
    debouncedCustomerSearch(term)
  }
})

function onCreateCustomerInput(value: string) {
  createForm.customerId = ''
  createCustomerName.value = ''
  createCustomerSearch.value = value
}

function onEditCustomerInput(value: string) {
  editForm.customerId = ''
  editCustomerName.value = ''
  editCustomerSearch.value = value
}

function selectCreateCustomer(id: string, fullName: string) {
  createForm.customerId = id
  createCustomerName.value = fullName
  createCustomerSearch.value = fullName
  customersStore.items = []
}

function selectEditCustomer(id: string, fullName: string) {
  editForm.customerId = id
  editCustomerName.value = fullName
  editCustomerSearch.value = fullName
  customersStore.items = []
}

const createPasswordValid = computed(() => createForm.password.length >= 8)
const createEmailValid = computed(() => /\S+@\S+\.\S+/.test(createForm.email))
const createCustomerValid = computed(() => createForm.role !== 'customer' || !!createForm.customerId)
const createFormValid = computed(
  () =>
    createEmailValid.value &&
    createPasswordValid.value &&
    createForm.name.trim().length > 0 &&
    createCustomerValid.value,
)

function openCreate() {
  createForm.email = ''
  createForm.password = ''
  createForm.name = ''
  createForm.role = 'agent'
  createForm.customerId = ''
  createCustomerSearch.value = ''
  createCustomerName.value = ''
  createFormError.value = null
  isCreateOpen.value = true
}

function closeCreate() {
  isCreateOpen.value = false
}

async function submitCreate() {
  if (!createFormValid.value) {
    createFormError.value = !createEmailValid.value
      ? 'invalid_email'
      : !createPasswordValid.value
        ? 'passwordTooShort'
        : !createCustomerValid.value
          ? 'customer_id_required'
          : 'name_required'
    return
  }
  try {
    await store.create({
      email: createForm.email,
      password: createForm.password,
      name: createForm.name,
      role: createForm.role,
      customerId: createForm.role === 'customer' ? createForm.customerId : undefined,
    })
    isCreateOpen.value = false
  } catch {
    createFormError.value = store.mutateError
  }
}

function openEdit(user: AdminUserListItem) {
  editingUserId.value = user.id
  editingUserRole.value = user.role
  editForm.email = user.email
  editForm.name = user.name
  editForm.customerId = ''
  editCustomerSearch.value = ''
  editCustomerName.value = ''
  editFormError.value = null
  isEditOpen.value = true
}

function closeEdit() {
  isEditOpen.value = false
  editingUserId.value = null
}

async function submitEdit() {
  if (!editingUserId.value) {
    return
  }
  try {
    await store.update(editingUserId.value, {
      email: editForm.email,
      name: editForm.name,
      customerId: editForm.customerId || undefined,
    })
    isEditOpen.value = false
    editingUserId.value = null
  } catch {
    editFormError.value = store.mutateError
  }
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
  void store.loadRoles()
})
</script>

<template>
  <div class="users-admin-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('security.users.title') }}</h1>
      </div>
      <AppButton type="button" @click="openCreate">{{ t('security.users.createUser') }}</AppButton>
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
            <th>{{ t('security.users.columns.permissions') }}</th>
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
            <td class="permissions-cell">
              <AppBadge v-for="permission in store.permissionsFor(user.role)" :key="permission" tone="neutral">
                {{ permission }}
              </AppBadge>
            </td>
            <td>
              <AppBadge :tone="user.isDisabled ? 'danger' : 'success'">
                {{ user.isDisabled ? t('security.users.disabled') : t('security.users.enabled') }}
              </AppBadge>
            </td>
            <td class="row-actions">
              <AppButton
                v-if="!user.isDisabled"
                variant="ghost"
                size="sm"
                type="button"
                :disabled="isSelf(user) || store.mutating"
                :title="isSelf(user) ? t('security.users.cannotDeactivateSelf') : undefined"
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
              <AppButton variant="ghost" size="sm" type="button" @click="openEdit(user)">
                {{ t('security.users.editUser') }}
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

    <AppDialog v-if="isCreateOpen" :title="t('security.users.createUser')" @close="closeCreate">
      <form class="user-form" @submit.prevent="submitCreate">
        <AppAlert v-if="createFormError" tone="danger">
          {{ t(`security.users.errors.${createFormError}`, createFormError) }}
        </AppAlert>
        <AppInput v-model="createForm.email" type="email" :label="t('security.users.fields.email')" required />
        <AppInput
          v-model="createForm.password"
          type="password"
          :label="t('security.users.fields.password')"
          :help="t('security.users.fields.passwordHelp')"
          required
        />
        <AppInput v-model="createForm.name" :label="t('security.users.fields.name')" required />
        <div class="field">
          <label for="create-user-role">{{ t('security.users.filters.role') }}</label>
          <select id="create-user-role" v-model="createForm.role">
            <option v-for="role in ROLES" :key="role" :value="role">{{ role }}</option>
          </select>
        </div>
        <div v-if="createForm.role === 'customer'" class="field customer-field">
          <AppInput
            id="create-user-customer"
            :label="t('security.users.fields.customer')"
            :help="t('security.users.fields.customerHelp')"
            type="text"
            autocomplete="off"
            :model-value="createCustomerSearch"
            @update:model-value="onCreateCustomerInput"
          />
          <ul
            v-if="!createForm.customerId && customersStore.items.length > 0"
            class="customer-suggestions"
          >
            <li
              v-for="customer in customersStore.items"
              :key="customer.id"
              @click="selectCreateCustomer(customer.id, customer.fullName)"
            >
              {{ customer.fullName }} — {{ customer.email }}
            </li>
          </ul>
        </div>
        <div class="form-actions">
          <AppButton type="button" variant="secondary" @click="closeCreate">{{ t('common.cancel') }}</AppButton>
          <AppButton type="submit" :disabled="store.mutating">{{ t('security.users.createUser') }}</AppButton>
        </div>
      </form>
    </AppDialog>

    <AppDialog v-if="isEditOpen" :title="t('security.users.editUser')" @close="closeEdit">
      <form class="user-form" @submit.prevent="submitEdit">
        <AppAlert v-if="editFormError" tone="danger">
          {{ t(`security.users.errors.${editFormError}`, editFormError) }}
        </AppAlert>
        <AppInput v-model="editForm.email" type="email" :label="t('security.users.fields.email')" required />
        <AppInput v-model="editForm.name" :label="t('security.users.fields.name')" required />
        <div v-if="editingUserRole === 'customer'" class="field customer-field">
          <AppInput
            id="edit-user-customer"
            :label="t('security.users.fields.customer')"
            :help="t('security.users.fields.customerRelinkHelp')"
            type="text"
            autocomplete="off"
            :model-value="editCustomerSearch"
            @update:model-value="onEditCustomerInput"
          />
          <ul
            v-if="!editForm.customerId && customersStore.items.length > 0"
            class="customer-suggestions"
          >
            <li
              v-for="customer in customersStore.items"
              :key="customer.id"
              @click="selectEditCustomer(customer.id, customer.fullName)"
            >
              {{ customer.fullName }} — {{ customer.email }}
            </li>
          </ul>
        </div>
        <div class="form-actions">
          <AppButton type="button" variant="secondary" @click="closeEdit">{{ t('common.cancel') }}</AppButton>
          <AppButton type="submit" :disabled="store.mutating">{{ t('common.save') }}</AppButton>
        </div>
      </form>
    </AppDialog>
  </div>
</template>

<style scoped>
.users-admin-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.row-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.permissions-cell {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-1);
  max-width: 16rem;
}

.user-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.customer-field {
  position: relative;
}

.customer-suggestions {
  list-style: none;
  margin: 0;
  padding: 0;
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  max-height: 12rem;
  overflow-y: auto;
  box-shadow: var(--shadow-md);
}

.customer-suggestions li {
  padding: var(--space-2) var(--space-3);
  cursor: pointer;
}

.customer-suggestions li:hover {
  background: #f5fbf9;
}
</style>
