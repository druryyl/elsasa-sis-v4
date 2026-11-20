<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import { AlertCircle, Check, Search, UserPlus } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { formatBackendDateOnly } from '@/shared/utils/dateUtils';
import type {
  ExtracurricularActivityDto,
  ExtracurricularParticipationDto,
  StudentLookupItem
} from '../api/extracurricular.api';
import { useEnrollmentStudentSearchQuery } from '../api/useExtracurricularQueries';
import { useEnrollParticipantMutation } from '../api/useExtracurricularMutations';
import {
  enrollmentSchema,
  type EnrollmentFormValues
} from '../schemas/extracurricular.schema';
import { resolveFormSubmissionError } from '../utils/formErrors';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    activity: ExtracurricularActivityDto | null;
    existingStudentIds?: string[];
  }>(),
  {
    existingStudentIds: () => []
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  enrolled: [participation: ExtracurricularParticipationDto];
}>();

const { activeAcademicYearId, activeAcademicYearName } = useAppContextBridge();
const searchText = ref('');
const selectedStudent = ref<StudentLookupItem | null>(null);
const errorBanner = ref<string | null>(null);

const studentsQuery = useEnrollmentStudentSearchQuery(
  () => ({
    search: searchText.value.trim() || undefined,
    academicYearId: activeAcademicYearId.value ?? undefined
  }),
  { enabled: () => props.visible }
);

const enrollMutation = useEnrollParticipantMutation();
const isSubmitting = computed(() => enrollMutation.isPending.value);

function getDefaultJoinedDate(): string {
  return formatBackendDateOnly(new Date());
}

const { defineField, errors, handleSubmit, resetForm, setErrors, setFieldValue } =
  useForm<EnrollmentFormValues>({
    validationSchema: toTypedSchema(enrollmentSchema),
    initialValues: {
      studentId: '',
      joinedDate: getDefaultJoinedDate()
    }
  });

const [studentId] = defineField('studentId');
const [joinedDate, joinedDateAttrs] = defineField('joinedDate');

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      searchText.value = '';
      selectedStudent.value = null;
      errorBanner.value = null;
      resetForm({
        values: {
          studentId: '',
          joinedDate: getDefaultJoinedDate()
        }
      });
    }
  }
);

const existingSet = computed(() => new Set(props.existingStudentIds));
const students = computed(() => studentsQuery.data.value ?? []);

function selectStudent(student: StudentLookupItem): void {
  if (!student.id || existingSet.value.has(student.id)) {
    return;
  }
  selectedStudent.value = student;
  setFieldValue('studentId', student.id);
}

function closeModal(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

const onSubmit = handleSubmit(async (values) => {
  if (!props.activity?.id) return;
  errorBanner.value = null;

  try {
    const participation = await enrollMutation.mutateAsync({
      activityId: props.activity.id,
      payload: {
        studentId: values.studentId,
        studentName: selectedStudent.value?.fullName ?? null,
        studentNis: selectedStudent.value?.nis ?? null,
        academicYearId: activeAcademicYearId.value ?? null,
        academicYearName: activeAcademicYearName.value ?? null,
        joinedDate: values.joinedDate
      }
    });

    emit('enrolled', participation);
    emit('update:visible', false);
  } catch (error) {
    errorBanner.value = resolveFormSubmissionError(
      error,
      setErrors,
      'Failed to enroll student into this club.'
    );
  }
});
</script>

<template>
  <Dialog
    :visible="visible"
    :modal="true"
    :draggable="false"
    :closable="!isSubmitting"
    class="w-full max-w-xl"
    data-testid="enrollment-modal"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-2.5">
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-accent-50 text-brand-accent-600"
        >
          <UserPlus class="h-4 w-4" />
        </div>
        <div>
          <h3 class="text-base font-semibold text-slate-900">Enroll Student</h3>
          <p class="text-xs text-slate-500">
            {{ activity?.name ? `Add a student to ${activity.name}` : 'Enroll student into club' }}
          </p>
        </div>
      </div>
    </template>

    <form class="space-y-4" @submit.prevent="onSubmit">
      <div
        v-if="errorBanner"
        role="alert"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="flex-1">{{ errorBanner }}</div>
      </div>

      <!-- Student Search & Selection -->
      <FormField
        label="Search & Select Student"
        field-id="enrollment-student-search"
        required
        :error="errors.studentId"
      >
        <div class="space-y-2.5">
          <div class="relative">
            <Search
              class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              id="enrollment-student-search"
              v-model="searchText"
              type="text"
              placeholder="Search student by name or NIS..."
              class="w-full pl-9"
              data-testid="enrollment-student-search"
            />
          </div>

          <div
            class="max-h-56 divide-y divide-slate-100 overflow-y-auto rounded-lg border border-slate-200 bg-white"
            data-testid="enrollment-student-list"
          >
            <div
              v-if="studentsQuery.isLoading.value"
              class="p-4 text-center text-xs text-slate-500"
            >
              Searching active students...
            </div>
            <div
              v-else-if="students.length === 0"
              class="p-4 text-center text-xs text-slate-500"
            >
              No active students found matching your search.
            </div>
            <button
              v-for="student in students"
              :key="student.id"
              type="button"
              :disabled="!student.id || existingSet.has(student.id)"
              :data-testid="`student-option-${student.id}`"
              class="flex w-full items-center justify-between gap-3 px-3.5 py-2.5 text-left transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:bg-slate-50/60 disabled:opacity-60"
              :class="
                studentId === student.id ? 'bg-brand-accent-50/70 hover:bg-brand-accent-50' : ''
              "
              @click="selectStudent(student)"
            >
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2">
                  <span class="truncate text-sm font-medium text-slate-900">
                    {{ student.fullName }}
                  </span>
                  <span class="font-mono text-xs text-slate-500">
                    {{ student.nis ?? 'No NIS' }}
                  </span>
                </div>
                <div class="text-xs text-slate-500">
                  {{
                    student.studyGroupName
                      ? `${student.studyGroupName}${student.grade ? ` • Grade ${student.grade}` : ''}`
                      : 'Active Student'
                  }}
                </div>
              </div>

              <span
                v-if="student.id && existingSet.has(student.id)"
                class="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-medium text-slate-600"
              >
                Already Enrolled
              </span>
              <span
                v-else-if="studentId === student.id"
                class="flex h-5 w-5 items-center justify-center rounded-full bg-brand-accent-600 text-white"
              >
                <Check class="h-3.5 w-3.5" />
              </span>
            </button>
          </div>
        </div>
      </FormField>

      <FormField
        label="Joined Date"
        field-id="enrollment-joined-date"
        required
        :error="errors.joinedDate"
      >
        <InputText
          id="enrollment-joined-date"
          v-model="joinedDate"
          v-bind="joinedDateAttrs"
          type="date"
          class="w-full"
          :invalid="Boolean(errors.joinedDate)"
          data-testid="enrollment-joined-date"
        />
      </FormField>

      <div class="flex items-center justify-end gap-2 pt-2">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeModal"
        />
        <Button
          type="submit"
          label="Enroll Student"
          :loading="isSubmitting"
          data-testid="submit-enrollment-button"
        />
      </div>
    </form>
  </Dialog>
</template>
