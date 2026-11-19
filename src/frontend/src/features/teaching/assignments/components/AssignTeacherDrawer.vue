<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import { useQuery } from '@tanstack/vue-query';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Select from 'primevue/select';
import { AlertCircle, UserCheck } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  academicYearsLookupQuery,
  activeTeachersLookupQuery,
  studyGroupsLookupQuery,
  subjectsLookupQuery
} from '@/shared/services/lookups.api';
import { ApiError } from '@/shared/types/common';
import type { TeacherAssignmentDto } from '../api/assignments.api';
import { useAssignTeacherMutation } from '../api/useAssignmentMutations';
import {
  assignTeacherSchema,
  type AssignTeacherFormValues
} from '../schemas/assignment.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    defaultOperationalPeriodId?: string | null;
  }>(),
  {
    defaultOperationalPeriodId: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [assignment: TeacherAssignmentDto];
}>();

const { activeAcademicYearId, activePeriodId, selectedPeriodId } = useAppContextBridge();

const currentYearId = computed(() => activeAcademicYearId.value ?? undefined);

// Lookups
const { data: activeTeachers, isLoading: isTeachersLoading } = useQuery({
  ...activeTeachersLookupQuery(),
  enabled: computed(() => props.visible)
});

const { data: subjects, isLoading: isSubjectsLoading } = useQuery({
  ...subjectsLookupQuery(),
  enabled: computed(() => props.visible)
});

const { data: studyGroups, isLoading: isStudyGroupsLoading } = useQuery({
  ...studyGroupsLookupQuery({ academicYearId: currentYearId.value }),
  enabled: computed(() => props.visible)
});

const { data: academicYears } = useQuery({
  ...academicYearsLookupQuery(),
  enabled: computed(() => props.visible)
});

const operationalPeriods = computed(() => {
  const currentYear = (academicYears.value ?? []).find(
    (y) => y.id === activeAcademicYearId.value || y.status === 'ACTIVE'
  );
  if (currentYear?.periods && currentYear.periods.length > 0) {
    return currentYear.periods;
  }
  // Fallback: collect all periods
  return (academicYears.value ?? []).flatMap((y) => y.periods ?? []);
});

const assignMutation = useAssignTeacherMutation();
const conflictBanner = ref<string | null>(null);

function resolveDefaultPeriod(): string {
  return (
    props.defaultOperationalPeriodId ||
    selectedPeriodId.value ||
    activePeriodId.value ||
    operationalPeriods.value[0]?.id ||
    ''
  );
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<AssignTeacherFormValues>({
    validationSchema: toTypedSchema(assignTeacherSchema),
    initialValues: {
      teacherId: '',
      operationalPeriodId: resolveDefaultPeriod(),
      studyGroupId: '',
      subjectId: ''
    }
  });

const [teacherId] = defineField('teacherId');
const [operationalPeriodId] = defineField('operationalPeriodId');
const [studyGroupId] = defineField('studyGroupId');
const [subjectId] = defineField('subjectId');

// Selected teacher's capability preview
const selectedTeacher = computed(() =>
  (activeTeachers.value ?? []).find((t) => t.id === teacherId.value)
);

const teacherCapabilities = computed(() => selectedTeacher.value?.capabilities ?? []);

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: {
          teacherId: '',
          operationalPeriodId: resolveDefaultPeriod(),
          studyGroupId: '',
          subjectId: ''
        }
      });
    }
  }
);

function closeDrawer(): void {
  if (assignMutation.isPending.value) return;
  emit('update:visible', false);
}

function mapValidationErrors(fieldErrors: Record<string, string[]>): Record<string, string> {
  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const first = messages[0];
    if (first) {
      const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
      mapped[normalizedKey] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    const saved = await assignMutation.mutateAsync({
      teacherId: values.teacherId,
      operationalPeriodId: values.operationalPeriodId,
      subjectId: values.subjectId,
      studyGroupId: values.studyGroupId
    });

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 409) {
        // Domain rule conflicts: inactive teacher, missing capability (BR-TCH-005), duplicate assignment (BR-TCH-004)
        conflictBanner.value =
          error.detail ||
          error.title ||
          'The teacher cannot be assigned to this subject and study group.';
      } else if (error.status === 422 && error.errors) {
        setErrors(mapValidationErrors(error.errors));
        if (error.detail && Object.keys(error.errors).length === 0) {
          conflictBanner.value = error.detail;
        }
      } else {
        conflictBanner.value = error.detail || error.title || 'Failed to create assignment.';
      }
    } else if (error instanceof Error) {
      conflictBanner.value = error.message;
    }
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="w-full sm:w-[500px]"
    :modal="true"
    :dismissable="!assignMutation.isPending.value"
    data-testid="assign-teacher-drawer"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <UserCheck class="h-5 w-5 text-brand-accent-600" />
          <h2 class="text-lg font-semibold text-slate-900" data-testid="assign-teacher-drawer-title">
            Assign Teacher to Class
          </h2>
        </div>
        <p class="text-xs text-slate-500">
          Assign an active teacher with subject capability to a study group for the selected
          operational period (BR-TCH-003, BR-TCH-005).
        </p>
      </div>
    </template>

    <form class="space-y-4 py-2" @submit.prevent="onSubmit">
      <!-- Conflict Alert Banner (409 handling) -->
      <div
        v-if="conflictBanner"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-xs text-rose-900"
        role="alert"
        data-testid="assignment-conflict-banner"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <div class="space-y-0.5">
          <p class="font-medium">Assignment Conflict</p>
          <p>{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Operational Period Selector -->
      <FormField
        label="Operational Period"
        field-id="operationalPeriod"
        :required="true"
        :error="errors.operationalPeriodId"
        hint="The semester/period this teaching assignment takes place."
      >
        <Select
          id="operationalPeriod"
          v-model="operationalPeriodId"
          :options="operationalPeriods"
          option-label="name"
          option-value="id"
          placeholder="Select Operational Period"
          class="w-full text-sm"
          :invalid="Boolean(errors.operationalPeriodId)"
          data-testid="assignment-period-select"
        />
      </FormField>

      <!-- Study Group Selector -->
      <FormField
        label="Study Group (Rombel)"
        field-id="studyGroup"
        :required="true"
        :error="errors.studyGroupId"
        hint="Target class/rombel for instruction."
      >
        <Select
          id="studyGroup"
          v-model="studyGroupId"
          :options="studyGroups ?? []"
          option-label="name"
          option-value="id"
          placeholder="Select Study Group (e.g. 7A)"
          class="w-full text-sm"
          :loading="isStudyGroupsLoading"
          :invalid="Boolean(errors.studyGroupId)"
          data-testid="assignment-studygroup-select"
        />
      </FormField>

      <!-- Active Teacher Selector -->
      <FormField
        label="Teacher"
        field-id="teacher"
        :required="true"
        :error="errors.teacherId"
        hint="Only active teachers are eligible for assignments (BR-TCH-003)."
      >
        <Select
          id="teacher"
          v-model="teacherId"
          :options="activeTeachers ?? []"
          option-label="fullName"
          option-value="id"
          placeholder="Select Active Teacher"
          class="w-full text-sm"
          :loading="isTeachersLoading"
          :invalid="Boolean(errors.teacherId)"
          data-testid="assignment-teacher-select"
        />
      </FormField>

      <!-- Teacher Capabilities Tag Preview -->
      <div
        v-if="selectedTeacher"
        class="rounded-lg border border-slate-200 bg-slate-50/60 p-3 space-y-1.5"
        data-testid="selected-teacher-capabilities-preview"
      >
        <div class="flex items-center justify-between text-xs font-medium text-slate-700">
          <span>Teacher Qualified Subjects:</span>
          <span>{{ teacherCapabilities.length }} subjects</span>
        </div>
        <div v-if="teacherCapabilities.length > 0" class="flex flex-wrap gap-1.5 pt-1">
          <span
            v-for="cap in teacherCapabilities"
            :key="cap.subjectId"
            class="inline-flex items-center rounded-md bg-white border border-slate-200 px-2 py-0.5 text-xs text-slate-700"
          >
            {{ cap.subjectName }}
          </span>
        </div>
        <p v-else class="text-xs text-amber-700 font-medium">
          Warning: This teacher has no recorded subject capabilities. Assigning will fail (BR-TCH-005).
        </p>
      </div>

      <!-- Subject Selector -->
      <FormField
        label="Subject"
        field-id="subject"
        :required="true"
        :error="errors.subjectId"
        hint="Subject to be taught (must be in teacher's capabilities per BR-TCH-005)."
      >
        <Select
          id="subject"
          v-model="subjectId"
          :options="subjects ?? []"
          option-label="name"
          option-value="id"
          placeholder="Select Subject (e.g. Matematika)"
          class="w-full text-sm"
          :loading="isSubjectsLoading"
          :invalid="Boolean(errors.subjectId)"
          data-testid="assignment-subject-select"
        />
      </FormField>

      <!-- Form Actions -->
      <div class="flex justify-end gap-2 border-t border-slate-200 pt-4">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          :disabled="assignMutation.isPending.value"
          data-testid="cancel-assignment-button"
          @click="closeDrawer"
        />
        <Button
          type="submit"
          label="Assign Teacher"
          :loading="assignMutation.isPending.value"
          data-testid="submit-assignment-button"
        />
      </div>
    </form>
  </Drawer>
</template>
