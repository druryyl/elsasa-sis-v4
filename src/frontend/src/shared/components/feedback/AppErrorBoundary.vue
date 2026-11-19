<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';
import { AlertTriangle, RotateCcw } from 'lucide-vue-next';

withDefaults(
  defineProps<{
    fallbackTitle?: string;
    fallbackMessage?: string;
  }>(),
  {
    fallbackTitle: 'Something went wrong',
    fallbackMessage: 'This section encountered an unexpected error. Please refresh or try again.'
  }
);

const hasError = ref(false);
const capturedError = ref<Error | null>(null);

onErrorCaptured((err: Error, _instance, info) => {
  hasError.value = true;
  capturedError.value = err;

  console.error('[AppErrorBoundary] Captured error:', {
    error: err.message,
    componentInfo: info,
    stack: err.stack
  });

  return false;
});

function reset(): void {
  hasError.value = false;
  capturedError.value = null;
}
</script>

<template>
  <slot v-if="!hasError" />
  <div
    v-else
    class="flex flex-col items-center justify-center gap-3 rounded-lg border border-rose-200 bg-rose-50 p-8 text-center"
    role="alert"
  >
    <div class="flex h-10 w-10 items-center justify-center rounded-full bg-rose-100 text-rose-600">
      <AlertTriangle class="h-5 w-5" />
    </div>
    <div class="space-y-1">
      <h3 class="text-sm font-semibold text-rose-900">{{ fallbackTitle }}</h3>
      <p class="max-w-md text-sm text-rose-700">{{ fallbackMessage }}</p>
    </div>
    <button
      type="button"
      class="inline-flex items-center gap-1.5 rounded-md bg-rose-600 px-3.5 py-2 text-sm font-medium text-white transition-colors hover:bg-rose-700 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:ring-offset-2"
      @click="reset"
    >
      <RotateCcw class="h-4 w-4" />
      <span>Try Again</span>
    </button>
  </div>
</template>
