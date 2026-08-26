<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useCustomersStore()

const id = route.params.id as string

const fullName = ref('')
const email = ref('')
const phone = ref('')
const company = ref('')
const fieldError = ref<string | null>(null)

const EMAIL_FORMAT_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

watch(
  () => store.current,
  (customer) => {
    if (!customer) {
      return
    }
    fullName.value = customer.fullName
    email.value = customer.email
    phone.value = customer.phone ?? ''
    company.value = customer.company ?? ''
  },
)

onMounted(() => {
  void store.getById(id)
})

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
  if (store.updating) {
    return
  }
  if (!validate()) {
    return
  }

  try {
    await store.update(id, {
      fullName: fullName.value.trim(),
      email: email.value.trim(),
      phone: phone.value.trim() || null,
      company: company.value.trim() || null,
    })
    router.push({ name: 'customers' })
  } catch {
    // store.updateError already carries the i18n key to display.
  }
}

function onCancel() {
  router.push({ name: 'customers' })
}
</script>

<template>
  <div class="customer-edit-view"><div class="page-heading"><div><p class="eyebrow">{{ t('customers.title') }}</p><h1>{{ t('customers.edit.title') }}</h1></div></div>

    <LoadingState v-if="store.loadingCurrent" :label="t('customers.edit.loading')" />

    <div v-else-if="store.notFound" class="surface empty-state">
      <p>{{ t('customers.edit.notFound') }}</p>
      <router-link :to="{ name: 'customers' }">{{ t('customers.edit.backToList') }}</router-link>
    </div>

    <form v-else class="surface form-surface" novalidate @submit.prevent="onSubmit">
      <div class="form-grid">
        <AppInput
          id="customer-fullName"
          v-model="fullName"
          :label="t('customers.create.fields.fullName')"
          type="text"
          maxlength="200"
          :placeholder="t('customers.create.fields.fullNamePlaceholder')"
          required
        />
        <AppInput
          id="customer-email"
          v-model="email"
          :label="t('customers.create.fields.email')"
          type="email"
          maxlength="320"
          :placeholder="t('customers.create.fields.emailPlaceholder')"
          required
        />
        <AppInput
          id="customer-phone"
          v-model="phone"
          :label="t('customers.create.fields.phone')"
          type="tel"
          maxlength="32"
          :placeholder="t('customers.create.fields.phonePlaceholder')"
        />
        <AppInput
          id="customer-company"
          v-model="company"
          :label="t('customers.create.fields.company')"
          type="text"
          maxlength="200"
          :placeholder="t('customers.create.fields.companyPlaceholder')"
        />
      </div>

      <AppAlert v-if="fieldError" tone="danger" role="alert" aria-live="polite">
        {{ fieldError }}
      </AppAlert>
      <AppAlert v-else-if="store.updateError" tone="danger" role="alert" aria-live="polite">
        {{ t(`customers.errors.${store.updateError}`) }}
      </AppAlert>

      <div class="form-actions">
        <AppButton variant="secondary" type="button" @click="onCancel">{{ t('customers.edit.actions.cancel') }}</AppButton>
        <AppButton type="submit" :loading="store.updating">
          {{ t('customers.edit.actions.submit') }}
        </AppButton>
      </div>
    </form>
  </div>
</template>

<style scoped>
.customer-edit-view {
  max-width: 30rem;
  margin: var(--space-8) auto;
}
</style>
