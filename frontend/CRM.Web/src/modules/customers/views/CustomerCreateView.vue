<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'

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

    <form class="surface form-surface" novalidate @submit.prevent="onSubmit">
      <div class="form-grid">
        <AppInput
          id="customer-fullName"
          v-model="fullName"
          :label="t('customers.create.fields.fullName')"
          type="text"
          maxlength="200"
          required
        />
        <AppInput
          id="customer-email"
          v-model="email"
          :label="t('customers.create.fields.email')"
          type="email"
          maxlength="320"
          required
        />
        <AppInput
          id="customer-phone"
          v-model="phone"
          :label="t('customers.create.fields.phone')"
          type="tel"
          maxlength="32"
        />
        <AppInput
          id="customer-company"
          v-model="company"
          :label="t('customers.create.fields.company')"
          type="text"
          maxlength="200"
        />
      </div>

      <AppAlert v-if="fieldError" tone="danger" role="alert" aria-live="polite">
        {{ fieldError }}
      </AppAlert>
      <AppAlert v-else-if="store.createError" tone="danger" role="alert" aria-live="polite">
        {{ t(`customers.errors.${store.createError}`) }}
      </AppAlert>

      <div class="form-actions">
        <AppButton variant="secondary" type="button" @click="onCancel">{{ t('customers.create.actions.cancel') }}</AppButton>
        <AppButton type="submit" :loading="store.creating">
          {{ t('customers.create.actions.submit') }}
        </AppButton>
      </div>
    </form>
  </div>
</template>

<style scoped>
.customer-create-view {
  max-width: 30rem;
  margin: var(--space-8) auto;
}
</style>
