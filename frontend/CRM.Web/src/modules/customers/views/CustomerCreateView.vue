<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'

const { t } = useI18n()
const router = useRouter()
const store = useCustomersStore()

const fullName = ref('')
const email = ref('')
const phone = ref('')
const company = ref('')
const fieldError = ref<string | null>(null)

const EMAIL_FORMAT_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate(): boolean {
  if (!fullName.value.trim()) {
    fieldError.value = t('customers.errors.fullNameRequired')
    return false
  }
  if (!EMAIL_FORMAT_PATTERN.test(email.value.trim())) {
    fieldError.value = t('customers.errors.emailInvalid')
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
    await store.create({
      fullName: fullName.value.trim(),
      email: email.value.trim(),
      phone: phone.value.trim() || null,
      company: company.value.trim() || null,
    })
    router.push({ name: 'customers' })
  } catch {
    // store.createError already carries the i18n key to display.
  }
}

function onCancel() {
  router.push({ name: 'customers' })
}
</script>

<template>
  <div class="customer-create-view"><div class="page-heading"><div><p class="eyebrow">{{ t('customers.title') }}</p><h1>{{ t('customers.create.title') }}</h1></div></div>

    <form class="surface form-surface" novalidate @submit.prevent="onSubmit"><div class="form-grid">
      <div class="field"><label for="customer-fullName">{{ t('customers.create.fields.fullName') }}</label>
      <input id="customer-fullName" v-model="fullName" type="text" maxlength="200" required />
      </div>

      <div class="field"><label for="customer-email">{{ t('customers.create.fields.email') }}</label>
      <input id="customer-email" v-model="email" type="email" maxlength="320" required />
      </div>

      <div class="field"><label for="customer-phone">{{ t('customers.create.fields.phone') }}</label>
      <input id="customer-phone" v-model="phone" type="tel" maxlength="32" />
      </div>

      <div class="field"><label for="customer-company">{{ t('customers.create.fields.company') }}</label>
      <input id="customer-company" v-model="company" type="text" maxlength="200" />
      </div></div>

      <p v-if="fieldError" role="alert" aria-live="polite" class="error">
        {{ fieldError }}
      </p>
      <p v-else-if="store.createError" role="alert" aria-live="polite" class="error">
        {{ t(`customers.errors.${store.createError}`) }}
      </p>

      <div class="form-actions">
        <button class="secondary-button" type="button" @click="onCancel">{{ t('customers.create.actions.cancel') }}</button>
        <button type="submit" :disabled="store.creating">
          {{ t('customers.create.actions.submit') }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.customer-create-view {
  max-width: 30rem;
  margin: 4rem auto;
}

form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.actions {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.error {
  color: #b00020;
}
</style>
