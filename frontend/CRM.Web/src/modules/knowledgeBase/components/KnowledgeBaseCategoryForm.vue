<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import AppButton from '@/components/ui/AppButton.vue'
import type { KnowledgeBaseCategory } from '@/types/knowledgeBase'

const NAME_MAX_LENGTH = 120
const DESCRIPTION_MAX_LENGTH = 1000

const props = withDefaults(defineProps<{
  category?: KnowledgeBaseCategory | null
  saving?: boolean
}>(), {
  category: null,
  saving: false,
})

const emit = defineEmits<{
  save: [payload: { name: string; description: string | null; isActive: boolean }]
  cancel: []
}>()

const { t } = useI18n()

const name = ref('')
const description = ref('')
const isActive = ref(true)
const nameError = ref<string | null>(null)
const descriptionError = ref<string | null>(null)

function resetFromProps() {
  name.value = props.category?.name ?? ''
  description.value = props.category?.description ?? ''
  isActive.value = props.category?.isActive ?? true
  nameError.value = null
  descriptionError.value = null
}

watch(() => props.category, resetFromProps, { immediate: true })

function validate(): boolean {
  const trimmedName = name.value.trim()
  const trimmedDescription = description.value.trim()

  nameError.value = trimmedName.length === 0
    ? t('knowledgeBase.categories.validation.nameRequired')
    : trimmedName.length > NAME_MAX_LENGTH
      ? t('knowledgeBase.categories.validation.nameTooLong')
      : null

  descriptionError.value = trimmedDescription.length > DESCRIPTION_MAX_LENGTH
    ? t('knowledgeBase.categories.validation.descriptionTooLong')
    : null

  return nameError.value === null && descriptionError.value === null
}

function onSubmit() {
  if (!validate()) {
    return
  }
  const trimmedDescription = description.value.trim()
  emit('save', {
    name: name.value.trim(),
    description: trimmedDescription.length > 0 ? trimmedDescription : null,
    isActive: isActive.value,
  })
}
</script>

<template>
  <form class="kb-category-form" @submit.prevent="onSubmit">
    <div class="field">
      <label for="kb-category-name">{{ t('knowledgeBase.categories.fields.name') }}</label>
      <input
        id="kb-category-name"
        v-model="name"
        type="text"
        maxlength="120"
        :placeholder="t('knowledgeBase.categories.fields.namePlaceholder')"
      />
      <p v-if="nameError" role="alert" class="field-error">{{ nameError }}</p>
    </div>
    <div class="field">
      <label for="kb-category-description">{{ t('knowledgeBase.categories.fields.description') }}</label>
      <textarea id="kb-category-description" v-model="description" maxlength="1000" rows="4"></textarea>
      <p v-if="descriptionError" role="alert" class="field-error">{{ descriptionError }}</p>
    </div>
    <div v-if="props.category" class="field">
      <label for="kb-category-status">{{ t('knowledgeBase.categories.fields.status') }}</label>
      <select id="kb-category-status" v-model="isActive">
        <option :value="true">{{ t('knowledgeBase.categories.status.active') }}</option>
        <option :value="false">{{ t('knowledgeBase.categories.status.inactive') }}</option>
      </select>
    </div>
    <div class="form-actions">
      <AppButton type="submit" size="sm" :disabled="props.saving">
        {{ props.saving ? t('knowledgeBase.categories.saving') : t('common.save') }}
      </AppButton>
      <AppButton type="button" variant="secondary" size="sm" @click="emit('cancel')">
        {{ t('common.cancel') }}
      </AppButton>
    </div>
  </form>
</template>

<style scoped>
.kb-category-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.field-error {
  color: var(--color-status-danger);
  font-size: var(--font-size-sm);
}

.form-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
