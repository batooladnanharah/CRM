<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AppShell from '@/components/AppShell.vue'
import ConfirmDialogHost from '@/components/ui/ConfirmDialogHost.vue'
import ToastHost from '@/components/ui/ToastHost.vue'

const route = useRoute()
const authStore = useAuthStore()
const showShell = computed(() => authStore.isAuthenticated && route.name !== 'login')
</script>

<template>
  <AppShell v-if="showShell" />
  <router-view v-else />
  <ConfirmDialogHost />
  <ToastHost />
</template>

<style scoped></style>
