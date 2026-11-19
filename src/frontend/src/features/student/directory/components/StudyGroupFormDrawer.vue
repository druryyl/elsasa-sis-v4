<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import {
  fetchAcademicYearsLookup,
  fetchActiveTeachersLookup,
  type AcademicYearLookupItem,
  type TeacherLookupItem
} from '@/shared/services/lookups.api';
import { ApiError } from '@/shared/types/common';
import type { StudyGroupDto, StudyGroupListItemDto } from '../api/studyGroups.api';
import {
  useCreateStudyGroupMutation,
  useUpdateStudyGroupMutation
} from '../api/useStudentMutations';
import { studyGroupSchema, type StudyGroupFormValues } from '../schemas/studyGroup.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studyGroup?: StudyGroupListItemDto | StudyGroupDto | null;
  }>(),
  {
    studyGroup: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [studyGroup: StudyGroupDto];
}>();

const { activeAcademicYearId } = useAppContextBridge();
const createMutation = useCreateStudyGroupMutation();
const updateMutation = useUpdateStudyGroupMutation();

const isEditMode = computed(() => Boolean(props.studyGroup?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Study Group' : 'Create Study Group (Rombel)'
);

const conflictBanner = ref<string | null>(null);
const academicYears = ref<AcademicYearLookupItem[]>([]);
const activeTeachers = ref<TeacherLookupItem[]>([]);
const isLoadingLookups = ref(false);

const schoolLevelOptions = [
  { label: 'SD (Elementary)', value: 'SD' },
  { label: 'SMP (Junior High)', value: 'SMP' },
  { label: 'SMA (Senior High)', value: 'SMA' },
  { label: 'SMK (Vocational)', value: 'SMK' }
];

function buildInitialValues(group?: StudyGroupListItemDto | StudyGroupDto | null): StudyGroupFormValues {
  if (!group) {
    return {
      academicYearId: activeAcademicYearId.value ?? '',
      schoolLevel: 'SMP',
      grade: '7',
      name: '',
      homeroomTeacherId: ''
    };
  }

  return {
    academicYearId: group.academicYearId ?? (activeAcademicYearId.value ?? ''),
    schoolLevel: (group.schoolLevel?.toUpperCase() as 'SD' | 'SMP' | 'SMA' | 'SMK') || 'SMP',
    grade: group.grade ?? '',
    name: group.name ?? '',
    homeroomTeacherId: group.homeroomTeacherId ?? ''
  };
}

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

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<StudyGroupFormValues>({
  validationSchema: toTypedSchema(studyGroupSchema),
  initialValues: buildInitialValues(props.studyGroup)
});

const [academicYearId] = defineField('academicYearId');
const [schoolLevel] = defineField('schoolLevel');
const [grade] = defineField('grade');
const [name] = defineField('name');
const [homeroomTeacherId] = defineField('homeroomTeacherId');

async function loadLookups(): Promise<void> {
  isLoadingLookups.value = true;
  try {
    const [years, teachers] = await Promise.all([
      fetchAcademicYearsLookup(),
      fetchActiveTeachersLookup()
    ]);
    academicYears.value = years;
    activeTeachers.value = teachers;
  } catch {
    // Non-fatal fallback
  } finally {
    isLoadingLookups.value = false;
  }
}

watch(
  () => props.visible,
  async (isVisible) => {
    if (isVisible) {
      conflictBanner.value = null;
      await loadLookups();
      resetForm({ values: buildInitialValues(props.studyGroup) });
    }
  }
);

function closeDrawer(): void {
  emit('update:visible', false);
  conflictBanner.value = null;
}

const academicYearOptions = computed(() =>
  academicYears.value.map((y) => ({
    label: `${y.name}${y.status === 'ACTIVE' ? ' (Active)' : ''}`,
    value: y.id
  }))
);

const teacherOptions = computed(() => [
  { label: 'None (Unassigned)', value: '' },
  ...activeTeachers.value.map((t) => ({
    label: `${t.fullName}${t.nip ? ' (' + t.nip + ')' : ''}`,
    value: t.id
  }))
]);

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const onSubmit = handleSubmit(async (formValues) => {
  conflictBanner.value = null;
  const teacherIdVal = formValues.homeroomTeacherId?.trim() || null;

  try {
    let result: StudyGroupDto;
    if (isEditMode.value && props.studyGroup?.id) {
      result = await updateMutation.mutateAsync({
        id: props.studyGroup.id,
        payload: {
          academicYearId: formValues.academicYearId,
          schoolLevel: formValues.schoolLevel,
          grade: formValues.grade.trim(),
          name: formValues.name.trim(),
          homeroomTeacherId: teacherIdVal
        }
      });
    } else {
      result = await createMutation.mutateAsync({
        academicYearId: formValues.academicYearId,
        schoolLevel: formValues.schoolLevel,
        grade: formValues.grade.trim(),
        name: formValues.name.trim(),
        homeroomTeacherId: teacherIdVal
      });
    }

    emit('saved', result);
    closeDrawer();
  } catch (error) {
    if (error instanceof ApiError && error.status === 422 && error.errors) {
      setErrors(mapValidationErrors(error.errors));
    } else if (error instanceof ApiError && error.status === 409) {
      conflictBanner.value =
        error.detail || 'Conflict: A study group with this name already exists in this academic year.';
    }
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    :header="drawerTitle"
    class="!w-full sm:!w-[520px]"
    @update:visible="closeDrawer"
  >
    <form class="flex h-full flex-col justify-between" novalidate @submit.prevent="onSubmit">
      <div class="space-y-5 overflow-y-auto pr-1">
        <!-- Conflict Alert Banner -->
        <div
          v-if="conflictBanner"
          role="alert"
          class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3.5 text-sm text-amber-900"
        >
          <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
          <div class="flex-1 text-xs leading-relaxed">{{ conflictBanner }}</div>
        </div>

        <!-- Academic Year -->
        <FormField
          label="Academic Year"
          required
          field-id="sgAcademicYear"
          :error="errors.academicYearId"
        >
          <Select
            id="sgAcademicYear"
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
          <FormField label="School Level" required field-id="sgSchoolLevel" :error="errors.schoolLevel">
            <Select
              id="sgSchoolLevel"
              v-model="schoolLevel"
              :options="schoolLevelOptions"
              option-label="label"
              option-value="value"
              placeholder="Select level"
              class="w-full"
              :invalid="Boolean(errors.schoolLevel)"
            />
          </FormField>

          <FormField label="Grade" required field-id="sgGrade" :error="errors.grade">
            <InputText
              id="sgGrade"
              v-model="grade"
              placeholder="e.g. 7"
              class="w-full"
              :invalid="Boolean(errors.grade)"
            />
          </FormField>
        </div>

        <!-- Study Group Name -->
        <FormField
          label="Study Group Name (Rombel)"
          required
          field-id="sgName"
          hint="Identifier for the cohort (e.g. 7A, 8B, 10 IPA 1, 10 TKJ 1)"
          :error="errors.name"
        >
          <InputText
            id="sgName"
            v-model="name"
            placeholder="e.g. 7A"
            class="w-full"
            :invalid="Boolean(errors.name)"
          />
        </FormField>

        <!-- Homeroom Teacher -->
        <FormField
          label="Homeroom Teacher (Wali Kelas)"
          field-id="sgTeacher"
          hint="Active faculty member designated as the primary pastoral mentor (UC-ADM-10)"
          :error="errors.homeroomTeacherId"
        >
          <Select
            id="sgTeacher"
            v-model="homeroomTeacherId"
            :options="teacherOptions"
            option-label="label"
            option-value="value"
            placeholder="Select homeroom teacher (optional)"
            class="w-full"
            filter
            :loading="isLoadingLookups"
            :invalid="Boolean(errors.homeroomTeacherId)"
          />
        </FormField>
      </div>

      <div class="mt-6 flex justify-end gap-2 border-t border-slate-200 pt-4">
        <Button label="Cancel" severity="secondary" text @click="closeDrawer" />
        <Button
          type="submit"
          :label="isEditMode ? 'Update Study Group' : 'Create Study Group'"
          icon="pi pi-check"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Drawer>
</template>
