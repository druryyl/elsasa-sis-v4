<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import { publishAuthSession } from '@/shared/services/authSession';
import { ApiError } from '@/shared/types/common';
import { authApi, mapAuthUserDtoToSession } from '../api/auth.api';
import { loginSchema, type LoginFormValues } from '../schemas/login.schema';

const route = useRoute();
const router = useRouter();

const authError = ref<string | null>(null);

const { defineField, handleSubmit, errors, isSubmitting, setErrors } = useForm<LoginFormValues>({
  validationSchema: toTypedSchema(loginSchema),
  initialValues: {
    username: '',
    password: ''
  }
});

const [username, usernameAttrs] = defineField('username');
const [password, passwordAttrs] = defineField('password');

function resolveRedirectTarget(roles: string[] = []): string {
  if (roles.includes('PARENT')) {
    return '/portal/parent';
  }
  if (roles.includes('STUDENT')) {
    return '/portal/student';
  }
  const redirectParam = route.query.redirect;
  if (
    typeof redirectParam === 'string' &&
    redirectParam.startsWith('/') &&
    !redirectParam.startsWith('//')
  ) {
    return redirectParam;
  }
  return '/operations/calendar';
}

const onSubmit = handleSubmit(async (values) => {
  authError.value = null;

  try {
    const response = await authApi.login({
      username: values.username,
      password: values.password
    });

    if (!response?.accessToken) {
      authError.value = 'Authentication response did not include an access token.';
      return;
    }

    const sessionUser = response.user
      ? mapAuthUserDtoToSession(response.user)
      : mapAuthUserDtoToSession(await authApi.me());

    publishAuthSession({
      accessToken: response.accessToken,
      user: sessionUser
    });

    await router.push(resolveRedirectTarget(sessionUser.roles));
  } catch (err) {
    if (err instanceof ApiError) {
      if (err.status === 401) {
        authError.value = 'Invalid username or password. Please check your credentials and try again.';
        return;
      }

      if (err.status === 422 && err.errors) {
        const fieldErrors: Partial<Record<keyof LoginFormValues, string>> = {};
        if (err.errors.username?.[0]) {
          fieldErrors.username = err.errors.username[0];
        }
        if (err.errors.password?.[0]) {
          fieldErrors.password = err.errors.password[0];
        }
        setErrors(fieldErrors);
        authError.value = err.detail || 'Please correct the highlighted fields.';
        return;
      }

      authError.value = err.detail || 'Unable to sign in. Please try again.';
      return;
    }

    authError.value = 'An unexpected error occurred while signing in. Please try again.';
  }
});
</script>

<template>
  <div class="space-y-6">
    <div class="space-y-1">
      <h1 class="text-xl font-semibold tracking-tight text-slate-900">Sign in to Elsasa SIS</h1>
      <p class="text-sm text-slate-500">
        Enter your school account credentials to access the workspace.
      </p>
    </div>

    <div
      v-if="authError"
      role="alert"
      data-testid="login-error-banner"
      class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-700"
    >
      <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" aria-hidden="true" />
      <span>{{ authError }}</span>
    </div>

    <form class="space-y-4" novalidate @submit.prevent="onSubmit">
      <FormField label="Username" field-id="username" required :error="errors.username">
        <InputText
          id="username"
          v-model="username"
          v-bind="usernameAttrs"
          type="text"
          autocomplete="username"
          placeholder="Enter your username"
          :invalid="Boolean(errors.username)"
          class="w-full"
        />
      </FormField>

      <FormField label="Password" field-id="password" required :error="errors.password">
        <Password
          v-model="password"
          v-bind="passwordAttrs"
          input-id="password"
          :feedback="false"
          toggle-mask
          autocomplete="current-password"
          placeholder="Enter your password"
          :invalid="Boolean(errors.password)"
          class="w-full"
          input-class="w-full"
        />
      </FormField>

      <Button
        type="submit"
        label="Sign In"
        :loading="isSubmitting"
        :disabled="isSubmitting"
        class="w-full"
      />
    </form>
  </div>
</template>
