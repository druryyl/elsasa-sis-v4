<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useFieldArray, useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import { AlertCircle, Plus, Trash2 } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { ApiError } from '@/shared/types/common';
import type { AcademicYearDto } from '../api/academicYears.api';
import {
  useCreateAcademicYearMutation,
  useUpdateAcademicYearMutation
} from '../api/useAcademicYearMutations';
import {
  academicYearSchema,
  type AcademicYearFormValues,
  type OperationalPeriodFormValues
} from '../schemas/academicYear.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    academicYear?: AcademicYearDto | null;
  }>(),
  {
    academicYear: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [academicYear: AcademicYearDto];
}>();

const isEditMode = computed(() => Boolean(props.academicYear?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Academic Year' : 'Create Academic Year'
);

const createMutation = useCreateAcademicYearMutation();
const updateMutation = useUpdateAcademicYearMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function createEmptyPeriod(name = ''): OperationalPeriodFormValues {
  return {
    id: null,
    name,
    startDate: '',
    endDate: ''
  };
}

function buildInitialValues(year?: AcademicYearDto | null): AcademicYearFormValues {
  if (!year) {
    return {
      name: '',
      startDate: '',
      endDate: '',
      periods: [createEmptyPeriod('Semester 1')]
    };
  }

  const periods =
    year.periods && year.periods.length > 0
      ? year.periods.map((period) => ({
          id: period.id ?? null,
          name: period.name ?? '',
          startDate: period.startDate ?? '',
          endDate: period.endDate ?? ''
        }))
      : [createEmptyPeriod('Semester 1')];

  return {
    name: year.name ?? '',
    startDate: year.startDate ?? '',
    endDate: year.endDate ?? '',
    periods
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<AcademicYearFormValues>({
    validationSchema: toTypedSchema(academicYearSchema),
    initialValues: buildInitialValues(props.academicYear)
  });

const [name, nameAttrs] = defineField('name');
const [startDate, startDateAttrs] = defineField('startDate');
const [endDate, endDateAttrs] = defineField('endDate');

const { fields: periodFields, push: pushPeriod, remove: removePeriod } =
  useFieldArray<OperationalPeriodFormValues>('periods');

watch(
  () => [props.visible, props.academicYear] as const,
  ([isOpen, year]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(year)
      });
    }
  }
);

function isPeriodActive(periodId?: string | null): boolean {
  if (!periodId || !props.academicYear?.periods) {
    return false;
  }
  return props.academicYear.periods.some(
    (period) => period.id === periodId && period.status === 'ACTIVE'
  );
}

function getPeriodError(index: number, field: keyof OperationalPeriodFormValues): string | undefined {
  const key = `periods[${index}].${String(field)}` as keyof typeof errors.value;
  return errors.value[key];
}

function addPeriod(): void {
  const nextIndex = periodFields.value.length + 1;
  pushPeriod(createEmptyPeriod(`Semester ${nextIndex}`));
}

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
    let saved: AcademicYearDto;
    if (isEditMode.value && props.academicYear?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.academicYear.id,
        payload: {
          name: values.name.trim(),
          startDate: values.startDate,
          endDate: values.endDate,
          periods: values.periods.map((period) => ({
            id: period.id ?? null,
            name: period.name.trim(),
            startDate: period.startDate,
            endDate: period.endDate
          }))
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        name: values.name.trim(),
        startDate: values.startDate,
        endDate: values.endDate,
        periods: values.periods.map((period) => ({
          name: period.name.trim(),
          startDate: period.startDate,
          endDate: period.endDate
        }))
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

    conflictBanner.value = 'Unable to save academic year. Please try again.';
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
    class="w-full sm:w-[600px]"
    data-testid="academic-year-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="academic-year-form" class="space-y-6" novalidate @submit.prevent="onSubmit">
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
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-900">Academic Year Details</h3>
          <StatusPill
            v-if="isEditMode && academicYear?.status"
            :status="academicYear.status"
          />
        </div>

        <FormField
          label="Academic Year Name"
          field-id="ay-name"
          required
          :error="errors.name"
          hint="Example: 2026/2027"
        >
          <InputText
            id="ay-name"
            v-model="name"
            v-bind="nameAttrs"
            placeholder="2026/2027"
            class="w-full"
            :invalid="Boolean(errors.name)"
            :disabled="isSubmitting"
          />
        </FormField>

        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="Start Date" field-id="ay-start-date" required :error="errors.startDate">
            <InputText
              id="ay-start-date"
              v-model="startDate"
              v-bind="startDateAttrs"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.startDate)"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField label="End Date" field-id="ay-end-date" required :error="errors.endDate">
            <InputText
              id="ay-end-date"
              v-model="endDate"
              v-bind="endDateAttrs"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.endDate)"
              :disabled="isSubmitting"
            />
          </FormField>
        </div>
      </div>

      <div class="space-y-4 border-t border-slate-200 pt-5">
        <div class="flex items-center justify-between gap-3">
          <div>
            <h3 class="text-sm font-semibold text-slate-900">Operational Periods</h3>
            <p class="text-xs text-slate-500">
              Define non-overlapping semesters or terms within the academic year bounds.
            </p>
          </div>
          <Button
            type="button"
            label="Add Period"
            size="small"
            severity="secondary"
            outlined
            :disabled="isSubmitting"
            data-testid="add-period-button"
            @click="addPeriod"
          >
            <template #icon>
              <Plus class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>

        <p v-if="errors.periods" role="alert" class="text-xs font-medium text-rose-600">
          {{ errors.periods }}
        </p>

        <div class="space-y-3">
          <div
            v-for="(field, index) in periodFields"
            :key="field.key"
            class="space-y-3 rounded-lg border border-slate-200 bg-slate-50/70 p-4"
            :data-testid="`period-row-${index}`"
          >
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Period {{ index + 1 }}
                </span>
                <StatusPill v-if="isPeriodActive(field.value.id)" status="ACTIVE" />
              </div>
              <button
                type="button"
                class="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-rose-600 transition-colors hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="
                  isSubmitting || periodFields.length <= 1 || isPeriodActive(field.value.id)
                "
                :title="
                  isPeriodActive(field.value.id)
                    ? 'Active operational period cannot be removed'
                    : periodFields.length <= 1
                      ? 'At least one operational period is required'
                      : 'Remove period'
                "
                :data-testid="`remove-period-${index}`"
                @click="removePeriod(index)"
              >
                <Trash2 class="h-3.5 w-3.5" />
                <span>Remove</span>
              </button>
            </div>

            <FormField
              label="Period Name"
              :field-id="`period-name-${index}`"
              required
              :error="getPeriodError(index, 'name')"
            >
              <InputText
                :id="`period-name-${index}`"
                v-model="field.value.name"
                placeholder="Semester 1"
                class="w-full"
                :invalid="Boolean(getPeriodError(index, 'name'))"
                :disabled="isSubmitting"
              />
            </FormField>

            <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <FormField
                label="Start Date"
                :field-id="`period-start-${index}`"
                required
                :error="getPeriodError(index, 'startDate')"
              >
                <InputText
                  :id="`period-start-${index}`"
                  v-model="field.value.startDate"
                  type="date"
                  class="w-full"
                  :invalid="Boolean(getPeriodError(index, 'startDate'))"
                  :disabled="isSubmitting"
                />
              </FormField>

              <FormField
                label="End Date"
                :field-id="`period-end-${index}`"
                required
                :error="getPeriodError(index, 'endDate')"
              >
                <InputText
                  :id="`period-end-${index}`"
                  v-model="field.value.endDate"
                  type="date"
                  class="w-full"
                  :invalid="Boolean(getPeriodError(index, 'endDate'))"
                  :disabled="isSubmitting"
                />
              </FormField>
            </div>
          </div>
        </div>
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
          form="academic-year-form"
          :label="isEditMode ? 'Save Changes' : 'Create Academic Year'"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="submit-academic-year-button"
        />
      </div>
    </template>
  </Drawer>
</template>
