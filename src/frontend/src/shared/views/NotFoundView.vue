<script setup lang="ts">
import { useRouter } from 'vue-router';
import { FileQuestion, ArrowLeft } from 'lucide-vue-next';

const router = useRouter();

function goHome(): void {
  void router.push('/');
}
</script>

<template>
  <main class="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-4 text-center">
    <div class="w-full max-w-md rounded-xl border border-slate-200 bg-white p-8 shadow-sm">
      <div
        class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-600"
      >
        <FileQuestion class="h-6 w-6" />
      </div>
      <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">404 Not Found</p>
      <h1 class="mt-1 text-xl font-semibold text-slate-900">Page Not Found</h1>
      <p class="mt-2 text-sm text-slate-600">
        The requested page could not be found or may have been moved.
      </p>
      <button
        type="button"
        class="mt-6 inline-flex items-center gap-2 rounded-lg bg-brand-accent-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-accent-500"
        @click="goHome"
      >
        <ArrowLeft class="h-4 w-4" />
        <span>Return to Workspace</span>
      </button>
    </div>
  </main>
</template>
