<script setup lang="ts">
import { ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { ApiError } from '@/shared/types/common';
import type { StudentDetailDto, StudentDto, StudentListItemDto } from '../api/students.api';
import { useUpdateStudentMutation } from '../api/useStudentMutations';
import { studentSchema, type StudentFormValues } from '../schemas/student.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    student?: StudentDetailDto | StudentListItemDto | null;
  }>(),
  {
    student: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [student: StudentDto];
}>();

const updateMutation = useUpdateStudentMutation();
const conflictBanner = ref<string | null>(null);

const genderOptions = [
  { label: 'Male', value: 'MALE' },
  { label: 'Female', value: 'FEMALE' }
];

function buildInitialValues(student?: StudentDetailDto | StudentListItemDto | null): StudentFormValues {
  if (!student) {
    return {
      fullName: '',
      nickname: '',
      dateOfBirth: '',
      gender: 'MALE',
      nisn: '',
      phone: '',
      email: '',
      address: ''
    };
  }

  return {
    fullName: student.fullName ?? '',
    nickname: student.nickname ?? '',
    dateOfBirth: student.dateOfBirth ? String(student.dateOfBirth).substring(0, 10) : '',
    gender: (student.gender?.toUpperCase() === 'FEMALE' ? 'FEMALE' : 'MALE') as 'MALE' | 'FEMALE',
    nisn: student.nisn ?? '',
    phone: student.phone ?? '',
    email: student.email ?? '',
    address: student.address ?? ''
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

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<StudentFormValues>({
  validationSchema: toTypedSchema(studentSchema),
  initialValues: buildInitialValues(props.student)
});

const [fullName] = defineField('fullName');
const [nickname] = defineField('nickname');
const [dateOfBirth] = defineField('dateOfBirth');
const [gender] = defineField('gender');
const [nisn] = defineField('nisn');
const [phone] = defineField('phone');
const [email] = defineField('email');
const [address] = defineField('address');

watch(
  () => props.student,
  (newStudent) => {
    resetForm({ values: buildInitialValues(newStudent) });
    conflictBanner.value = null;
  },
  { immediate: true }
);

function closeDrawer(): void {
  emit('update:visible', false);
  conflictBanner.value = null;
}

const onSubmit = handleSubmit(async (formValues) => {
  if (!props.student?.id) {
    return;
  }
  conflictBanner.value = null;

  try {
    const updated = await updateMutation.mutateAsync({
      id: props.student.id,
      payload: {
        fullName: formValues.fullName.trim(),
        nickname: formValues.nickname?.trim() || null,
        dateOfBirth: formValues.dateOfBirth,
        gender: formValues.gender,
        nisn: formValues.nisn?.trim() || null,
        phone: formValues.phone?.trim() || null,
        email: formValues.email?.trim() || null,
        address: formValues.address?.trim() || null
      }
    });

    emit('saved', updated);
    closeDrawer();
  } catch (error) {
    if (error instanceof ApiError && error.status === 422 && error.errors) {
      setErrors(mapValidationErrors(error.errors));
    } else if (error instanceof ApiError && error.status === 409) {
      conflictBanner.value =
        error.detail || 'Conflict: National student number (NISN) is already registered to another student.';
    }
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    header="Edit Student Profile"
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

        <!-- Read-only NIS identifier -->
        <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
          <div class="font-medium text-slate-800">Student Identification Number (NIS)</div>
          <div class="mt-1 font-mono text-sm font-semibold text-brand-neutral-900">
            {{ student?.nis ?? 'N/A' }}
          </div>
          <div class="mt-0.5 text-slate-500">Master institutional ID assigned sequentially upon enrollment.</div>
        </div>

        <!-- Full Name -->
        <FormField label="Full Name" required field-id="fullName" :error="errors.fullName">
          <InputText
            id="fullName"
            v-model="fullName"
            placeholder="Student full legal name"
            class="w-full"
            :invalid="Boolean(errors.fullName)"
          />
        </FormField>

        <!-- Nickname & NISN -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="Nickname" field-id="nickname" :error="errors.nickname">
            <InputText
              id="nickname"
              v-model="nickname"
              placeholder="e.g. Ahmad"
              class="w-full"
              :invalid="Boolean(errors.nickname)"
            />
          </FormField>

          <FormField
            label="National ID (NISN)"
            field-id="nisn"
            hint="Unique 10-digit national identifier (BR-STU-002)"
            :error="errors.nisn"
          >
            <InputText
              id="nisn"
              v-model="nisn"
              placeholder="e.g. 0012345678"
              class="w-full"
              :invalid="Boolean(errors.nisn)"
            />
          </FormField>
        </div>

        <!-- Date of Birth & Gender -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="Date of Birth" required field-id="dateOfBirth" :error="errors.dateOfBirth">
            <InputText
              id="dateOfBirth"
              v-model="dateOfBirth"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.dateOfBirth)"
            />
          </FormField>

          <FormField label="Gender" required field-id="gender" :error="errors.gender">
            <Select
              id="gender"
              v-model="gender"
              :options="genderOptions"
              option-label="label"
              option-value="value"
              placeholder="Select gender"
              class="w-full"
              :invalid="Boolean(errors.gender)"
            />
          </FormField>
        </div>

        <!-- Phone & Email -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField label="Student Phone" field-id="phone" :error="errors.phone">
            <InputText
              id="phone"
              v-model="phone"
              placeholder="e.g. 081234567890"
              class="w-full"
              :invalid="Boolean(errors.phone)"
            />
          </FormField>

          <FormField label="Student Email" field-id="email" :error="errors.email">
            <InputText
              id="email"
              v-model="email"
              type="email"
              placeholder="e.g. student@example.com"
              class="w-full"
              :invalid="Boolean(errors.email)"
            />
          </FormField>
        </div>

        <!-- Address -->
        <FormField label="Home Address" field-id="address" :error="errors.address">
          <Textarea
            id="address"
            v-model="address"
            rows="3"
            placeholder="Street address, city, postal code"
            class="w-full"
            :invalid="Boolean(errors.address)"
          />
        </FormField>
      </div>

      <div class="mt-6 flex justify-end gap-2 border-t border-slate-200 pt-4">
        <Button label="Cancel" severity="secondary" text @click="closeDrawer" />
        <Button
          type="submit"
          label="Save Changes"
          icon="pi pi-check"
          :loading="updateMutation.isPending.value"
        />
      </div>
    </form>
  </Drawer>
</template>
