<script setup lang="ts">
import { computed } from 'vue';
import { ArrowLeft } from 'lucide-vue-next';

const props = withDefaults(
  defineProps<{
    title?: string;
    subtitle?: string;
    hasSelection?: boolean;
    backLabel?: string;
  }>(),
  {
    title: undefined,
    subtitle: undefined,
    hasSelection: undefined,
    backLabel: 'Back to list'
  }
);

const emit = defineEmits<{
  back: [];
}>();

const showWorklistOnSmall = computed(() => props.hasSelection !== true);
const showWorkbenchOnSmall = computed(() => props.hasSelection !== false);
</script>

<template>
  <div
    class="flex flex-col gap-4 lg:h-[calc(100vh-8.5rem)] lg:min-h-96"
    data-testid="workbench-layout"
  >
    <!-- Optional Page / Queue Header -->
    <slot name="header">
      <div
        v-if="title || subtitle || $slots.actions"
        class="flex shrink-0 flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
      >
        <div class="space-y-1">
          <h1 v-if="title" class="text-xl font-semibold tracking-tight text-slate-900">
            {{ title }}
          </h1>
          <p v-if="subtitle" class="text-sm text-slate-600">
            {{ subtitle }}
          </p>
        </div>
        <div v-if="$slots.actions" class="flex shrink-0 flex-wrap items-center gap-2">
          <slot name="actions" />
        </div>
      </div>
    </slot>

    <!-- Two-Pane Work Queue Canvas -->
    <div class="flex min-h-0 flex-1 flex-col gap-4 lg:flex-row">
      <!-- Left Scrollable Filterable Worklist Pane (35%-40% on desktop) -->
      <aside
        aria-label="Work queue list"
        data-testid="workbench-worklist-pane"
        class="min-h-0 flex-col overflow-hidden rounded-xl border border-slate-200 bg-white lg:w-[38%] lg:min-w-72 lg:max-w-md lg:shrink-0"
        :class="[
          showWorklistOnSmall ? 'flex' : 'hidden lg:flex',
          hasSelection === undefined ? 'max-h-112 lg:max-h-none' : ''
        ]"
      >
        <div
          v-if="$slots['worklist-header']"
          class="shrink-0 border-b border-slate-200 bg-slate-50/70 p-3.5"
        >
          <slot name="worklist-header" />
        </div>

        <div class="min-h-0 flex-1 overflow-y-auto p-3" data-testid="workbench-worklist-scroll">
          <slot name="worklist" />
        </div>

        <div
          v-if="$slots['worklist-footer']"
          class="shrink-0 border-t border-slate-200 bg-slate-50 px-3.5 py-2.5"
        >
          <slot name="worklist-footer" />
        </div>
      </aside>

      <!-- Right Detail Workbench Canvas (60%-65% on desktop) -->
      <section
        aria-label="Detail workbench"
        data-testid="workbench-detail-pane"
        class="min-h-0 min-w-0 flex-1 flex-col overflow-hidden rounded-xl border border-slate-200 bg-white"
        :class="showWorkbenchOnSmall ? 'flex' : 'hidden lg:flex'"
      >
        <div
          v-if="hasSelection === true || $slots['workbench-header']"
          class="flex shrink-0 items-center justify-between gap-3 border-b border-slate-200 bg-slate-50/70 px-4 py-3"
          :class="hasSelection === true && !$slots['workbench-header'] ? 'lg:hidden' : ''"
        >
          <button
            v-if="hasSelection === true"
            type="button"
            data-testid="workbench-back-button"
            class="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-brand-accent-600 lg:hidden"
            @click="emit('back')"
          >
            <ArrowLeft class="h-3.5 w-3.5" />
            <span>{{ backLabel }}</span>
          </button>

          <div class="min-w-0 flex-1">
            <slot name="workbench-header" />
          </div>
        </div>

        <div
          class="min-h-0 flex-1 overflow-y-auto p-4 lg:p-6"
          data-testid="workbench-detail-scroll"
        >
          <slot name="workbench">
            <slot />
          </slot>
        </div>
      </section>
    </div>
  </div>
</template>
