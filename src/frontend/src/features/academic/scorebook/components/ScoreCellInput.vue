<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { MessageSquare, Check, X } from 'lucide-vue-next';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Button from 'primevue/button';

const props = withDefaults(
  defineProps<{
    studentId: string;
    studentName?: string;
    assignmentId: string;
    assignmentTitle?: string;
    maxScore: number;
    modelValue: number | null;
    feedback?: string | null;
    isDirty?: boolean;
    threshold?: number;
    rowIndex: number;
    colIndex: number;
    disabled?: boolean;
  }>(),
  {
    studentName: 'Student',
    assignmentTitle: 'Assignment',
    feedback: null,
    isDirty: false,
    threshold: 75,
    disabled: false
  }
);

const emit = defineEmits<{
  'update:modelValue': [value: number | null];
  'update:feedback': [feedback: string | null];
  navigate: [direction: 'up' | 'down' | 'left' | 'right', rowIndex: number, colIndex: number];
}>();

const inputRef = ref<HTMLInputElement | null>(null);
const isFeedbackModalOpen = ref(false);
const draftFeedback = ref(props.feedback ?? '');

watch(
  () => props.feedback,
  (val) => {
    draftFeedback.value = val ?? '';
  }
);

const isBelowThreshold = computed(() => {
  if (props.modelValue === null || props.modelValue === undefined || isNaN(props.modelValue)) {
    return false;
  }
  return props.modelValue < props.threshold;
});

const cellHighlightClasses = computed(() => {
  if (isBelowThreshold.value) {
    return 'bg-rose-50 border-rose-300 text-rose-800 font-semibold focus-within:ring-rose-400';
  }
  if (props.isDirty) {
    return 'bg-amber-50/60 border-amber-300 text-slate-900 focus-within:ring-amber-400';
  }
  return 'bg-white border-slate-200 text-slate-800 focus-within:ring-brand-accent-500';
});

function onInput(event: Event): void {
  const target = event.target as HTMLInputElement;
  const raw = target.value.trim();
  if (raw === '') {
    emit('update:modelValue', null);
    return;
  }
  const parsed = parseFloat(raw);
  if (!isNaN(parsed)) {
    // Clamp between 0 and maxScore
    const clamped = Math.max(0, Math.min(props.maxScore, parsed));
    emit('update:modelValue', clamped);
  }
}

function onBlur(): void {
  if (props.modelValue !== null && props.modelValue !== undefined && inputRef.value) {
    // normalize display value
    inputRef.value.value = String(props.modelValue);
  }
}

function onKeydown(e: KeyboardEvent): void {
  if (e.key === 'Enter') {
    e.preventDefault();
    emit('navigate', 'down', props.rowIndex, props.colIndex);
  } else if (e.key === 'ArrowDown') {
    e.preventDefault();
    emit('navigate', 'down', props.rowIndex, props.colIndex);
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    emit('navigate', 'up', props.rowIndex, props.colIndex);
  } else if (e.key === 'ArrowRight') {
    const el = inputRef.value;
    if (el && (el.selectionStart === el.value.length || el.selectionStart === 0 && el.selectionEnd === el.value.length)) {
      e.preventDefault();
      emit('navigate', 'right', props.rowIndex, props.colIndex);
    }
  } else if (e.key === 'ArrowLeft') {
    const el = inputRef.value;
    if (el && (el.selectionStart === 0 || el.selectionStart === 0 && el.selectionEnd === el.value.length)) {
      e.preventDefault();
      emit('navigate', 'left', props.rowIndex, props.colIndex);
    }
  }
}

function openFeedbackModal(): void {
  draftFeedback.value = props.feedback ?? '';
  isFeedbackModalOpen.value = true;
}

function saveFeedback(): void {
  const val = draftFeedback.value.trim();
  emit('update:feedback', val.length > 0 ? val : null);
  isFeedbackModalOpen.value = false;
}

function clearFeedback(): void {
  draftFeedback.value = '';
  emit('update:feedback', null);
  isFeedbackModalOpen.value = false;
}

defineExpose({
  focus: () => {
    inputRef.value?.focus();
    inputRef.value?.select();
  }
});
</script>

<template>
  <div
    class="group relative flex h-full min-h-10 items-center justify-between rounded border px-2 py-1 transition-colors"
    :class="cellHighlightClasses"
    :data-testid="`score-cell-${studentId}-${assignmentId}`"
  >
    <!-- Dirty Indicator Dot -->
    <span
      v-if="isDirty"
      class="absolute top-1 left-1 h-1.5 w-1.5 rounded-full bg-amber-500"
      title="Unsaved changes"
      data-testid="dirty-indicator"
    />

    <!-- Score Input -->
    <input
      ref="inputRef"
      type="number"
      step="0.01"
      min="0"
      :max="maxScore"
      :value="modelValue ?? ''"
      :disabled="disabled"
      class="w-full bg-transparent text-center text-xs outline-none font-mono focus:ring-0 disabled:opacity-50"
      :data-row-index="rowIndex"
      :data-col-index="colIndex"
      data-testid="score-cell-input"
      placeholder="-"
      @input="onInput"
      @blur="onBlur"
      @keydown="onKeydown"
      @focus="($event.target as HTMLInputElement).select()"
    />

    <!-- Feedback Button / Tooltip Trigger -->
    <button
      type="button"
      class="ml-1 shrink-0 rounded p-0.5 transition-colors hover:bg-slate-200/60 focus:outline-none"
      :class="[
        feedback ? 'text-brand-accent-600' : 'text-slate-300 group-hover:text-slate-400',
        disabled ? 'cursor-not-allowed opacity-40' : ''
      ]"
      :title="feedback ? `Feedback: ${feedback}` : 'Add qualitative feedback'"
      :disabled="disabled"
      data-testid="feedback-trigger-btn"
      @click.stop="openFeedbackModal"
    >
      <MessageSquare class="h-3 w-3" :class="{ 'fill-brand-accent-100': Boolean(feedback) }" />
    </button>

    <!-- Feedback Modal Dialog -->
    <Dialog
      v-model:visible="isFeedbackModalOpen"
      modal
      header="Qualitative Feedback"
      :style="{ width: '420px' }"
      :closable="true"
      data-testid="feedback-modal"
    >
      <div class="space-y-3 py-1">
        <div class="text-xs text-slate-500">
          <p class="font-medium text-slate-800">{{ studentName }}</p>
          <p>{{ assignmentTitle }} (Max: {{ maxScore }})</p>
        </div>

        <Textarea
          v-model="draftFeedback"
          rows="4"
          maxlength="1000"
          placeholder="Enter notes or feedback for this student..."
          class="w-full text-xs"
          data-testid="feedback-textarea"
        />
        <div class="flex justify-between text-[11px] text-slate-400">
          <span>Max 1,000 characters</span>
          <span>{{ draftFeedback.length }} / 1000</span>
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-between gap-2">
          <Button
            v-if="feedback"
            label="Clear"
            severity="danger"
            text
            size="small"
            data-testid="feedback-clear-btn"
            @click="clearFeedback"
          >
            <template #icon>
              <X class="h-3.5 w-3.5 mr-1" />
            </template>
          </Button>
          <span v-else />

          <div class="flex items-center gap-2">
            <Button
              label="Cancel"
              severity="secondary"
              text
              size="small"
              data-testid="feedback-cancel-btn"
              @click="isFeedbackModalOpen = false"
            />
            <Button
              label="Save Note"
              severity="primary"
              size="small"
              data-testid="feedback-save-btn"
              @click="saveFeedback"
            >
              <template #icon>
                <Check class="h-3.5 w-3.5 mr-1" />
              </template>
            </Button>
          </div>
        </div>
      </template>
    </Dialog>
  </div>
</template>
