<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

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

function loadArticle() {
  if (!id) {
    return
  }
  void store.fetchArticle(id)
}

onMounted(loadArticle)

function onBack() {
  router.push({ name: 'portal-knowledge-base-list' })
}
</script>

<template>
  <div class="portal-kb-article-view">
    <LoadingState v-if="store.articleLoading" :label="t('portal.helpCentre.loading')" />

    <!-- A missing article and a draft/archived article both resolve to a
         404 from the API — both surface the same not-found state here so
         nothing about the article's existence or status leaks. -->
    <div v-else-if="!id || (!store.currentArticle && store.articleError === null)" class="surface state-card">
      <p class="text-body">{{ t('portal.helpCentre.notFound') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('portal.helpCentre.backToList') }}</AppButton>
    </div>

    <div v-else-if="store.articleError || !store.currentArticle" class="surface state-card" role="alert">
      <p class="text-body">{{ t('portal.helpCentre.notFound') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('portal.helpCentre.backToList') }}</AppButton>
    </div>

    <div v-else class="surface article-body">
      <h1 dir="auto">{{ store.currentArticle.title }}</h1>
      <p class="published-meta">
        {{ t('portal.helpCentre.publishedOn', { date: formatDate(store.currentArticle.publishedAtUtc) }) }}
      </p>
      <div class="article-content" dir="auto">{{ store.currentArticle.body }}</div>
      <AppButton variant="ghost" type="button" @click="onBack">{{ t('portal.helpCentre.backToList') }}</AppButton>
    </div>
  </div>
</template>

<style scoped>
.portal-kb-article-view {
  max-width: 40rem;
  margin: var(--space-8) auto;
}

.state-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-6);
}

.article-body {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  padding: var(--space-6);
}

.published-meta {
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.article-content {
  white-space: pre-wrap;
}
</style>
