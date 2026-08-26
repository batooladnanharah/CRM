<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTicketsStore } from '@/stores/tickets'
import { useCustomersStore } from '@/stores/customers'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import type { TicketPriority } from '@/types/tickets'

const { t } = useI18n()
const router = useRouter()
const store = useTicketsStore()
const customersStore = useCustomersStore()

const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const customerSearchTerm = ref('')
const selectedCustomerId = ref('')
const selectedCustomerName = ref('')
const title = ref('')
const description = ref('')
const priority = ref<TicketPriority>('Normal')
const fieldError = ref<string | null>(null)

let customerSearchDebounceHandle: ReturnType<typeof setTimeout> | null = null

watch(customerSearchTerm, (term) => {
  if (customerSearchDebounceHandle) {
    clearTimeout(customerSearchDebounceHandle)
  }

  if (selectedCustomerId.value) {
    return
  }

  customerSearchDebounceHandle = setTimeout(() => {
    customerSearchDebounceHandle = null
    void customersStore.fetch({ search: term, page: 1, pageSize: 10 })
  }, 300)
})

function onCustomerInput(value: string) {
  selectedCustomerId.value = ''
  selectedCustomerName.value = ''
  customerSearchTerm.value = value
}

function selectCustomer(id: string, fullName: string) {
  selectedCustomerId.value = id
  selectedCustomerName.value = fullName
  customerSearchTerm.value = fullName
  customersStore.items = []
}

function validate(): boolean {
  if (!selectedCustomerId.value) {
    fieldError.value = t('tickets.create.errors.customerRequired')
    return false
  }
  if (!title.value.trim()) {
    fieldError.value = t('tickets.create.errors.titleRequired')
    return false
  }
  if (title.value.trim().length > 200) {
    fieldError.value = t('tickets.create.errors.titleTooLong')
    return false
  }
  if (!description.value.trim()) {
    fieldError.value = t('tickets.create.errors.descriptionRequired')
    return false
  }
  if (description.value.trim().length > 4000) {
    fieldError.value = t('tickets.create.errors.descriptionTooLong')
    return false
  }

  fieldError.value = null
  return true
}

async function onSubmit() {
  if (store.creating) {
    return
  }
  if (!validate()) {
    return
  }

  try {
    const ticket = await store.create({
      customerId: selectedCustomerId.value,
      title: title.value.trim(),
      description: description.value.trim(),
      priority: priority.value,
    })
    router.push({ name: 'ticket-details', params: { id: ticket.id } })
  } catch {
    // store.createError already carries the i18n key to display.
  }
}

function onCancel() {
  router.push({ name: 'tickets-list' })
}
</script>

<template>
  <div class="ticket-create-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('tickets.title') }}</p>
        <h1>{{ t('tickets.create.title') }}</h1>
      </div>
    </div>

    <form class="surface form-surface" novalidate @submit.prevent="onSubmit">
      <div class="form-grid">
        <div class="field customer-field">
          <AppInput
            id="ticket-customer"
            :label="t('tickets.create.fields.customer')"
            type="text"
            autocomplete="off"
            :model-value="customerSearchTerm"
            @update:model-value="onCustomerInput"
          />
          <ul
            v-if="!selectedCustomerId && customersStore.items.length > 0"
            class="customer-suggestions"
          >
            <li
              v-for="customer in customersStore.items"
              :key="customer.id"
              @click="selectCustomer(customer.id, customer.fullName)"
            >
              {{ customer.fullName }} — {{ customer.email }}
            </li>
          </ul>
        </div>

        <AppInput
          id="ticket-title"
          v-model="title"
          :label="t('tickets.create.fields.title')"
          type="text"
          maxlength="200"
          required
        />

        <div class="field">
          <label for="ticket-description">{{ t('tickets.create.fields.description') }}</label>
          <textarea
            id="ticket-description"
            v-model="description"
            maxlength="4000"
            rows="4"
            required
          ></textarea>
        </div>

        <div class="field">
          <label for="ticket-priority">{{ t('tickets.create.fields.priority') }}</label>
          <select id="ticket-priority" v-model="priority">
            <option v-for="option in PRIORITIES" :key="option" :value="option">
              {{ t(`tickets.priorities.${option}`) }}
            </option>
          </select>
        </div>
      </div>

      <AppAlert v-if="fieldError" tone="danger" role="alert" aria-live="polite">
        {{ fieldError }}
      </AppAlert>
      <AppAlert v-else-if="store.createError === 'customerNotFound'" tone="danger" role="alert" aria-live="polite">
        {{ t('tickets.create.errors.customerNotFound') }}
      </AppAlert>
      <AppAlert v-else-if="store.createError" tone="danger" role="alert" aria-live="polite">
        {{ t('tickets.create.errors.generic') }}
      </AppAlert>

      <div class="form-actions">
        <AppButton variant="secondary" type="button" @click="onCancel">
          {{ t('tickets.create.cancel') }}
        </AppButton>
        <AppButton type="submit" :loading="store.creating">
          {{ t('tickets.create.submit') }}
        </AppButton>
      </div>
    </form>
  </div>
</template>

<style scoped>
.ticket-create-view {
  max-width: 30rem;
  margin: var(--space-8) auto;
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
