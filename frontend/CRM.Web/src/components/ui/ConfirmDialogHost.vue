<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { confirmState, resolveConfirm } from '@/composables/useConfirm'
import AppDialog from '@/components/ui/AppDialog.vue'
import AppButton from '@/components/ui/AppButton.vue'

const { t } = useI18n()
</script>

<template>
  <AppDialog v-if="confirmState.open" :title="confirmState.title ?? t('common.confirmTitle')" @close="resolveConfirm(false)">
    <p class="confirm-message">{{ confirmState.message }}</p>
    <template #footer>
      <AppButton variant="secondary" type="button" @click="resolveConfirm(false)">
        {{ confirmState.cancelLabel ?? t('common.cancel') }}
      </AppButton>
      <AppButton
        :variant="confirmState.tone === 'danger' ? 'danger' : 'primary'"
        type="button"
        @click="resolveConfirm(true)"
      >
        {{ confirmState.confirmLabel ?? t('common.confirm') }}
      </AppButton>
    </template>
  </AppDialog>
</template>

<style scoped>
.confirm-message {
  margin: 0;
  color: var(--text-primary, var(--ink));
  font: 400 14px var(--font-sans, Arial, sans-serif);
}
</style>
