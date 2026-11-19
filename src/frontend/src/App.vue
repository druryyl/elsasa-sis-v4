<script setup lang="ts">
import Toast from 'primevue/toast';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
</script>

<template>
  <AppErrorBoundary>
    <Toast position="top-right" />
    <router-view />
  </AppErrorBoundary>
</template>
