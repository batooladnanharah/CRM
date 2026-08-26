<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <div class="forbidden-view">
    <h1>{{ t('forbidden.title') }}</h1>
    <p>{{ t('forbidden.message') }}</p>
  </div>
</template>

<style scoped>
.forbidden-view {
  max-width: 40rem;
  margin: 4rem auto;
}
</style>
