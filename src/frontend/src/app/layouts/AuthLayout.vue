<script setup lang="ts">
import { GraduationCap } from 'lucide-vue-next';
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-4 py-8">
    <div class="mb-6 flex items-center gap-2.5 text-brand-primary-900">
      <div
        class="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-primary-900 text-white shadow-sm"
      >
        <GraduationCap class="h-5 w-5" />
      </div>
      <div>
        <p class="text-lg font-bold leading-tight tracking-tight">Elsasa SIS</p>
        <p class="text-xs text-slate-500">School Information System</p>
      </div>
    </div>

    <main class="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
      <slot>
        <router-view />
      </slot>
    </main>
  </div>
</template>
