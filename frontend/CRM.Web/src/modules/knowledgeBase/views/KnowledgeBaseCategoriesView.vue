<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import { confirm } from '@/composables/useConfirm'
import { useToast } from '@/composables/useToast'
import { ApiError } from '@/api/http'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppDialog from '@/components/ui/AppDialog.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import KnowledgeBaseCategoryForm from '@/modules/knowledgeBase/components/KnowledgeBaseCategoryForm.vue'
import type { KnowledgeBaseCategory } from '@/types/knowledgeBase'

const { t } = useI18n()
const store = useKnowledgeBaseStore()
const toast = useToast()

const isDialogOpen = ref(false)
const editingCategory = ref<KnowledgeBaseCategory | null>(null)
const saving = ref(false)
const savingError = ref<string | null>(null)
const statusChangingId = ref<string | null>(null)

function refetch() {
  return store.fetchCategories().catch(() => {})
}

onMounted(() => {
  refetch()
})

function openCreateDialog() {
  editingCategory.value = null
  savingError.value = null
  isDialogOpen.value = true
}

function openEditDialog(category: KnowledgeBaseCategory) {
  editingCategory.value = category
  savingError.value = null
  isDialogOpen.value = true
}

function closeDialog() {
  isDialogOpen.value = false
  editingCategory.value = null
  savingError.value = null
}

async function onSave(payload: { name: string; description: string | null; isActive: boolean }) {
  saving.value = true
  savingError.value = null

  try {
    if (editingCategory.value) {
      await store.updateCategory(editingCategory.value.id, {
        name: payload.name,
        description: payload.description,
      })
    } else {
      await store.createCategory({ name: payload.name, description: payload.description })
    }
    closeDialog()
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      savingError.value = t('knowledgeBase.categories.errors.duplicate')
      toast.error(t('knowledgeBase.categories.errors.duplicate'))
    } else {
      savingError.value = t('knowledgeBase.categories.errors.saveFailed')
    }
  } finally {
    saving.value = false
  }
}

async function onToggleActive(category: KnowledgeBaseCategory) {
  if (category.isActive) {
    const confirmed = await confirm({
      message: t('knowledgeBase.categories.deactivateConfirm'),
      tone: 'danger',
      confirmLabel: t('knowledgeBase.categories.deactivate'),
    })
    if (!confirmed) {
      return
    }
  }

  statusChangingId.value = category.id
  try {
    if (category.isActive) {
      await store.deactivateCategory(category.id)
    } else {
      await store.activateCategory(category.id)
    }
  } catch {
    // error surfaced via store.categoriesError
  } finally {
    statusChangingId.value = null
  }
}
</script>

<template>
  <div class="knowledge-base-categories-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('knowledgeBase.categories.title') }}</h1>
      </div>
      <AppButton type="button" @click="openCreateDialog">
        {{ t('knowledgeBase.categories.new') }}
      </AppButton>
    </div>

    <AppAlert v-if="store.categoriesError" tone="danger" role="alert" class="kb-error-alert">
      {{ t('knowledgeBase.categories.errors.loadFailed') }}
      <AppButton variant="secondary" size="sm" type="button" @click="refetch">{{ t('common.retry') }}</AppButton>
    </AppAlert>

    <LoadingState v-if="store.categoriesLoading && store.categories.length === 0" :label="t('knowledgeBase.categories.loading')" />
    <EmptyState v-else-if="store.categories.length === 0" :description="t('knowledgeBase.categories.empty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('knowledgeBase.categories.columns.name') }}</th>
            <th>{{ t('knowledgeBase.categories.columns.status') }}</th>
            <th>{{ t('knowledgeBase.categories.columns.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="category in store.categories" :key="category.id">
            <td>
              <strong>{{ category.name }}</strong>
              <p v-if="category.description" class="category-description">{{ category.description }}</p>
            </td>
            <td>
              {{ category.isActive ? t('knowledgeBase.categories.status.active') : t('knowledgeBase.categories.status.inactive') }}
            </td>
            <td>
              <AppButton variant="ghost" size="sm" type="button" @click="openEditDialog(category)">
                {{ t('knowledgeBase.categories.edit') }}
              </AppButton>
              <AppButton
                variant="ghost"
                size="sm"
                type="button"
                :disabled="statusChangingId === category.id"
                @click="onToggleActive(category)"
              >
                {{ category.isActive ? t('knowledgeBase.categories.deactivate') : t('knowledgeBase.categories.activate') }}
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppDialog
      v-if="isDialogOpen"
      :title="editingCategory ? t('knowledgeBase.categories.editTitle') : t('knowledgeBase.categories.new')"
      @close="closeDialog"
    >
      <AppAlert v-if="savingError" tone="danger" role="alert">{{ savingError }}</AppAlert>
      <KnowledgeBaseCategoryForm
        :category="editingCategory"
        :saving="saving"
        @save="onSave"
        @cancel="closeDialog"
      />
    </AppDialog>
  </div>
</template>

<style scoped>
.knowledge-base-categories-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.kb-error-alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.category-description {
  margin: var(--space-1) 0 0;
  color: var(--muted);
  font-size: var(--font-size-sm);
}
</style>
