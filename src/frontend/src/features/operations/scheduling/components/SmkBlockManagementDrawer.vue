<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle, AlertTriangle, CalendarRange, Plus, Trash2 } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { ApiError } from '@/shared/types/common';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type { StudyGroupTimetableBlockDto, CreateSmkBlockRequest } from '../api/timetables.api';
import { useSmkBlocksQuery } from '../api/useTimetableQueries';
import { useCreateSmkBlockMutation, useDeleteSmkBlockMutation } from '../api/useTimetableMutations';
import {
  smkBlockSchema,
  type SmkBlockFormValues,
  type SmkBlockScheduleType
} from '../schemas/timetable.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studyGroupId: string;
    operationalPeriodId: string;
    studyGroupName?: string;
    readOnly?: boolean;
  }>(),
  {
    studyGroupName: '',
    readOnly: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
}>();

const blockQueryParams = computed(() => ({
  periodId: props.operationalPeriodId
}));

const blocksQuery = useSmkBlocksQuery(
  () => props.studyGroupId,
  blockQueryParams,
  { enabled: () => props.visible && Boolean(props.studyGroupId) && Boolean(props.operationalPeriodId) }
);

const blocks = computed(() => blocksQuery.data.value ?? []);
const isLoading = computed(() => blocksQuery.isLoading.value);

const createMutation = useCreateSmkBlockMutation();
const deleteMutation = useDeleteSmkBlockMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || deleteMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

const blockToDelete = ref<StudyGroupTimetableBlockDto | null>(null);
const isDeleteDialogOpen = ref(false);

const scheduleTypeOptions: Array<{ label: string; value: SmkBlockScheduleType }> = [
  { label: 'General Subjects (Akademik / Normatif-Adaptif)', value: 'GENERAL' },
  { label: 'Vocational Practice (Produktif / Kejuruan)', value: 'VOCATIONAL' }
];

function buildInitialValues(): SmkBlockFormValues {
  return {
    operationalPeriodId: props.operationalPeriodId,
    scheduleType: 'GENERAL',
    startDate: '',
    endDate: ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<SmkBlockFormValues>({
  validationSchema: toTypedSchema(smkBlockSchema),
  initialValues: buildInitialValues()
});

const [scheduleType, scheduleTypeAttrs] = defineField('scheduleType');
const [startDate, startDateAttrs] = defineField('startDate');
const [endDate, endDateAttrs] = defineField('endDate');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues()
      });
    }
  }
);

function closeDrawer(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onAddBlock = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    const payload: CreateSmkBlockRequest = {
      operationalPeriodId: values.operationalPeriodId,
      scheduleType: values.scheduleType,
      startDate: values.startDate,
      endDate: values.endDate
    };

    await createMutation.mutateAsync({
      studyGroupId: props.studyGroupId,
      payload
    });

    resetForm({
      values: buildInitialValues()
    });
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 422 && error.errors) {
        const mappedErrors: Record<string, string> = {};
        for (const [key, messages] of Object.entries(error.errors)) {
          const first = messages[0];
          if (first) {
            mappedErrors[key] = first;
          }
        }
        setErrors(mappedErrors);
        return;
      }
      if (error.status === 409) {
        conflictBanner.value =
          error.detail || 'Overlapping SMK block rotation date range exists.';
        return;
      }
    }
    conflictBanner.value =
      error instanceof Error ? error.message : 'Failed to create SMK block rotation.';
  }
});

function confirmDeleteBlock(block: StudyGroupTimetableBlockDto): void {
  blockToDelete.value = block;
  isDeleteDialogOpen.value = true;
}

async function handleDeleteConfirm(): Promise<void> {
  if (!blockToDelete.value?.id) return;

  try {
    await deleteMutation.mutateAsync({
      studyGroupId: props.studyGroupId,
      blockId: blockToDelete.value.id
    });
    isDeleteDialogOpen.value = false;
    blockToDelete.value = null;
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value = error.detail || 'Failed to delete block.';
    } else {
      conflictBanner.value = 'Failed to delete block.';
    }
  }
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="w-full sm:w-[520px]"
    :modal="true"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="space-y-1">
        <h2 class="text-base font-semibold text-slate-900" data-testid="smk-blocks-drawer-title">
          SMK Block Date Rotations
        </h2>
        <p class="text-xs text-slate-500">
          Manage alternating General &amp; Vocational block date ranges for
          <span class="font-medium text-slate-700">{{ studyGroupName || 'Study Group' }}</span>
        </p>
      </div>
    </template>

    <div class="space-y-6 p-1">
      <!-- Read-Only Banner when locked -->
      <div
        v-if="readOnly"
        class="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800"
      >
        <AlertTriangle class="h-4 w-4 shrink-0 text-amber-600" />
        <span>Block schedule rotation editing is disabled in Read-Only mode.</span>
      </div>

      <!-- Domain Conflict Banner (409) -->
      <div
        v-if="conflictBanner"
        class="flex items-start gap-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-800"
        data-testid="block-conflict-banner"
      >
        <AlertCircle class="h-4 w-4 shrink-0 text-rose-600" />
        <div>
          <p class="font-medium">Block Schedule Notice</p>
          <p>{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Add New Block Card (Enabled for managers) -->
      <div
        v-if="!readOnly"
        class="rounded-xl border border-slate-200 bg-slate-50/60 p-4 space-y-3"
        data-testid="add-block-section"
      >
        <div class="flex items-center gap-1.5 text-xs font-semibold text-slate-700 uppercase tracking-wide">
          <CalendarRange class="h-4 w-4 text-brand-primary-600" />
          <span>Add Block Date Range</span>
        </div>

        <form id="smk-block-form" class="space-y-3" @submit.prevent="onAddBlock">
          <FormField label="Schedule Cycle Type" :error="errors.scheduleType" required>
            <Select
              v-model="scheduleType"
              v-bind="scheduleTypeAttrs"
              :options="scheduleTypeOptions"
              option-label="label"
              option-value="value"
              class="w-full text-sm"
              :disabled="isSubmitting"
              data-testid="block-type-select"
            />
          </FormField>

          <div class="grid grid-cols-2 gap-3">
            <FormField label="Start Date" :error="errors.startDate" required>
              <InputText
                v-model="startDate"
                v-bind="startDateAttrs"
                type="date"
                class="w-full text-sm"
                :disabled="isSubmitting"
                data-testid="block-start-date"
              />
            </FormField>

            <FormField label="End Date" :error="errors.endDate" required>
              <InputText
                v-model="endDate"
                v-bind="endDateAttrs"
                type="date"
                class="w-full text-sm"
                :disabled="isSubmitting"
                data-testid="block-end-date"
              />
            </FormField>
          </div>

          <div class="flex justify-end pt-1">
            <Button
              type="submit"
              size="small"
              class="text-xs"
              :loading="createMutation.isPending.value"
              data-testid="add-block-btn"
            >
              <Plus class="mr-1 h-3.5 w-3.5" />
              Add Block Range
            </Button>
          </div>
        </form>
      </div>

      <!-- Active Block Ranges List -->
      <div class="space-y-3" data-testid="blocks-list-section">
        <div class="flex items-center justify-between">
          <h3 class="text-xs font-semibold text-slate-700 uppercase tracking-wide">
            Configured Date Ranges ({{ blocks.length }})
          </h3>
        </div>

        <div v-if="isLoading" class="space-y-2 py-4">
          <div class="h-12 animate-pulse rounded-lg bg-slate-100" />
          <div class="h-12 animate-pulse rounded-lg bg-slate-100" />
        </div>

        <EmptyState
          v-else-if="blocks.length === 0"
          title="No SMK Blocks Configured"
          description="Add general and vocational date ranges to define alternating weekly block cycles for this group."
          icon="calendar"
          size="sm"
        />

        <div v-else class="space-y-2 max-h-[360px] overflow-y-auto pr-1">
          <div
            v-for="block in blocks"
            :key="block.id"
            class="flex items-center justify-between rounded-lg border border-slate-200 bg-white p-3 shadow-xs hover:border-slate-300 transition-colors"
            data-testid="smk-block-item"
          >
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span
                  class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
                  :class="
                    block.scheduleType === 'VOCATIONAL'
                      ? 'bg-purple-100 text-purple-700'
                      : 'bg-sky-100 text-sky-700'
                  "
                  data-testid="block-badge"
                >
                  {{ block.scheduleType }}
                </span>
                <span class="text-xs font-medium text-slate-800">
                  {{ block.startDate ? formatDisplayDate(block.startDate) : '' }} &ndash;
                  {{ block.endDate ? formatDisplayDate(block.endDate) : '' }}
                </span>
              </div>
              <div class="text-[11px] text-slate-400">
                Created: {{ block.createdAt ? formatDisplayDate(block.createdAt) : '' }}
              </div>
            </div>

            <Button
              v-if="!readOnly"
              type="button"
              severity="danger"
              variant="text"
              size="small"
              class="p-1.5 text-slate-400 hover:text-rose-600"
              :disabled="isSubmitting"
              data-testid="delete-block-btn"
              @click="confirmDeleteBlock(block)"
            >
              <Trash2 class="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end border-t border-slate-200 pt-3">
        <Button
          type="button"
          severity="secondary"
          variant="text"
          size="small"
          class="text-xs"
          @click="closeDrawer"
        >
          Close
        </Button>
      </div>
    </template>
  </Drawer>

  <!-- Confirmation Dialog for Block Removal -->
  <ConfirmActionDialog
    v-model:visible="isDeleteDialogOpen"
    title="Remove SMK Block Range"
    :message="`Are you sure you want to remove the ${blockToDelete?.scheduleType ?? ''} block range (${blockToDelete?.startDate} to ${blockToDelete?.endDate})? This will revert those dates to regular rotation.`"
    confirm-label="Remove Block"
    severity="danger"
    :loading="deleteMutation.isPending.value"
    @confirm="handleDeleteConfirm"
    @cancel="isDeleteDialogOpen = false"
  />
</template>
