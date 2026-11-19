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
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { ApiError } from '@/shared/types/common';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import type { AnnouncementDto } from '../api/announcements.api';
import {
  useCreateAnnouncementMutation,
  useUpdateAnnouncementMutation
} from '../api/useAnnouncementMutations';
import {
  announcementSchema,
  AUDIENCE_OPTIONS,
  type AnnouncementAudience,
  type AnnouncementFormValues
} from '../schemas/announcement.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    announcement?: AnnouncementDto | null;
  }>(),
  {
    announcement: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [announcement: AnnouncementDto];
}>();

const { activeAcademicYearId } = useAppContextBridge();

const isEditMode = computed(() => Boolean(props.announcement?.id));
const drawerTitle = computed(() =>
  isEditMode.value ? 'Edit Announcement' : 'Compose Announcement Draft'
);

const createMutation = useCreateAnnouncementMutation();
const updateMutation = useUpdateAnnouncementMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(announcement?: AnnouncementDto | null): AnnouncementFormValues {
  if (!announcement) {
    return {
      title: '',
      targetAudience: 'ALL',
      content: '',
      academicYearId: activeAcademicYearId.value ?? undefined
    };
  }

  const audience = (announcement.targetAudience?.toUpperCase() as AnnouncementAudience) || 'ALL';

  return {
    title: announcement.title ?? '',
    targetAudience: AUDIENCE_OPTIONS.some((o) => o.value === audience) ? audience : 'ALL',
    content: announcement.content ?? '',
    academicYearId: announcement.academicYearId ?? activeAcademicYearId.value ?? undefined
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<AnnouncementFormValues>(
  {
    validationSchema: toTypedSchema(announcementSchema),
    initialValues: buildInitialValues(props.announcement)
  }
);

const [title, titleAttrs] = defineField('title');
const [targetAudience] = defineField('targetAudience');
const [content, contentAttrs] = defineField('content');

watch(
  () => [props.visible, props.announcement] as const,
  ([isOpen, announcement]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(announcement)
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
      // Normalize casing from backend RFC 7807 (e.g. Title -> title)
      const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
      mapped[normalizedKey] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    let saved: AnnouncementDto;
    if (isEditMode.value && props.announcement?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.announcement.id,
        payload: {
          title: values.title.trim(),
          content: values.content.trim(),
          targetAudience: values.targetAudience
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        title: values.title.trim(),
        content: values.content.trim(),
        targetAudience: values.targetAudience,
        academicYearId: values.academicYearId || activeAcademicYearId.value || undefined
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

    conflictBanner.value = 'Unable to save announcement. Please try again.';
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
    class="w-full sm:w-[580px]"
    data-testid="announcement-form-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="announcement-form" class="space-y-6" novalidate @submit.prevent="onSubmit">
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="announcement-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <span>{{ conflictBanner }}</span>
      </div>

      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-900">Announcement Details</h3>
          <StatusPill v-if="isEditMode && announcement?.status" :status="announcement.status" />
        </div>

        <FormField
          label="Title"
          field-id="announcement-title"
          required
          :error="errors.title"
          hint="Brief headline describing this announcement (max 255 characters)"
        >
          <InputText
            id="announcement-title"
            v-model="title"
            v-bind="titleAttrs"
            placeholder="e.g. End of Semester Examination Schedule"
            class="w-full"
            :invalid="Boolean(errors.title)"
            :disabled="isSubmitting"
            data-testid="announcement-title-input"
          />
        </FormField>

        <FormField
          label="Target Audience"
          field-id="announcement-audience"
          required
          :error="errors.targetAudience"
          hint="Select the intended recipients of this announcement"
        >
          <Select
            id="announcement-audience"
            v-model="targetAudience"
            :options="AUDIENCE_OPTIONS"
            option-label="label"
            option-value="value"
            placeholder="Select target audience"
            class="w-full"
            :invalid="Boolean(errors.targetAudience)"
            :disabled="isSubmitting"
            data-testid="announcement-audience-select"
          />
        </FormField>

        <FormField
          label="Message Content"
          field-id="announcement-content"
          required
          :error="errors.content"
          hint="Plain text formatted announcement bulletin message"
        >
          <Textarea
            id="announcement-content"
            v-model="content"
            v-bind="contentAttrs"
            rows="8"
            placeholder="Type your announcement content here. Plain text formatting, line breaks, and spacing will be preserved."
            class="w-full resize-y font-sans text-sm"
            :invalid="Boolean(errors.content)"
            :disabled="isSubmitting"
            data-testid="announcement-content-input"
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
          data-testid="cancel-announcement-button"
          @click="closeDrawer"
        />
        <Button
          type="submit"
          form="announcement-form"
          :label="isEditMode ? 'Save Changes' : 'Save Draft'"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="submit-announcement-button"
        />
      </div>
    </template>
  </Drawer>
</template>
