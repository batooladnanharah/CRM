<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTicketsStore } from '@/stores/tickets'
import { useCustomersStore } from '@/stores/customers'
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

function onCustomerInput(event: Event) {
  selectedCustomerId.value = ''
  selectedCustomerName.value = ''
  customerSearchTerm.value = (event.target as HTMLInputElement).value
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
          <label for="ticket-customer">{{ t('tickets.create.fields.customer') }}</label>
          <input
            id="ticket-customer"
            type="text"
            autocomplete="off"
            :value="customerSearchTerm"
            @input="onCustomerInput"
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

        <div class="field">
          <label for="ticket-title">{{ t('tickets.create.fields.title') }}</label>
          <input id="ticket-title" v-model="title" type="text" maxlength="200" required />
        </div>

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

      <p v-if="fieldError" role="alert" aria-live="polite" class="error">
        {{ fieldError }}
      </p>
      <p v-else-if="store.createError === 'customerNotFound'" role="alert" aria-live="polite" class="error">
        {{ t('tickets.create.errors.customerNotFound') }}
      </p>
      <p v-else-if="store.createError" role="alert" aria-live="polite" class="error">
        {{ t('tickets.create.errors.generic') }}
      </p>

      <div class="form-actions">
        <button class="secondary-button" type="button" @click="onCancel">
          {{ t('tickets.create.cancel') }}
        </button>
        <button type="submit" :disabled="store.creating">
          {{ t('tickets.create.submit') }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.ticket-create-view {
  max-width: 30rem;
  margin: 4rem auto;
}

form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.customer-field {
  position: relative;
}

.customer-suggestions {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid #ddd;
  max-height: 12rem;
  overflow-y: auto;
}

.customer-suggestions li {
  padding: 0.5rem;
  cursor: pointer;
}

.customer-suggestions li:hover {
  background: #f5f5f5;
}

.form-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.error {
  color: #b00020;
}
</style>
