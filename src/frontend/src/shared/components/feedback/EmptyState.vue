<script setup lang="ts">
import { computed } from 'vue';
import { Inbox, SearchX, AlertCircle, ShieldAlert } from 'lucide-vue-next';

export type EmptyStateVariant = 'empty' | 'no-results' | 'error' | 'no-permission';

const props = withDefaults(
  defineProps<{
    title: string;
    description?: string;
    variant?: EmptyStateVariant;
  }>(),
  {
    description: undefined,
    variant: 'empty'
  }
);

const iconComponent = computed(() => {
  switch (props.variant) {
    case 'no-results':
      return SearchX;
    case 'error':
      return AlertCircle;
    case 'no-permission':
      return ShieldAlert;
    default:
      return Inbox;
  }
});

const iconContainerClass = computed(() => {
  switch (props.variant) {
    case 'error':
      return 'bg-rose-50 text-rose-600 border-rose-200';
    case 'no-permission':
      return 'bg-amber-50 text-amber-600 border-amber-200';
    default:
      return 'bg-slate-100 text-slate-500 border-slate-200';
  }
});
</script>

<template>
  <div
    class="flex flex-col items-center justify-center rounded-lg border border-dashed border-slate-200 bg-white px-6 py-12 text-center"
  >
    <div
      class="mb-3 flex h-12 w-12 items-center justify-center rounded-full border"
      :class="iconContainerClass"
    >
      <component :is="iconComponent" class="h-6 w-6" />
    </div>
    <h3 class="text-sm font-semibold text-slate-900">{{ title }}</h3>
    <p v-if="description" class="mt-1 max-w-md text-sm text-slate-500">
      {{ description }}
    </p>
    <div v-if="$slots.default" class="mt-4">
      <slot />
    </div>
  </div>
</template>
