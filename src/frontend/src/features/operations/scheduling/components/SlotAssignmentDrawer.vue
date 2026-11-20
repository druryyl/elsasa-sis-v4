<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import { useQuery } from '@tanstack/vue-query';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle, AlertTriangle, Trash2 } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import { ApiError } from '@/shared/types/common';
import {
  subjectsLookupQuery,
  activeTeachersLookupQuery,
  roomsLookupQuery
} from '@/shared/services/lookups.api';
import type {
  StudyGroupTimetableSlotDto,
  ConfigureStudyGroupTimetableRequest,
  StudyGroupScheduleDto
} from '../api/timetables.api';
import { useSaveTimetableSlotMutation, useDeleteTimetableSlotMutation } from '../api/useTimetableMutations';
import {
  slotFormSchema,
  type SlotFormValues,
  type ScheduleType
} from '../schemas/timetable.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studyGroupId: string;
    operationalPeriodId: string;
    scheduleType: ScheduleType;
    dayOfWeek: number;
    periodSlot: number;
    timetableSlot?: StudyGroupTimetableSlotDto | null;
    readOnly?: boolean;
  }>(),
  {
    timetableSlot: null,
    readOnly: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [slot: StudyGroupScheduleDto];
  deleted: [scheduleId: string];
}>();

const DAY_NAMES = [
  '',
  'Monday (Senin)',
  'Tuesday (Selasa)',
  'Wednesday (Rabu)',
  'Thursday (Kamis)',
  'Friday (Jumat)',
  'Saturday (Sabtu)'
];

const DEFAULT_SLOT_TIMES: Record<number, { start: string; end: string }> = {
  1: { start: '07:30', end: '08:15' },
  2: { start: '08:15', end: '09:00' },
  3: { start: '09:00', end: '09:45' },
  4: { start: '10:00', end: '10:45' },
  5: { start: '10:45', end: '11:30' },
  6: { start: '11:30', end: '12:15' },
  7: { start: '12:45', end: '13:30' },
  8: { start: '13:30', end: '14:15' },
  9: { start: '14:15', end: '15:00' },
  10: { start: '15:00', end: '15:45' }
};

const isEditMode = computed(() => Boolean(props.timetableSlot?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Timetable Slot' : 'Assign Timetable Slot'
);

// Lookups queries
const subjectsQuery = useQuery(subjectsLookupQuery());
const subjects = computed(() => subjectsQuery.data.value ?? []);
const teachersQuery = useQuery(activeTeachersLookupQuery());
const teachers = computed(() => teachersQuery.data.value ?? []);
const roomsQuery = useQuery(roomsLookupQuery());
const rooms = computed(() => roomsQuery.data.value ?? []);

const saveMutation = useSaveTimetableSlotMutation();
const deleteMutation = useDeleteTimetableSlotMutation();

const isSubmitting = computed(
  () => saveMutation.isPending.value || deleteMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);
const isDeleteDialogOpen = ref(false);

function formatTimeForInput(timeStr?: string | null): string {
  if (!timeStr) return '';
  const parts = timeStr.split(':');
  if (parts.length >= 2) {
    return `${parts[0]}:${parts[1]}`;
  }
  return timeStr;
}

function buildInitialValues(): SlotFormValues {
  const currentSlot = props.timetableSlot;
  const defaultTimes = DEFAULT_SLOT_TIMES[props.periodSlot] ?? { start: '07:30', end: '08:15' };

  return {
    operationalPeriodId: props.operationalPeriodId,
    scheduleType: props.scheduleType,
    dayOfWeek: props.dayOfWeek,
    periodSlot: props.periodSlot,
    startTime: currentSlot?.startTime
      ? formatTimeForInput(currentSlot.startTime)
      : defaultTimes.start,
    endTime: currentSlot?.endTime
      ? formatTimeForInput(currentSlot.endTime)
      : defaultTimes.end,
    subjectId: currentSlot?.subjectId ?? '',
    teacherId: currentSlot?.teacherId ?? '',
    roomId: currentSlot?.roomId ?? null
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<SlotFormValues>({
  validationSchema: toTypedSchema(slotFormSchema),
  initialValues: buildInitialValues()
});

const [subjectId, subjectIdAttrs] = defineField('subjectId');
const [teacherId, teacherIdAttrs] = defineField('teacherId');
const [roomId, roomIdAttrs] = defineField('roomId');
const [startTime, startTimeAttrs] = defineField('startTime');
const [endTime, endTimeAttrs] = defineField('endTime');

watch(
  () => [props.visible, props.timetableSlot, props.dayOfWeek, props.periodSlot, props.scheduleType] as const,
  ([isOpen]) => {
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

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    const payload: ConfigureStudyGroupTimetableRequest = {
      operationalPeriodId: values.operationalPeriodId,
      scheduleType: values.scheduleType,
      dayOfWeek: values.dayOfWeek,
      periodSlot: values.periodSlot,
      startTime: values.startTime.includes(':') && values.startTime.split(':').length === 2
        ? `${values.startTime}:00`
        : values.startTime,
      endTime: values.endTime.includes(':') && values.endTime.split(':').length === 2
        ? `${values.endTime}:00`
        : values.endTime,
      subjectId: values.subjectId,
      teacherId: values.teacherId,
      roomId: values.roomId || null
    };

    const saved = await saveMutation.mutateAsync({
      studyGroupId: props.studyGroupId,
      payload
    });

    emit('saved', saved);
    closeDrawer();
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
          error.detail || 'A scheduling conflict occurred while saving this timetable slot.';
        return;
      }
    }
    conflictBanner.value =
      error instanceof Error ? error.message : 'An unexpected error occurred while saving the slot.';
  }
});

async function handleDeleteConfirm(): Promise<void> {
  if (!props.timetableSlot?.id) return;

  try {
    await deleteMutation.mutateAsync({
      studyGroupId: props.studyGroupId,
      scheduleId: props.timetableSlot.id
    });
    isDeleteDialogOpen.value = false;
    emit('deleted', props.timetableSlot.id);
    closeDrawer();
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value = error.detail || 'Failed to delete timetable slot.';
    } else {
      conflictBanner.value = 'Failed to delete timetable slot.';
    }
  }
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="w-full sm:w-[480px]"
    :modal="true"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="space-y-1">
        <h2 class="text-base font-semibold text-slate-900" data-testid="slot-drawer-title">
          {{ drawerTitle }}
        </h2>
        <p class="text-xs text-slate-500">
          {{ DAY_NAMES[dayOfWeek] }} &bull; Period Slot {{ periodSlot }} &bull; {{ scheduleType }}
        </p>
      </div>
    </template>

    <div class="space-y-5 p-1">
      <!-- Read-Only Banner when locked -->
      <div
        v-if="readOnly"
        class="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800"
      >
        <AlertTriangle class="h-4 w-4 shrink-0 text-amber-600" />
        <span>Timetable editing is disabled in Read-Only / Historical mode.</span>
      </div>

      <!-- Domain Conflict Banner (409) -->
      <div
        v-if="conflictBanner"
        class="flex items-start gap-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-800"
        data-testid="slot-conflict-banner"
      >
        <AlertCircle class="h-4 w-4 shrink-0 text-rose-600" />
        <div>
          <p class="font-medium">Scheduling Notice</p>
          <p>{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Inline Read Conflict Alert if existing slot has conflicts -->
      <div
        v-if="timetableSlot && (timetableSlot.teacherConflict || timetableSlot.roomConflict)"
        class="flex items-start gap-2 rounded-lg border border-rose-300 bg-rose-50 px-3 py-2.5 text-xs text-rose-800"
        data-testid="slot-existing-conflict"
      >
        <AlertCircle class="h-4 w-4 shrink-0 text-rose-600" />
        <div class="space-y-1">
          <p class="font-semibold text-rose-900">Current Overlap Detected</p>
          <p v-if="timetableSlot.teacherConflict">
            Teacher collision: <strong>{{ timetableSlot.teacherName }}</strong> is assigned to another study group at this time slot.
          </p>
          <p v-if="timetableSlot.roomConflict">
            Room double-booking: <strong>{{ timetableSlot.roomName }}</strong> is allocated to another study group at this time slot.
          </p>
        </div>
      </div>

      <form id="slot-form" class="space-y-4" @submit.prevent="onSubmit">
        <!-- Subject Selection -->
        <FormField label="Subject" :error="errors.subjectId" required>
          <Select
            v-model="subjectId"
            v-bind="subjectIdAttrs"
            :options="subjects"
            option-label="name"
            option-value="id"
            placeholder="Select Subject..."
            class="w-full text-sm"
            :disabled="readOnly || isSubmitting"
            data-testid="slot-subject-select"
          />
        </FormField>

        <!-- Teacher Selection -->
        <FormField label="Teacher" :error="errors.teacherId" required>
          <Select
            v-model="teacherId"
            v-bind="teacherIdAttrs"
            :options="teachers"
            option-label="fullName"
            option-value="id"
            placeholder="Select Teacher..."
            class="w-full text-sm"
            :disabled="readOnly || isSubmitting"
            data-testid="slot-teacher-select"
          />
        </FormField>

        <!-- Room Selection (Optional) -->
        <FormField label="Room (Optional)" :error="errors.roomId">
          <Select
            v-model="roomId"
            v-bind="roomIdAttrs"
            :options="rooms"
            option-label="name"
            option-value="id"
            placeholder="Select Classroom / Lab..."
            show-clear
            class="w-full text-sm"
            :disabled="readOnly || isSubmitting"
            data-testid="slot-room-select"
          />
        </FormField>

        <!-- Time Range -->
        <div class="grid grid-cols-2 gap-3">
          <FormField label="Start Time" :error="errors.startTime" required>
            <InputText
              v-model="startTime"
              v-bind="startTimeAttrs"
              type="time"
              class="w-full text-sm font-mono"
              :disabled="readOnly || isSubmitting"
              data-testid="slot-start-time"
            />
          </FormField>

          <FormField label="End Time" :error="errors.endTime" required>
            <InputText
              v-model="endTime"
              v-bind="endTimeAttrs"
              type="time"
              class="w-full text-sm font-mono"
              :disabled="readOnly || isSubmitting"
              data-testid="slot-end-time"
            />
          </FormField>
        </div>
      </form>
    </div>

    <template #footer>
      <div class="flex items-center justify-between gap-2 border-t border-slate-200 pt-3">
        <div>
          <Button
            v-if="isEditMode && !readOnly"
            type="button"
            severity="danger"
            variant="text"
            size="small"
            class="text-xs"
            :disabled="isSubmitting"
            data-testid="slot-delete-btn"
            @click="isDeleteDialogOpen = true"
          >
            <Trash2 class="mr-1 h-3.5 w-3.5" />
            Delete Slot
          </Button>
        </div>

        <div class="flex items-center gap-2">
          <Button
            type="button"
            severity="secondary"
            variant="text"
            size="small"
            class="text-xs"
            :disabled="isSubmitting"
            @click="closeDrawer"
          >
            Cancel
          </Button>
          <Button
            type="submit"
            form="slot-form"
            size="small"
            class="text-xs"
            :loading="isSubmitting"
            :disabled="readOnly"
            data-testid="slot-save-btn"
          >
            Save Slot
          </Button>
        </div>
      </div>
    </template>
  </Drawer>

  <!-- Tier 2 Confirmation Dialog for Deletion -->
  <ConfirmActionDialog
    v-model:visible="isDeleteDialogOpen"
    title="Delete Timetable Slot"
    :message="`Are you sure you want to delete this ${timetableSlot?.subjectName ?? 'scheduled'} timetable slot? This action will free up the period for other allocations.`"
    confirm-label="Confirm Delete"
    severity="danger"
    :loading="deleteMutation.isPending.value"
    @confirm="handleDeleteConfirm"
    @cancel="isDeleteDialogOpen = false"
  />
</template>
