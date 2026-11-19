<script setup lang="ts">
import { computed, ref } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import { AlertCircle, Phone, UserCheck, Users } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import { ApiError } from '@/shared/types/common';
import type { StudentGuardianDto } from '../api/students.api';
import {
  useAddGuardianMutation,
  useRemoveGuardianMutation,
  useUpdateGuardianMutation
} from '../api/useStudentMutations';
import { guardianSchema, type GuardianFormValues } from '../schemas/guardian.schema';

const props = withDefaults(
  defineProps<{
    studentId: string;
    guardians: StudentGuardianDto[];
    canManage?: boolean;
  }>(),
  {
    canManage: true
  }
);

const emit = defineEmits<{
  changed: [];
}>();

const addMutation = useAddGuardianMutation();
const updateMutation = useUpdateGuardianMutation();
const removeMutation = useRemoveGuardianMutation();

const relationshipOptions = [
  { label: 'Father', value: 'FATHER' },
  { label: 'Mother', value: 'MOTHER' },
  { label: 'Guardian', value: 'GUARDIAN' }
];

// Dialog state
const isFormDialogOpen = ref(false);
const editingGuardian = ref<StudentGuardianDto | null>(null);
const formConflictBanner = ref<string | null>(null);

// Delete confirmation state
const guardianToDelete = ref<StudentGuardianDto | null>(null);
const isDeleteDialogOpen = ref(false);
const deleteConflictBanner = ref<string | null>(null);

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

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<GuardianFormValues>({
  validationSchema: toTypedSchema(guardianSchema),
  initialValues: {
    fullName: '',
    relationshipType: 'MOTHER',
    phone: '',
    email: '',
    address: ''
  }
});

const [fullName] = defineField('fullName');
const [relationshipType] = defineField('relationshipType');
const [phone] = defineField('phone');
const [email] = defineField('email');
const [address] = defineField('address');

function openAddDialog(): void {
  editingGuardian.value = null;
  formConflictBanner.value = null;
  resetForm({
    values: {
      fullName: '',
      relationshipType: 'MOTHER',
      phone: '',
      email: '',
      address: ''
    }
  });
  isFormDialogOpen.value = true;
}

function openEditDialog(guardian: StudentGuardianDto): void {
  editingGuardian.value = guardian;
  formConflictBanner.value = null;
  const rel = (guardian.relationshipType?.toUpperCase() ?? 'GUARDIAN') as 'FATHER' | 'MOTHER' | 'GUARDIAN';
  resetForm({
    values: {
      fullName: guardian.fullName ?? '',
      relationshipType: ['FATHER', 'MOTHER', 'GUARDIAN'].includes(rel) ? rel : 'GUARDIAN',
      phone: guardian.phone ?? '',
      email: guardian.email ?? '',
      address: guardian.address ?? ''
    }
  });
  isFormDialogOpen.value = true;
}

function closeFormDialog(): void {
  isFormDialogOpen.value = false;
  editingGuardian.value = null;
  formConflictBanner.value = null;
}

const isSaving = computed(
  () => addMutation.isPending.value || updateMutation.isPending.value
);

const onSaveForm = handleSubmit(async (values) => {
  formConflictBanner.value = null;
  try {
    if (editingGuardian.value?.id) {
      await updateMutation.mutateAsync({
        studentId: props.studentId,
        guardianId: editingGuardian.value.id,
        payload: {
          fullName: values.fullName.trim(),
          relationshipType: values.relationshipType,
          phone: values.phone?.trim() || null,
          email: values.email?.trim() || null,
          address: values.address?.trim() || null
        }
      });
    } else {
      await addMutation.mutateAsync({
        studentId: props.studentId,
        payload: {
          fullName: values.fullName.trim(),
          relationshipType: values.relationshipType,
          phone: values.phone?.trim() || null,
          email: values.email?.trim() || null,
          address: values.address?.trim() || null
        }
      });
    }
    emit('changed');
    closeFormDialog();
  } catch (error) {
    if (error instanceof ApiError && error.status === 422 && error.errors) {
      setErrors(mapValidationErrors(error.errors));
    } else if (error instanceof ApiError && error.status === 409) {
      formConflictBanner.value = error.detail || 'Conflict: Unable to save guardian record.';
    }
  }
});

function promptDeleteGuardian(guardian: StudentGuardianDto): void {
  if (props.guardians.length <= 1) {
    return;
  }
  deleteConflictBanner.value = null;
  guardianToDelete.value = guardian;
  isDeleteDialogOpen.value = true;
}

async function confirmDeleteGuardian(): Promise<void> {
  if (!guardianToDelete.value?.id) return;
  try {
    await removeMutation.mutateAsync({
      studentId: props.studentId,
      guardianId: guardianToDelete.value.id
    });
    emit('changed');
    isDeleteDialogOpen.value = false;
    guardianToDelete.value = null;
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      deleteConflictBanner.value =
        error.detail ||
        'Cannot remove the last guardian. Every student must have at least one registered parent/guardian (BR-STU-003).';
    }
  }
}

const isLastGuardian = computed(() => props.guardians.length <= 1);
const lastGuardianTooltip =
  'A student must have at least one registered parent/guardian (BR-STU-003). Cannot delete the last contact.';
</script>

<template>
  <div class="space-y-4">
    <!-- Header with Add Button -->
    <div class="flex items-center justify-between border-b border-slate-200 pb-3">
      <div>
        <h3 class="text-sm font-semibold text-slate-800">Parents &amp; Guardians</h3>
        <p class="text-xs text-slate-500">Official emergency contacts and legal guardians (BR-STU-003/007).</p>
      </div>

      <Button
        v-if="canManage"
        size="small"
        icon="pi pi-plus"
        label="Add Guardian"
        @click="openAddDialog"
      />
    </div>

    <!-- Error banner for 409 conflict during delete -->
    <div
      v-if="deleteConflictBanner"
      role="alert"
      class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800"
    >
      <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
      <div class="flex-1">{{ deleteConflictBanner }}</div>
    </div>

    <!-- Guardians List -->
    <div v-if="guardians.length === 0" class="rounded-lg border border-dashed border-slate-300 p-6 text-center">
      <Users class="mx-auto h-8 w-8 text-slate-400" />
      <p class="mt-2 text-xs text-slate-500">No guardian records registered.</p>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="(guardian, idx) in guardians"
        :key="guardian.id ?? idx"
        class="flex flex-col justify-between gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-xs sm:flex-row sm:items-center"
      >
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <span class="text-sm font-semibold text-slate-900">{{ guardian.fullName }}</span>
            <span
              class="inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium"
              :class="{
                'bg-blue-50 text-blue-700': guardian.relationshipType === 'FATHER',
                'bg-purple-50 text-purple-700': guardian.relationshipType === 'MOTHER',
                'bg-emerald-50 text-emerald-700': guardian.relationshipType === 'GUARDIAN'
              }"
            >
              {{ guardian.relationshipType }}
            </span>
            <span
              v-if="idx === 0"
              class="inline-flex items-center gap-1 rounded bg-amber-50 px-1.5 py-0.5 text-[10px] font-medium text-amber-700"
              title="Primary Contact"
            >
              <UserCheck class="h-3 w-3" />
              Primary
            </span>
          </div>

          <!-- Contact details -->
          <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600">
            <div v-if="guardian.phone" class="flex items-center gap-1 font-medium text-slate-800">
              <Phone class="h-3.5 w-3.5 text-brand-accent-600" />
              <span>{{ guardian.phone }}</span>
            </div>
            <div v-else class="italic text-slate-400">No phone provided</div>

            <div v-if="guardian.email" class="text-slate-500">
              {{ guardian.email }}
            </div>
            <div v-if="guardian.address" class="text-slate-500">
              {{ guardian.address }}
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div v-if="canManage" class="flex items-center gap-1">
          <Button
            size="small"
            severity="secondary"
            text
            icon="pi pi-pencil"
            aria-label="Edit guardian"
            title="Edit Guardian"
            @click="openEditDialog(guardian)"
          />

          <!-- Disabled delete button when last guardian -->
          <span :title="isLastGuardian ? lastGuardianTooltip : 'Remove Guardian'">
            <Button
              size="small"
              severity="danger"
              text
              icon="pi pi-trash"
              :disabled="isLastGuardian"
              aria-label="Remove guardian"
              @click="promptDeleteGuardian(guardian)"
            />
          </span>
        </div>
      </div>
    </div>

    <!-- Add/Edit Guardian Dialog -->
    <Dialog
      v-model:visible="isFormDialogOpen"
      modal
      :header="editingGuardian ? 'Edit Guardian Record' : 'Register New Guardian'"
      class="!w-full sm:!w-[480px]"
    >
      <form novalidate @submit.prevent="onSaveForm">
        <div class="space-y-4">
          <div
            v-if="formConflictBanner"
            role="alert"
            class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900"
          >
            <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
            <div class="flex-1">{{ formConflictBanner }}</div>
          </div>

          <FormField label="Full Name" required field-id="gFullName" :error="errors.fullName">
            <InputText
              id="gFullName"
              v-model="fullName"
              placeholder="Guardian full legal name"
              class="w-full"
              :invalid="Boolean(errors.fullName)"
            />
          </FormField>

          <FormField
            label="Relationship Type"
            required
            field-id="gRelationship"
            :error="errors.relationshipType"
          >
            <Select
              id="gRelationship"
              v-model="relationshipType"
              :options="relationshipOptions"
              option-label="label"
              option-value="value"
              placeholder="Select relationship"
              class="w-full"
              :invalid="Boolean(errors.relationshipType)"
            />
          </FormField>

          <FormField
            label="Emergency Phone"
            field-id="gPhone"
            hint="Primary telephone number for school notifications & emergencies"
            :error="errors.phone"
          >
            <InputText
              id="gPhone"
              v-model="phone"
              placeholder="e.g. 081234567890"
              class="w-full"
              :invalid="Boolean(errors.phone)"
            />
          </FormField>

          <FormField label="Email" field-id="gEmail" :error="errors.email">
            <InputText
              id="gEmail"
              v-model="email"
              type="email"
              placeholder="e.g. parent@example.com"
              class="w-full"
              :invalid="Boolean(errors.email)"
            />
          </FormField>

          <FormField label="Residential Address" field-id="gAddress" :error="errors.address">
            <Textarea
              id="gAddress"
              v-model="address"
              rows="2"
              placeholder="Guardian street address"
              class="w-full"
              :invalid="Boolean(errors.address)"
            />
          </FormField>
        </div>

        <div class="mt-6 flex justify-end gap-2 border-t border-slate-200 pt-4">
          <Button label="Cancel" severity="secondary" text @click="closeFormDialog" />
          <Button
            type="submit"
            :label="editingGuardian ? 'Save Guardian' : 'Add Guardian'"
            :loading="isSaving"
          />
        </div>
      </form>
    </Dialog>

    <!-- Confirm Remove Guardian Dialog -->
    <ConfirmActionDialog
      v-model:visible="isDeleteDialogOpen"
      title="Remove Guardian Contact"
      :message="`Are you sure you want to remove ${guardianToDelete?.fullName ?? 'this guardian'}? This action cannot be undone.`"
      confirm-label="Remove Guardian"
      variant="danger"
      :loading="removeMutation.isPending.value"
      @confirm="confirmDeleteGuardian"
      @cancel="isDeleteDialogOpen = false"
    />
  </div>
</template>
