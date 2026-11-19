<script setup lang="ts">
import { computed, ref, watch } from 'vue';
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
import type { TeacherDetailDto, TeacherDto } from '../api/teachers.api';
import {
  useCreateTeacherMutation,
  useUpdateTeacherMutation
} from '../api/useTeacherMutations';
import {
  GENDER_OPTIONS,
  teacherSchema,
  type GenderType,
  type TeacherFormValues
} from '../schemas/teacher.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    teacher?: TeacherDto | TeacherDetailDto | null;
  }>(),
  {
    teacher: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [teacher: TeacherDto];
}>();

const isEditMode = computed(() => Boolean(props.teacher?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Teacher Profile' : 'Register Faculty Member'
);

const createMutation = useCreateTeacherMutation();
const updateMutation = useUpdateTeacherMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function normalizeDate(raw?: string | null): string {
  if (!raw) return '';
  return raw.includes('T') ? raw.split('T')[0] ?? '' : raw;
}

function buildInitialValues(teacher?: TeacherDto | TeacherDetailDto | null): TeacherFormValues {
  if (!teacher) {
    return {
      fullName: '',
      nickname: '',
      nip: '',
      nik: '',
      dateOfBirth: '',
      gender: 'MALE',
      phone: '',
      email: '',
      address: ''
    };
  }

  const genderVal = (teacher.gender?.toUpperCase() as GenderType) || 'MALE';

  return {
    fullName: teacher.fullName ?? '',
    nickname: teacher.nickname ?? '',
    nip: teacher.nip ?? '',
    nik: teacher.nik ?? '',
    dateOfBirth: normalizeDate(teacher.dateOfBirth),
    gender: GENDER_OPTIONS.some((g) => g.value === genderVal) ? genderVal : 'MALE',
    phone: teacher.phone ?? '',
    email: teacher.email ?? '',
    address: teacher.address ?? ''
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<TeacherFormValues>({
  validationSchema: toTypedSchema(teacherSchema),
  initialValues: buildInitialValues(props.teacher)
});

const [fullName, fullNameAttrs] = defineField('fullName');
const [nickname, nicknameAttrs] = defineField('nickname');
const [nip, nipAttrs] = defineField('nip');
const [nik, nikAttrs] = defineField('nik');
const [dateOfBirth, dateOfBirthAttrs] = defineField('dateOfBirth');
const [gender] = defineField('gender');
const [phone, phoneAttrs] = defineField('phone');
const [email, emailAttrs] = defineField('email');
const [address, addressAttrs] = defineField('address');

watch(
  () => [props.visible, props.teacher] as const,
  ([isOpen, teacher]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(teacher)
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
      const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
      mapped[normalizedKey] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    let saved: TeacherDto;
    if (isEditMode.value && props.teacher?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.teacher.id,
        payload: {
          fullName: values.fullName.trim(),
          nickname: values.nickname?.trim() || null,
          nip: values.nip?.trim() || null,
          nik: values.nik?.trim() || null,
          dateOfBirth: values.dateOfBirth,
          gender: values.gender,
          phone: values.phone?.trim() || null,
          email: values.email?.trim() || null,
          address: values.address?.trim() || null
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        fullName: values.fullName.trim(),
        nickname: values.nickname?.trim() || null,
        nip: values.nip?.trim() || null,
        nik: values.nik?.trim() || null,
        dateOfBirth: values.dateOfBirth,
        gender: values.gender,
        phone: values.phone?.trim() || null,
        email: values.email?.trim() || null,
        address: values.address?.trim() || null
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
      } else if (error.status === 409) {
        conflictBanner.value =
          error.detail || 'A teacher record with these identification numbers already exists.';
      } else {
        conflictBanner.value = error.detail || error.title || 'An unexpected error occurred.';
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
    :dismissable="!isSubmitting"
    data-testid="teacher-form-drawer"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="space-y-1">
        <h2 class="text-lg font-semibold text-slate-900" data-testid="teacher-form-drawer-title">
          {{ drawerTitle }}
        </h2>
        <p class="text-xs text-slate-500">
          {{
            isEditMode
              ? 'Update faculty personal details, NIP/NIK identification, and contact info.'
              : 'Register a new active faculty member. At least one contact method is required.'
          }}
        </p>
      </div>
    </template>

    <form id="teacher-form" data-testid="teacher-form" class="space-y-4 py-2" @submit.prevent="onSubmit">
      <!-- Conflict or Global Error Banner -->
      <div
        v-if="conflictBanner"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900"
        role="alert"
        data-testid="teacher-form-conflict-banner"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="space-y-0.5">
          <p class="font-medium">Registration Issue</p>
          <p>{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Full Name -->
      <FormField
        label="Full Name"
        field-id="fullName"
        :required="true"
        :error="errors.fullName"
        hint="Legal full name with academic titles if applicable."
      >
        <InputText
          id="fullName"
          v-model="fullName"
          v-bind="fullNameAttrs"
          placeholder="e.g. Dra. Siti Rahmawati, M.Pd."
          class="w-full text-sm"
          :invalid="Boolean(errors.fullName)"
          data-testid="teacher-fullname-input"
        />
      </FormField>

      <!-- Nickname & Gender -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField label="Nickname" field-id="nickname" :error="errors.nickname">
          <InputText
            id="nickname"
            v-model="nickname"
            v-bind="nicknameAttrs"
            placeholder="e.g. Bu Siti"
            class="w-full text-sm"
            data-testid="teacher-nickname-input"
          />
        </FormField>

        <FormField label="Gender" field-id="gender" :required="true" :error="errors.gender">
          <Select
            id="gender"
            v-model="gender"
            :options="GENDER_OPTIONS"
            option-label="label"
            option-value="value"
            placeholder="Select Gender"
            class="w-full text-sm"
            :invalid="Boolean(errors.gender)"
            data-testid="teacher-gender-select"
          />
        </FormField>
      </div>

      <!-- Date of Birth -->
      <FormField
        label="Date of Birth"
        field-id="dateOfBirth"
        :required="true"
        :error="errors.dateOfBirth"
        hint="YYYY-MM-DD format. Cannot be in the future."
      >
        <input
          id="dateOfBirth"
          v-model="dateOfBirth"
          v-bind="dateOfBirthAttrs"
          type="date"
          class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-xs focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
          :class="{ 'border-rose-500': Boolean(errors.dateOfBirth) }"
          data-testid="teacher-dob-input"
        />
      </FormField>

      <!-- NIP & NIK Identifiers -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField
          label="NIP (Civil Servant ID)"
          field-id="nip"
          :error="errors.nip"
          hint="Optional (PNS only)."
        >
          <InputText
            id="nip"
            v-model="nip"
            v-bind="nipAttrs"
            placeholder="18-digit NIP"
            class="w-full text-sm"
            data-testid="teacher-nip-input"
          />
        </FormField>

        <FormField
          label="NIK (National ID)"
          field-id="nik"
          :error="errors.nik"
          hint="16-digit KTP number."
        >
          <InputText
            id="nik"
            v-model="nik"
            v-bind="nikAttrs"
            placeholder="16-digit NIK"
            class="w-full text-sm"
            data-testid="teacher-nik-input"
          />
        </FormField>
      </div>

      <!-- Contact Info Section (BR-TCH-002: At least one required) -->
      <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-3.5 space-y-3">
        <div class="flex items-center justify-between">
          <span class="text-xs font-semibold uppercase tracking-wider text-slate-700">
            Contact Methods
          </span>
          <span class="text-xs text-brand-accent-600 font-medium">
            At least 1 required (BR-TCH-002)
          </span>
        </div>

        <FormField
          label="Phone Number"
          field-id="phone"
          :error="errors.phone"
          hint="Active mobile or WhatsApp number."
        >
          <InputText
            id="phone"
            v-model="phone"
            v-bind="phoneAttrs"
            placeholder="e.g. 081234567890"
            class="w-full text-sm"
            :invalid="Boolean(errors.phone)"
            data-testid="teacher-phone-input"
          />
        </FormField>

        <FormField
          label="Email Address"
          field-id="email"
          :error="errors.email"
          hint="School or personal email."
        >
          <InputText
            id="email"
            v-model="email"
            v-bind="emailAttrs"
            type="email"
            placeholder="e.g. siti.rahmawati@sekolah.sch.id"
            class="w-full text-sm"
            :invalid="Boolean(errors.email)"
            data-testid="teacher-email-input"
          />
        </FormField>
      </div>

      <!-- Address -->
      <FormField label="Residential Address" field-id="address" :error="errors.address">
        <Textarea
          id="address"
          v-model="address"
          v-bind="addressAttrs"
          rows="2"
          placeholder="Complete street address..."
          class="w-full text-sm"
          data-testid="teacher-address-input"
        />
      </FormField>

      <!-- Form Actions -->
      <div class="flex justify-end gap-2 border-t border-slate-200 pt-4">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          :disabled="isSubmitting"
          data-testid="cancel-teacher-button"
          @click="closeDrawer"
        />
        <Button
          type="submit"
          :label="isEditMode ? 'Save Changes' : 'Register Teacher'"
          :loading="isSubmitting"
          data-testid="save-teacher-button"
        />
      </div>
    </form>
  </Drawer>
</template>
