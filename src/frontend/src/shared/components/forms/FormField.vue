<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string;
    fieldId?: string;
    required?: boolean;
    hint?: string;
    error?: string;
  }>(),
  {
    fieldId: undefined,
    required: false,
    hint: undefined,
    error: undefined
  }
);
</script>

<template>
  <div class="space-y-1.5">
    <label :for="fieldId" class="block text-sm font-medium text-slate-700">
      <span>{{ label }}</span>
      <span v-if="required" class="ml-0.5 text-rose-600" aria-hidden="true">*</span>
    </label>

    <slot />

    <p v-if="error" role="alert" class="text-xs font-medium text-rose-600">
      {{ error }}
    </p>
    <p v-else-if="hint" class="text-xs text-slate-500">
      {{ hint }}
    </p>
  </div>
</template>
