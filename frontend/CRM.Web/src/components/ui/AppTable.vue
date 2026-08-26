<script setup lang="ts">
withDefaults(defineProps<{ items: unknown[] }>(), {})
</script>

<template>
  <div class="ui-table table-wrap">
    <table class="ui-table__table">
      <thead>
        <tr><slot name="head" /></tr>
      </thead>
      <tbody>
        <tr v-for="(item, index) in items" :key="index">
          <slot name="row" :item="item" :index="index" />
        </tr>
      </tbody>
    </table>

    <div class="ui-table__cards">
      <div v-for="(item, index) in items" :key="index" class="ui-table__card surface">
        <slot name="row-card" :item="item" :index="index" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.ui-table__cards {
  display: none;
}

@media (max-width: 640px) {
  .ui-table__table {
    display: none;
  }

  .ui-table__cards {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
  }

  .ui-table__card {
    padding: var(--space-4);
  }
}
</style>
