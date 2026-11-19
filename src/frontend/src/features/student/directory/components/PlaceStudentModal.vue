<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle, School } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  fetchAcademicYearsLookup,
  fetchStudyGroupsLookup,
  type AcademicYearLookupItem,
  type StudyGroupLookupItem
} from '@/shared/services/lookups.api';
import { ApiError } from '@/shared/types/common';
import type { StudentPlacementDto } from '../api/students.api';
import { usePlaceStudentMutation } from '../api/useStudentMutations';
import { placementSchema, type PlacementFormValues } from '../schemas/placement.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studentId: string;
    studentName?: string | null;
    currentGrade?: string | null;
    currentSchoolLevel?: string | null;
  }>(),
  {
    studentName: null,
    currentGrade: null,
    currentSchoolLevel: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  placed: [placement: StudentPlacementDto];
}>();

const { activeAcademicYearId } = useAppContextBridge();
const placeMutation = usePlaceStudentMutation();

const conflictBanner = ref<string | null>(null);
const academicYears = ref<AcademicYearLookupItem[]>([]);
const availableStudyGroups = ref<StudyGroupLookupItem[]>([]);
const isLoadingLookups = ref(false);
const isLoadingGroups = ref(false);

const schoolLevelOptions = [
  { label: 'SD (Elementary)', value: 'SD' },
  { label: 'SMP (Junior High)', value: 'SMP' },
  { label: 'SMA (Senior High)', value: 'SMA' },
  { label: 'SMK (Vocational)', value: 'SMK' }
];

function mapValidationErrors(fieldErrors?: Record<string, string[]> | null): Record<string, string> {
  if (!fieldErrors) return {};
  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
    const first = messages?.[0];
    if (first) {
      mapped[camelKey] = first;
      mapped[key] = first;
    }
  }
  return mapped;
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<PlacementFormValues>({
  validationSchema: toTypedSchema(placementSchema),
  initialValues: {
    academicYearId: activeAcademicYearId.value ?? '',
    schoolLevel: 'SMP',
    grade: '7',
    studyGroupId: ''
  }
});

const [academicYearId] = defineField('academicYearId');
const [schoolLevel] = defineField('schoolLevel');
const [grade] = defineField('grade');
const [studyGroupId] = defineField('studyGroupId');

async function loadLookups(): Promise<void> {
  isLoadingLookups.value = true;
  try {
    const years = await fetchAcademicYearsLookup();
    academicYears.value = years;
  } catch {
    academicYears.value = [];
  } finally {
    isLoadingLookups.value = false;
  }
}

async function loadStudyGroups(): Promise<void> {
  if (!academicYearId.value) {
    availableStudyGroups.value = [];
    return;
  }

  isLoadingGroups.value = true;
  try {
    const groups = await fetchStudyGroupsLookup({
      academicYearId: academicYearId.value,
      schoolLevel: schoolLevel.value || undefined,
      grade: grade.value || undefined
    });
    availableStudyGroups.value = groups;
  } catch {
    availableStudyGroups.value = [];
  } finally {
    isLoadingGroups.value = false;
  }
}

watch(
  () => props.visible,
  async (isVisible) => {
    if (isVisible) {
      conflictBanner.value = null;
      await loadLookups();
      resetForm({
        values: {
          academicYearId: activeAcademicYearId.value ?? (academicYears.value[0]?.id ?? ''),
          schoolLevel: (props.currentSchoolLevel as 'SD' | 'SMP' | 'SMA' | 'SMK') || 'SMP',
          grade: props.currentGrade || '7',
          studyGroupId: ''
        }
      });
      await loadStudyGroups();
    }
  }
);

watch([academicYearId, schoolLevel, grade], () => {
  if (props.visible) {
    loadStudyGroups();
  }
});

function closeModal(): void {
  emit('update:visible', false);
  conflictBanner.value = null;
}

const studyGroupOptions = computed(() =>
  availableStudyGroups.value.map((g) => ({
    label: `${g.name} (${g.schoolLevel} Gr.${g.grade}${g.homeroomTeacherName ? ' — ' + g.homeroomTeacherName : ''})`,
    value: g.id
  }))
);

const academicYearOptions = computed(() =>
  academicYears.value.map((y) => ({
    label: `${y.name}${y.status === 'ACTIVE' ? ' (Active)' : ''}`,
    value: y.id
  }))
);

const onSubmit = handleSubmit(async (values) => {
  if (!props.studentId) return;
  conflictBanner.value = null;

  try {
    const result = await placeMutation.mutateAsync({
      studentId: props.studentId,
      payload: {
        academicYearId: values.academicYearId,
        schoolLevel: values.schoolLevel,
        grade: values.grade.trim(),
        studyGroupId: values.studyGroupId || null
      }
    });

    emit('placed', result);
    closeModal();
  } catch (error) {
    if (error instanceof ApiError && error.status === 422 && error.errors) {
      setErrors(mapValidationErrors(error.errors));
    } else if (error instanceof ApiError && error.status === 409) {
      conflictBanner.value =
        error.detail ||
        'Conflict: The selected study group does not match the academic year, school level, or grade (BR-STU-008).';
    }
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    header="Place Student in Study Group"
    class="!w-full sm:!w-[500px]"
    @update:visible="closeModal"
  >
    <form novalidate @submit.prevent="onSubmit">
      <div class="space-y-4">
        <!-- Target Student Banner -->
        <div class="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs">
          <School class="h-5 w-5 text-brand-accent-600" />
          <div>
            <div class="font-medium text-slate-800">Assigning Placement For:</div>
            <div class="font-semibold text-slate-900">{{ studentName ?? 'Selected Student' }}</div>
          </div>
        </div>

        <!-- Conflict Alert Banner -->
        <div
          v-if="conflictBanner"
          role="alert"
          class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900"
        >
          <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
          <div class="flex-1">{{ conflictBanner }}</div>
        </div>

        <!-- Academic Year -->
        <FormField
          label="Academic Year"
          required
          field-id="pAcademicYear"
          :error="errors.academicYearId"
        >
          <Select
            id="pAcademicYear"
            v-model="academicYearId"
            :options="academicYearOptions"
            option-label="label"
            option-value="value"
            placeholder="Select academic year"
            class="w-full"
            :loading="isLoadingLookups"
            :invalid="Boolean(errors.academicYearId)"
          />
        </FormField>

        <!-- School Level & Grade -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="School Level" required field-id="pSchoolLevel" :error="errors.schoolLevel">
            <Select
              id="pSchoolLevel"
              v-model="schoolLevel"
              :options="schoolLevelOptions"
              option-label="label"
              option-value="value"
              placeholder="Select level"
              class="w-full"
              :invalid="Boolean(errors.schoolLevel)"
            />
          </FormField>

          <FormField label="Grade" required field-id="pGrade" :error="errors.grade">
            <InputText
              id="pGrade"
              v-model="grade"
              placeholder="e.g. 7"
              class="w-full"
              :invalid="Boolean(errors.grade)"
            />
          </FormField>
        </div>

        <!-- Study Group Picker -->
        <FormField
          label="Study Group (Rombel)"
          required
          field-id="pStudyGroup"
          hint="Target class cohort for the selected academic year (BR-STU-004)"
          :error="errors.studyGroupId"
        >
          <Select
            id="pStudyGroup"
            v-model="studyGroupId"
            :options="studyGroupOptions"
            option-label="label"
            option-value="value"
            placeholder="Select study group class"
            class="w-full"
            :loading="isLoadingGroups"
            :invalid="Boolean(errors.studyGroupId)"
          />
        </FormField>
      </div>

      <div class="mt-6 flex justify-end gap-2 border-t border-slate-200 pt-4">
        <Button label="Cancel" severity="secondary" text @click="closeModal" />
        <Button
          type="submit"
          label="Confirm Placement"
          icon="pi pi-check"
          :loading="placeMutation.isPending.value"
        />
      </div>
    </form>
  </Dialog>
</template>
