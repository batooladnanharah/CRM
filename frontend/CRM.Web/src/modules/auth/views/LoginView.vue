<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const fieldError = ref<string | null>(null)

const EMAIL_FORMAT_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate(): boolean {
  const trimmedEmail = email.value.trim()

  if (!trimmedEmail) {
    fieldError.value = t('login.errors.emailRequired')
    return false
  }
  if (!EMAIL_FORMAT_PATTERN.test(trimmedEmail)) {
    fieldError.value = t('login.errors.emailFormat')
    return false
  }
  if (!password.value) {
    fieldError.value = t('login.errors.passwordRequired')
    return false
  }

  fieldError.value = null
  return true
}

async function onSubmit() {
  if (authStore.status === 'loading') {
    return
  }
  if (!validate()) {
    return
  }

  try {
    await authStore.login(email.value.trim(), password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.replace(redirect)
  } catch {
    // authStore.errorMessage already carries the i18n key to display.
  }
}

function submitErrorKey(): string | null {
  if (fieldError.value) {
    return null // field-level error is shown separately below.
  }
  if (authStore.status === 'error' && authStore.errorMessage) {
    return `login.errors.${authStore.errorMessage}`
  }
  return null
}
</script>

<template>
  <div class="login-view">
    <div class="login-brand"><span class="brand-mark">C</span><strong>{{ t('app.name') }}</strong></div>
    <div class="login-intro">
      <p class="eyebrow">{{ t('app.name') }}</p>
      <h1>{{ t('login.title') }}</h1>
      <p>{{ t('login.subtitle') }}</p>
    </div>

    <form class="surface login-form" novalidate @submit.prevent="onSubmit">
      <AppInput
        id="login-email"
        v-model="email"
        :label="t('login.email')"
        type="email"
        autocomplete="username"
        required
      />

      <AppInput
        id="login-password"
        v-model="password"
        :label="t('login.password')"
        type="password"
        autocomplete="current-password"
        required
      />

      <AppAlert v-if="fieldError" tone="danger" role="alert" aria-live="polite">
        {{ fieldError }}
      </AppAlert>
      <AppAlert v-else-if="submitErrorKey()" tone="danger" role="alert" aria-live="polite">
        {{ t(submitErrorKey()!) }}
      </AppAlert>

      <AppButton type="submit" :loading="authStore.status === 'loading'">
        {{ authStore.status === 'loading' ? t('login.submitting') : t('login.submit') }}
      </AppButton>
    </form>
  </div>
</template>

<style scoped>
.login-view {
  width: min(440px, calc(100% - 40px));
  margin: 0 auto;
  padding: 8vh 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-7);
}

.login-brand { display: flex; align-items: center; gap: 11px; color: var(--navy); font: 700 1.15rem Arial, sans-serif; }
.login-intro h1 { font-size: 2.7rem; }
.login-intro p:last-child { color: var(--muted); }
.login-form { display: flex; flex-direction: column; gap: var(--space-5); padding: var(--space-7); }
.login-form :deep(.ui-button) { width: 100%; margin-top: var(--space-1); }
.login-view .brand-mark { color: var(--navy); background: #d2eee6; }

@media (max-width: 480px) {
  .login-form { padding: var(--space-5); }
  .login-intro h1 { font-size: 2.2rem; }
}
</style>
