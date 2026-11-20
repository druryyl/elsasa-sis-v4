/* eslint-disable vue/one-component-per-file */
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';
import { defineComponent } from 'vue';
import { useAuthStore } from '@/app/stores/auth.store';
import PortalLayout from '../PortalLayout.vue';

const DummyChild = defineComponent({
  template: '<div data-testid="dummy-child">Child Content</div>'
});

const DummyLogin = defineComponent({
  template: '<div data-testid="login-view">Login</div>'
});

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/portal',
        component: PortalLayout,
        children: [
          {
            path: 'test',
            name: 'portal.test',
            component: DummyChild
          }
        ]
      },
      {
        path: '/login',
        name: 'login',
        component: DummyLogin
      }
    ]
  });
}

describe('PortalLayout.vue', () => {
  let pinia: ReturnType<typeof createPinia>;

  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);
  });

  it('renders application brand logo and title "ELSASA-SIS"', async () => {
    const router = createTestRouter();
    await router.push('/portal/test');
    await router.isReady();

    const wrapper = mount(PortalLayout, {
      global: {
        plugins: [pinia, router]
      }
    });

    expect(wrapper.text()).toContain('ELSASA-SIS');
  });

  it('renders role badge "Portal Orang Tua" when authenticated as PARENT', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-parent',
      user: {
        id: 'u-parent',
        username: 'parent.hendra',
        displayName: 'Hendra Dahlan',
        roles: ['PARENT'],
        permissions: ['Permissions.Students.Children.View']
      }
    });

    const router = createTestRouter();
    await router.push('/portal/test');
    await router.isReady();

    const wrapper = mount(PortalLayout, {
      global: {
        plugins: [pinia, router]
      }
    });

    const badge = wrapper.find('[data-testid="portal-role-badge"]');
    expect(badge.exists()).toBe(true);
    expect(badge.text()).toBe('Portal Orang Tua');
    expect(wrapper.find('[data-testid="portal-user-name"]').text()).toBe('Hendra Dahlan');
  });

  it('renders role badge "Portal Siswa" when authenticated as STUDENT', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-student',
      user: {
        id: 'u-student',
        username: 'student.ahmad',
        displayName: 'Ahmad Dahlan',
        roles: ['STUDENT'],
        permissions: ['Permissions.Operations.Timetables.ViewOwn']
      }
    });

    const router = createTestRouter();
    await router.push('/portal/test');
    await router.isReady();

    const wrapper = mount(PortalLayout, {
      global: {
        plugins: [pinia, router]
      }
    });

    const badge = wrapper.find('[data-testid="portal-role-badge"]');
    expect(badge.exists()).toBe(true);
    expect(badge.text()).toBe('Portal Siswa');
    expect(wrapper.find('[data-testid="portal-user-name"]').text()).toBe('Ahmad Dahlan');
  });

  it('signs out and redirects to /login when clicking sign-out button', async () => {
    const authStore = useAuthStore();
    authStore.setSession({
      accessToken: 'token-parent',
      user: {
        id: 'u-parent',
        username: 'parent.hendra',
        displayName: 'Hendra Dahlan',
        roles: ['PARENT'],
        permissions: []
      }
    });

    const router = createTestRouter();
    await router.push('/portal/test');
    await router.isReady();

    const pushSpy = vi.spyOn(router, 'push');
    const logoutSpy = vi.spyOn(authStore, 'logout').mockResolvedValue(undefined);

    const wrapper = mount(PortalLayout, {
      global: {
        plugins: [pinia, router]
      }
    });

    const logoutBtn = wrapper.find('[data-testid="portal-logout-button"]');
    expect(logoutBtn.exists()).toBe(true);
    await logoutBtn.trigger('click');

    expect(logoutSpy).toHaveBeenCalled();
    expect(pushSpy).toHaveBeenCalledWith('/login');
  });

  it('renders child router-view / slot inside centered max-w-5xl container', async () => {
    const router = createTestRouter();
    await router.push('/portal/test');
    await router.isReady();

    const wrapper = mount(PortalLayout, {
      global: {
        plugins: [pinia, router]
      }
    });

    const mainContainer = wrapper.find('main');
    expect(mainContainer.classes()).toContain('max-w-5xl');
    expect(mainContainer.classes()).toContain('mx-auto');
    expect(wrapper.find('[data-testid="dummy-child"]').exists()).toBe(true);
  });
});
