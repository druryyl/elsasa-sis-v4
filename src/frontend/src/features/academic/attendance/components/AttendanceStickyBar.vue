<script setup lang="ts">
import Button from 'primevue/button';
import { AlertCircle, Check, X } from 'lucide-vue-next';

defineProps<{
  visible: boolean;
  modifiedCount: number;
  saving?: boolean;
}>();

const emit = defineEmits<{
  save: [];
  discard: [];
}>();
</script>

<template>
  <Transition
    enter-active-class="transition duration-200 ease-out"
    enter-from-class="transform translate-y-full opacity-0"
    enter-to-class="transform translate-y-0 opacity-100"
    leave-active-class="transition duration-150 ease-in"
    leave-from-class="transform translate-y-0 opacity-100"
    leave-to-class="transform translate-y-full opacity-0"
  >
    <div
      v-if="visible"
      class="fixed bottom-0 left-0 right-0 z-40 border-t border-slate-200 bg-white/95 backdrop-blur-md shadow-2xl py-3 px-4 sm:px-6"
      data-testid="attendance-sticky-bar"
    >
      <div class="mx-auto flex max-w-7xl items-center justify-between gap-4">
        <!-- Left: Unsaved Indicator & Count -->
        <div class="flex items-center gap-2.5">
          <span class="flex h-2.5 w-2.5 rounded-full bg-amber-500 animate-pulse" />
          <div class="flex items-center gap-1.5 text-xs sm:text-sm font-medium text-slate-700">
            <AlertCircle class="h-4 w-4 text-amber-500 shrink-0" />
            <span data-testid="dirty-count-label">
              {{ modifiedCount }} unsaved student attendance {{ modifiedCount === 1 ? 'change' : 'changes' }}
            </span>
          </div>
        </div>

        <!-- Right: Action Buttons -->
        <div class="flex items-center gap-2.5">
          <Button
            type="button"
            severity="secondary"
            variant="text"
            size="small"
            :disabled="saving"
            class="text-xs sm:text-sm font-medium text-slate-600 hover:text-slate-900"
            data-testid="discard-btn"
            @click="emit('discard')"
          >
            <X class="h-4 w-4 mr-1" />
            <span>Cancel</span>
          </Button>

          <Button
            type="button"
            severity="primary"
            size="small"
            :loading="saving"
            class="!bg-emerald-600 hover:!bg-emerald-700 !border-emerald-600 !text-white text-xs sm:text-sm font-semibold shadow-xs"
            data-testid="save-changes-btn"
            @click="emit('save')"
          >
            <Check v-if="!saving" class="h-4 w-4 mr-1" />
            <span>Save Changes</span>
          </Button>
        </div>
      </div>
    </div>
  </Transition>
</template>
