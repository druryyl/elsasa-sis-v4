<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import { AlertTriangle } from 'lucide-vue-next';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    severity?: 'primary' | 'warn' | 'danger';
    loading?: boolean;
    requireReason?: boolean;
    reasonLabel?: string;
    reasonPlaceholder?: string;
  }>(),
  {
    confirmLabel: 'Confirm',
    cancelLabel: 'Cancel',
    severity: 'danger',
    loading: false,
    requireReason: false,
    reasonLabel: 'Reason',
    reasonPlaceholder: 'Enter reason...'
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [reason?: string];
  cancel: [];
}>();

const reason = ref('');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      reason.value = '';
    }
  }
);

const isConfirmDisabled = computed(
  () => props.loading || (props.requireReason && reason.value.trim().length === 0)
);

function handleClose(): void {
  if (props.loading) return;
  emit('update:visible', false);
  emit('cancel');
}

function handleConfirm(): void {
  if (isConfirmDisabled.value) return;
  emit('confirm', props.requireReason ? reason.value.trim() : undefined);
}
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :closable="!loading"
    :draggable="false"
    class="w-full max-w-md"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
          :class="
            severity === 'danger'
              ? 'bg-rose-100 text-rose-600'
              : severity === 'warn'
                ? 'bg-amber-100 text-amber-600'
                : 'bg-brand-accent-50 text-brand-accent-600'
          "
        >
          <AlertTriangle class="h-4 w-4" />
        </div>
        <h3 class="text-base font-semibold text-slate-900">{{ title }}</h3>
      </div>
    </template>

    <div class="space-y-4">
      <p class="text-sm leading-relaxed text-slate-600">{{ message }}</p>

      <slot />

      <div v-if="requireReason" class="space-y-1.5">
        <label class="block text-sm font-medium text-slate-700">
          {{ reasonLabel }} <span class="text-rose-600">*</span>
        </label>
        <Textarea
          v-model="reason"
          rows="3"
          :placeholder="reasonPlaceholder"
          class="w-full"
          :disabled="loading"
        />
      </div>
    </div>

    <template #footer>
      <Button
        type="button"
        :label="cancelLabel"
        severity="secondary"
        text
        :disabled="loading"
        @click="handleClose"
      />
      <Button
        type="button"
        :label="confirmLabel"
        :severity="severity === 'primary' ? undefined : severity"
        :loading="loading"
        :disabled="isConfirmDisabled"
        @click="handleConfirm"
      />
    </template>
  </Dialog>
</template>
