<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { ApiError } from '@/shared/types/common';
import type { CalendarEntryDto } from '../api/calendarEntries.api';
import {
  useCreateCalendarEntryMutation,
  useUpdateCalendarEntryMutation
} from '../api/useCalendarEntryMutations';
import {
  calendarEntrySchema,
  type CalendarEntryFormValues
} from '../schemas/calendarEntry.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    academicYearId: string;
    academicYearName?: string | null;
    entry?: CalendarEntryDto | null;
  }>(),
  {
    academicYearName: null,
    entry: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [entry: CalendarEntryDto];
}>();

const isEditMode = computed(() => Boolean(props.entry?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Calendar Entry' : 'Add Calendar Entry'
);

const createMutation = useCreateCalendarEntryMutation();
const updateMutation = useUpdateCalendarEntryMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(
  entry?: CalendarEntryDto | null,
  yearId?: string
): CalendarEntryFormValues {
  if (entry) {
    return {
      academicYearId: entry.academicYearId ?? yearId ?? '',
      entryDate: entry.entryDate ?? '',
      dayType: (entry.dayType as 'SCHOOL_DAY' | 'HOLIDAY' | 'CLOSURE') || 'HOLIDAY',
      description: entry.description ?? ''
    };
  }

  return {
    academicYearId: yearId ?? '',
    entryDate: '',
    dayType: 'HOLIDAY',
    description: ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<CalendarEntryFormValues>({
    validationSchema: toTypedSchema(calendarEntrySchema),
    initialValues: buildInitialValues(props.entry, props.academicYearId)
  });

const [entryDate, entryDateAttrs] = defineField('entryDate');
const [dayType, dayTypeAttrs] = defineField('dayType');
const [description, descriptionAttrs] = defineField('description');

watch(
  () => [props.visible, props.entry, props.academicYearId] as const,
  ([isOpen, entry, yearId]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(entry, yearId)
      });
    }
  }
);

function closeDrawer(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

function mapValidationErrors(fieldErrors: Record<string, string[]>): Record<string, string> {
  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const first = messages[0];
    if (first) {
      mapped[key] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    let saved: CalendarEntryDto;
    if (isEditMode.value && props.entry?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.entry.id,
        payload: {
          entryDate: values.entryDate,
          dayType: values.dayType,
          description: values.description || null
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        academicYearId: values.academicYearId || props.academicYearId,
        entryDate: values.entryDate,
        dayType: values.dayType,
        description: values.description || null
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 422 && error.errors) {
        setErrors(mapValidationErrors(error.errors));
        if (error.detail && Object.keys(error.errors).length === 0) {
          conflictBanner.value = error.detail;
        }
        return;
      }

      if (error.status === 409 || error.status === 422 || error.status >= 400) {
        conflictBanner.value = error.detail || error.title;
        return;
      }
    }

    conflictBanner.value = 'Unable to save calendar entry. Please try again.';
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    :header="drawerTitle"
    :dismissable="!isSubmitting"
    :closable="!isSubmitting"
    class="w-full sm:w-[500px]"
    data-testid="calendar-entry-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="calendar-entry-form" class="space-y-6" novalidate @submit.prevent="onSubmit">
      <!-- Conflict / Error Banner -->
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="drawer-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <span>{{ conflictBanner }}</span>
      </div>

      <div class="space-y-4">
        <!-- Target Academic Year Info -->
        <div class="rounded-md border border-slate-200 bg-slate-50/80 p-3">
          <p class="text-xs font-medium uppercase tracking-wider text-slate-500">Academic Year</p>
          <p class="text-sm font-semibold text-slate-900">
            {{ academicYearName || 'Selected Academic Year' }}
          </p>
        </div>

        <div v-if="isEditMode && entry?.dayType" class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-900">Entry Classification</h3>
          <StatusPill :status="entry.dayType" />
        </div>

        <!-- Date Input -->
        <FormField
          label="Date"
          field-id="entry-date"
          required
          :error="errors.entryDate"
          hint="Calendar entry date (YYYY-MM-DD)"
        >
          <InputText
            id="entry-date"
            v-model="entryDate"
            v-bind="entryDateAttrs"
            type="date"
            class="w-full"
            :invalid="Boolean(errors.entryDate)"
            :disabled="isSubmitting"
            data-testid="entry-date-input"
          />
        </FormField>

        <!-- Day Type -->
        <FormField
          label="Day Classification"
          field-id="day-type"
          required
          :error="errors.dayType"
          hint="Classifies the school operation for this date"
        >
          <select
            id="day-type"
            v-model="dayType"
            v-bind="dayTypeAttrs"
            class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
            :disabled="isSubmitting"
            data-testid="day-type-select"
          >
            <option value="SCHOOL_DAY">School Day</option>
            <option value="HOLIDAY">Holiday</option>
            <option value="CLOSURE">School Closure</option>
          </select>
        </FormField>

        <!-- Description -->
        <FormField
          label="Description / Reason"
          field-id="entry-description"
          :error="errors.description"
          hint="Optional description or reason (e.g. Independence Day, Flood Closure)"
        >
          <Textarea
            id="entry-description"
            v-model="description"
            v-bind="descriptionAttrs"
            rows="3"
            placeholder="e.g. National Holiday celebration"
            class="w-full"
            :invalid="Boolean(errors.description)"
            :disabled="isSubmitting"
            data-testid="entry-description-input"
          />
        </FormField>
      </div>
    </form>

    <template #footer>
      <div class="flex w-full items-center justify-end gap-2 border-t border-slate-200 pt-4">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeDrawer"
        />
        <Button
          type="submit"
          form="calendar-entry-form"
          :label="isEditMode ? 'Save Changes' : 'Add Entry'"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="submit-calendar-entry-button"
        />
      </div>
    </template>
  </Drawer>
</template>
