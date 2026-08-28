<script setup lang="ts">
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const { t } = useI18n()
const router = useRouter()
const store = useCustomerPortalStore()

function loadCategories() {
  void store.fetchPortalCategories()
}

function onCardClick(id: string) {
  router.push({ name: 'portal-knowledge-base-category', params: { id } })
}

onMounted(loadCategories)
</script>

<template>
  <div class="portal-kb-list-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.helpCentre.title') }}</h1>
      </div>
    </div>

    <LoadingState v-if="store.portalCategoriesLoading" :label="t('portal.helpCentre.loading')" />
    <AppAlert v-else-if="store.portalCategoriesError" tone="danger" class="portal-error">
      {{ t('portal.helpCentre.error') }}
      <AppButton variant="secondary" size="sm" type="button" @click="loadCategories">
        {{ t('portal.helpCentre.retry') }}
      </AppButton>
    </AppAlert>
    <EmptyState v-else-if="store.portalCategories.length === 0" :title="t('knowledgeBase.categories.emptyPortal')" />

    <div v-else class="category-grid">
      <button
        v-for="category in store.portalCategories"
        :key="category.id"
        type="button"
        class="surface category-card"
        @click="onCardClick(category.id)"
      >
        <h3 dir="auto">{{ category.name }}</h3>
        <p v-if="category.description" class="category-card-description" dir="auto">{{ category.description }}</p>
        <p class="category-card-count">
          {{ t('knowledgeBase.categories.articleCount', { count: category.articleCount }) }}
        </p>
      </button>
    </div>
  </div>
</template>

<style scoped>
.portal-kb-list-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.portal-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr));
  gap: var(--space-4);
}

.category-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-2);
  padding: var(--space-5);
  text-align: start;
  cursor: pointer;
}

.category-card-description {
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.category-card-count {
  font-weight: 600;
}
</style>
