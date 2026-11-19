<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useFieldArray, useForm } from 'vee-validate';
import { useQuery } from '@tanstack/vue-query';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { AlertCircle, Plus, Trash2, UserPlus } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { academicYearsLookupQuery } from '@/shared/services/lookups.api';
import { ApiError } from '@/shared/types/common';
import type { CandidateDto } from '../api/admissions.api';
import { useRegisterCandidateMutation } from '../api/useAdmissionsMutations';
import {
  registerCandidateSchema,
  type CandidateGuardianFormValues,
  type RegisterCandidateFormValues
} from '../schemas/candidate.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    defaultAdmissionYearId?: string | null;
  }>(),
  {
    defaultAdmissionYearId: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  registered: [candidate: CandidateDto];
}>();

const { data: academicYears = [] } = useQuery(academicYearsLookupQuery());

const registerMutation = useRegisterCandidateMutation();
const isSubmitting = computed(() => registerMutation.isPending.value);
const conflictBanner = ref<string | null>(null);

function createEmptyGuardian(relationshipType: 'FATHER' | 'MOTHER' | 'GUARDIAN' = 'FATHER'): CandidateGuardianFormValues {
  return {
    fullName: '',
    relationshipType,
    phone: '',
    email: '',
    address: ''
  };
}

function buildInitialValues(): RegisterCandidateFormValues {
  return {
    admissionYearId: props.defaultAdmissionYearId ?? '',
    fullName: '',
    nickname: '',
    dateOfBirth: '',
    gender: 'MALE',
    phone: '',
    email: '',
    address: '',
    targetSchoolLevel: 'SMP',
    targetGrade: '7',
    guardians: [createEmptyGuardian('FATHER')]
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } =
  useForm<RegisterCandidateFormValues>({
    validationSchema: toTypedSchema(registerCandidateSchema),
    initialValues: buildInitialValues()
  });

const [admissionYearId, admissionYearIdAttrs] = defineField('admissionYearId');
const [fullName, fullNameAttrs] = defineField('fullName');
const [nickname, nicknameAttrs] = defineField('nickname');
const [dateOfBirth, dateOfBirthAttrs] = defineField('dateOfBirth');
const [gender, genderAttrs] = defineField('gender');
const [phone, phoneAttrs] = defineField('phone');
const [email, emailAttrs] = defineField('email');
const [address, addressAttrs] = defineField('address');
const [targetSchoolLevel, targetSchoolLevelAttrs] = defineField('targetSchoolLevel');
const [targetGrade, targetGradeAttrs] = defineField('targetGrade');

const {
  fields: guardianFields,
  push: pushGuardian,
  remove: removeGuardian
} = useFieldArray<CandidateGuardianFormValues>('guardians');

watch(
  () => [props.visible, props.defaultAdmissionYearId] as const,
  ([isOpen, defaultYearId]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: {
          ...buildInitialValues(),
          admissionYearId: defaultYearId ?? ''
        }
      });
    }
  }
);

function addGuardian(): void {
  const currentCount = guardianFields.value.length;
  const rel = currentCount === 1 ? 'MOTHER' : 'GUARDIAN';
  pushGuardian(createEmptyGuardian(rel));
}

function removeGuardianItem(index: number): void {
  // BR-STU-007: Enforce at least 1 guardian contact
  if (guardianFields.value.length <= 1) {
    return;
  }
  removeGuardian(index);
}

function getGuardianError(
  index: number,
  field: keyof CandidateGuardianFormValues
): string | undefined {
  const key = `guardians[${index}].${String(field)}` as keyof typeof errors.value;
  return errors.value[key];
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
    const registered = await registerMutation.mutateAsync({
      admissionYearId: values.admissionYearId,
      fullName: values.fullName.trim(),
      nickname: values.nickname?.trim() || null,
      dateOfBirth: values.dateOfBirth,
      gender: values.gender,
      phone: values.phone?.trim() || null,
      email: values.email?.trim() || null,
      address: values.address?.trim() || null,
      targetSchoolLevel: values.targetSchoolLevel,
      targetGrade: values.targetGrade.trim(),
      guardians: values.guardians.map((g) => ({
        fullName: g.fullName.trim(),
        relationshipType: g.relationshipType,
        phone: g.phone?.trim() || null,
        email: g.email?.trim() || null,
        address: g.address?.trim() || null
      }))
    });

    emit('registered', registered);
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

    conflictBanner.value = 'Unable to register candidate. Please verify the information and try again.';
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    header="Register Prospective Candidate"
    :dismissable="!isSubmitting"
    :closable="!isSubmitting"
    class="w-full sm:w-[640px]"
    data-testid="register-candidate-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="register-candidate-form" class="space-y-6" novalidate @submit.prevent="onSubmit">
      <!-- Error / Conflict Banner -->
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="candidate-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <span>{{ conflictBanner }}</span>
      </div>

      <!-- Section: Admission Target -->
      <div class="space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-200 pb-2">
          <UserPlus class="h-4 w-4 text-brand-accent-600" />
          <h3 class="text-sm font-semibold text-slate-900">Admission Target</h3>
        </div>

        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <FormField
            label="Admission Year"
            field-id="candidate-year"
            required
            :error="errors.admissionYearId"
            class="sm:col-span-3"
          >
            <select
              id="candidate-year"
              v-model="admissionYearId"
              v-bind="admissionYearIdAttrs"
              data-testid="candidate-year-select"
              class="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-brand-accent-600 focus:outline-none focus:ring-1 focus:ring-brand-accent-600"
              :disabled="isSubmitting"
            >
              <option value="" disabled>Select admission academic year</option>
              <option
                v-for="ay in academicYears"
                :key="ay.id"
                :value="ay.id"
              >
                {{ ay.name }} ({{ ay.status }})
              </option>
            </select>
          </FormField>

          <FormField
            label="School Level"
            field-id="candidate-school-level"
            required
            :error="errors.targetSchoolLevel"
          >
            <select
              id="candidate-school-level"
              v-model="targetSchoolLevel"
              v-bind="targetSchoolLevelAttrs"
              data-testid="candidate-level-select"
              class="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-brand-accent-600 focus:outline-none focus:ring-1 focus:ring-brand-accent-600"
              :disabled="isSubmitting"
            >
              <option value="SD">SD (Elementary)</option>
              <option value="SMP">SMP (Junior High)</option>
              <option value="SMA">SMA (Senior High)</option>
              <option value="SMK">SMK (Vocational)</option>
            </select>
          </FormField>

          <FormField
            label="Target Grade"
            field-id="candidate-target-grade"
            required
            :error="errors.targetGrade"
            hint="e.g. 1, 7, 10"
            class="sm:col-span-2"
          >
            <InputText
              id="candidate-target-grade"
              v-model="targetGrade"
              v-bind="targetGradeAttrs"
              data-testid="candidate-grade-input"
              placeholder="e.g. 7"
              class="w-full"
              :invalid="Boolean(errors.targetGrade)"
              :disabled="isSubmitting"
            />
          </FormField>
        </div>
      </div>

      <!-- Section: Candidate Demographics -->
      <div class="space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-200 pb-2">
          <h3 class="text-sm font-semibold text-slate-900">Personal Demographics</h3>
        </div>

        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField
            label="Full Name"
            field-id="candidate-fullname"
            required
            :error="errors.fullName"
            class="sm:col-span-2"
          >
            <InputText
              id="candidate-fullname"
              v-model="fullName"
              v-bind="fullNameAttrs"
              data-testid="candidate-fullname-input"
              placeholder="Candidate official name"
              class="w-full"
              :invalid="Boolean(errors.fullName)"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField
            label="Nickname"
            field-id="candidate-nickname"
            :error="errors.nickname"
          >
            <InputText
              id="candidate-nickname"
              v-model="nickname"
              v-bind="nicknameAttrs"
              data-testid="candidate-nickname-input"
              placeholder="Familiar name"
              class="w-full"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField
            label="Gender"
            field-id="candidate-gender"
            required
            :error="errors.gender"
          >
            <select
              id="candidate-gender"
              v-model="gender"
              v-bind="genderAttrs"
              data-testid="candidate-gender-select"
              class="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-brand-accent-600 focus:outline-none focus:ring-1 focus:ring-brand-accent-600"
              :disabled="isSubmitting"
            >
              <option value="MALE">Male (Laki-laki)</option>
              <option value="FEMALE">Female (Perempuan)</option>
            </select>
          </FormField>

          <FormField
            label="Date of Birth"
            field-id="candidate-dob"
            required
            :error="errors.dateOfBirth"
          >
            <InputText
              id="candidate-dob"
              v-model="dateOfBirth"
              v-bind="dateOfBirthAttrs"
              data-testid="candidate-dob-input"
              type="date"
              class="w-full"
              :invalid="Boolean(errors.dateOfBirth)"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField
            label="Phone Number"
            field-id="candidate-phone"
            :error="errors.phone"
          >
            <InputText
              id="candidate-phone"
              v-model="phone"
              v-bind="phoneAttrs"
              data-testid="candidate-phone-input"
              placeholder="e.g. 081234567890"
              class="w-full"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField
            label="Email Address"
            field-id="candidate-email"
            :error="errors.email"
            class="sm:col-span-2"
          >
            <InputText
              id="candidate-email"
              v-model="email"
              v-bind="emailAttrs"
              data-testid="candidate-email-input"
              type="email"
              placeholder="e.g. candidate@example.com"
              class="w-full"
              :invalid="Boolean(errors.email)"
              :disabled="isSubmitting"
            />
          </FormField>

          <FormField
            label="Residential Address"
            field-id="candidate-address"
            :error="errors.address"
            class="sm:col-span-2"
          >
            <Textarea
              id="candidate-address"
              v-model="address"
              v-bind="addressAttrs"
              data-testid="candidate-address-input"
              rows="2"
              placeholder="Street name, neighborhood, city"
              class="w-full"
              :disabled="isSubmitting"
            />
          </FormField>
        </div>
      </div>

      <!-- Section: Guardians (BR-STU-007 Dynamic 1..N) -->
      <div class="space-y-4">
        <div class="flex items-center justify-between border-b border-slate-200 pb-2">
          <div>
            <h3 class="text-sm font-semibold text-slate-900">Guardian Contacts</h3>
            <p class="text-xs text-slate-500">At least 1 guardian is required (BR-STU-007)</p>
          </div>
          <Button
            type="button"
            severity="secondary"
            size="small"
            outlined
            data-testid="add-guardian-button"
            :disabled="isSubmitting"
            @click="addGuardian"
          >
            <Plus class="mr-1.5 h-3.5 w-3.5" />
            Add Guardian
          </Button>
        </div>

        <div class="space-y-4">
          <div
            v-for="(guardian, index) in guardianFields"
            :key="guardian.key"
            data-testid="guardian-item"
            class="space-y-3 rounded-lg border border-slate-200 bg-slate-50/50 p-3.5"
          >
            <div class="flex items-center justify-between">
              <span class="text-xs font-semibold text-slate-700">Guardian #{{ index + 1 }}</span>
              <Button
                v-if="guardianFields.length > 1"
                type="button"
                severity="danger"
                text
                size="small"
                data-testid="remove-guardian-button"
                :disabled="isSubmitting"
                @click="removeGuardianItem(index)"
              >
                <Trash2 class="h-3.5 w-3.5 text-rose-600" />
              </Button>
            </div>

            <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <FormField
                label="Full Name"
                :field-id="`guardian-${index}-name`"
                required
                :error="getGuardianError(index, 'fullName')"
              >
                <InputText
                  :id="`guardian-${index}-name`"
                  v-model="guardian.value.fullName"
                  data-testid="guardian-fullname-input"
                  placeholder="Guardian official name"
                  class="w-full"
                  :invalid="Boolean(getGuardianError(index, 'fullName'))"
                  :disabled="isSubmitting"
                />
              </FormField>

              <FormField
                label="Relationship"
                :field-id="`guardian-${index}-rel`"
                required
                :error="getGuardianError(index, 'relationshipType')"
              >
                <select
                  :id="`guardian-${index}-rel`"
                  v-model="guardian.value.relationshipType"
                  data-testid="guardian-rel-select"
                  class="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-brand-accent-600 focus:outline-none focus:ring-1 focus:ring-brand-accent-600"
                  :disabled="isSubmitting"
                >
                  <option value="FATHER">Father (Ayah)</option>
                  <option value="MOTHER">Mother (Ibu)</option>
                  <option value="GUARDIAN">Guardian (Wali)</option>
                </select>
              </FormField>

              <FormField
                label="Phone Number"
                :field-id="`guardian-${index}-phone`"
                :error="getGuardianError(index, 'phone')"
              >
                <InputText
                  :id="`guardian-${index}-phone`"
                  v-model="guardian.value.phone"
                  data-testid="guardian-phone-input"
                  placeholder="Phone number"
                  class="w-full"
                  :disabled="isSubmitting"
                />
              </FormField>

              <FormField
                label="Email Address"
                :field-id="`guardian-${index}-email`"
                :error="getGuardianError(index, 'email')"
              >
                <InputText
                  :id="`guardian-${index}-email`"
                  v-model="guardian.value.email"
                  data-testid="guardian-email-input"
                  type="email"
                  placeholder="Email address"
                  class="w-full"
                  :invalid="Boolean(getGuardianError(index, 'email'))"
                  :disabled="isSubmitting"
                />
              </FormField>

              <FormField
                label="Address"
                :field-id="`guardian-${index}-address`"
                :error="getGuardianError(index, 'address')"
                class="sm:col-span-2"
              >
                <InputText
                  :id="`guardian-${index}-address`"
                  v-model="guardian.value.address"
                  data-testid="guardian-address-input"
                  placeholder="Contact address"
                  class="w-full"
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
          form="register-candidate-form"
          label="Register Candidate"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="submit-candidate-button"
        />
      </div>
    </template>
  </Drawer>
</template>
