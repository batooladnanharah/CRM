<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const { t } = useI18n()
const { locale } = useLocale()
const route = useRoute()
const router = useRouter()
const store = useCustomerPortalStore()

const id = (route.params.id as string | undefined)?.trim() ?? ''

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function loadArticles() {
  if (!id) {
    return
  }
  void store.fetchPortalCategoryArticles(id)
}

function onRowClick(articleId: string) {
  router.push({ name: 'portal-knowledge-base-article', params: { id: articleId } })
}

function onBack() {
  router.push({ name: 'portal-knowledge-base-list' })
}

onMounted(loadArticles)
</script>

<template>
  <div class="portal-kb-category-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.helpCentre.title') }}</h1>
      </div>
      <AppButton variant="ghost" type="button" @click="onBack">{{ t('portal.helpCentre.backToList') }}</AppButton>
    </div>

    <LoadingState v-if="store.portalCategoryArticlesLoading" :label="t('portal.helpCentre.loading')" />
    <AppAlert v-else-if="store.portalCategoryArticlesError" tone="danger" class="portal-error">
      {{ t('portal.helpCentre.error') }}
      <AppButton variant="secondary" size="sm" type="button" @click="loadArticles">
        {{ t('portal.helpCentre.retry') }}
      </AppButton>
    </AppAlert>
    <EmptyState v-else-if="store.portalCategoryArticles.length === 0" :title="t('knowledgeBase.categories.emptyArticles')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('knowledgeBase.fields.title') }}</th>
            <th>{{ t('portal.helpCentre.publishedOn', { date: '' }).split(' ')[0] }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="article in store.portalCategoryArticles"
            :key="article.id"
            class="clickable-row"
            @click="onRowClick(article.id)"
          >
            <td dir="auto">{{ article.title }}</td>
            <td>{{ formatDate(article.publishedAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.portal-kb-category-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.portal-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.clickable-row {
  cursor: pointer;
}
</style>
