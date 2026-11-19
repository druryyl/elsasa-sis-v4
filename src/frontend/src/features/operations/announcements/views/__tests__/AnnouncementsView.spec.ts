import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import { configureAuthSessionReader } from '@/shared/services/authSession';
import type { AnnouncementDto } from '../../api/announcements.api';
import AnnouncementsView from '../AnnouncementsView.vue';

const server = setupServer();

const ANNOUNCEMENT_DRAFT_ID = '01900000-0000-7000-8000-000000000501';
const ANNOUNCEMENT_PUBLISHED_ID = '01900000-0000-7000-8000-000000000502';

function createSampleAnnouncements(): AnnouncementDto[] {
  return [
    {
      id: ANNOUNCEMENT_DRAFT_ID,
      academicYearId: '01900000-0000-7000-8000-000000000101',
      academicYearName: '2025/2026',
      title: 'Teacher Evaluation Guidelines (Draft)',
      content:
        'Important guidelines for teacher evaluations.\nPlease review carefully before the faculty meeting.',
      authorId: '01900000-0000-7000-8000-000000000001',
      authorName: 'Admin Staff',
      targetAudience: 'TEACHERS',
      status: 'DRAFT',
      publishedAt: null,
      createdAt: '2025-08-01T08:00:00Z',
      updatedAt: '2025-08-01T08:00:00Z'
    },
    {
      id: ANNOUNCEMENT_PUBLISHED_ID,
      academicYearId: '01900000-0000-7000-8000-000000000101',
      academicYearName: '2025/2026',
      title: 'Welcome Back Assembly 2025/2026',
      content: 'All students, parents, and staff are warmly invited to the opening assembly.',
      authorId: '01900000-0000-7000-8000-000000000001',
      authorName: 'Principal Office',
      targetAudience: 'ALL',
      status: 'PUBLISHED',
      publishedAt: '2025-08-05T09:00:00Z',
      createdAt: '2025-08-05T08:30:00Z',
      updatedAt: '2025-08-05T09:00:00Z'
    }
  ];
}

function configureAdminSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000000001',
      username: 'admin',
      displayName: 'Administrator',
      roles: ['SCHOOL_ADMIN'],
      permissions: [
        'Permissions.Operations.Announcements.View',
        'Permissions.Operations.Announcements.Create',
        'Permissions.Operations.Announcements.Publish'
      ]
    })
  });
}

function configureTeacherSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000000002',
      username: 'teacher.budi',
      displayName: 'Budi Santoso',
      roles: ['TEACHER'],
      permissions: ['Permissions.Operations.Announcements.View']
    })
  });
}

function configurePrincipalSession() {
  configureAuthSessionReader({
    getUser: () => ({
      id: '01900000-0000-7000-8000-000000000003',
      username: 'principal',
      displayName: 'School Principal',
      roles: ['PRINCIPAL'],
      permissions: ['Permissions.Operations.Announcements.View']
    })
  });
}

function mountAnnouncementsView() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(AnnouncementsView, {
    global: {
      plugins: [
        [VueQueryPlugin, { queryClient }],
        ToastService,
        [
          PrimeVue,
          {
            theme: {
              preset: Aura
            }
          }
        ]
      ],
      stubs: {
        Teleport: true
      }
    }
  });
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn()
    }))
  });
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
  configureAuthSessionReader(null);
});

afterAll(() => {
  server.close();
});

describe('AnnouncementsView.vue (SCR-OPR-ANNOUNCEMENTS)', () => {
  it('renders announcements list, author/admin toolbar, and audience filter pills', async () => {
    configureAdminSession();
    const items = createSampleAnnouncements();

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items,
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      )
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('School Announcements & Bulletin');
      expect(wrapper.text()).toContain('Teacher Evaluation Guidelines (Draft)');
      expect(wrapper.text()).toContain('Welcome Back Assembly 2025/2026');
    });

    // Check header create button and status filter for admin
    expect(wrapper.find('[data-testid="create-announcement-button"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="status-filter-container"]').exists()).toBe(true);

    // Audience filter pills present
    expect(wrapper.find('[data-testid="audience-pill-all"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="audience-pill-ALL"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="audience-pill-TEACHERS"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="audience-pill-STUDENTS"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="audience-pill-PARENTS"]').exists()).toBe(true);
  });

  it('filters by audience when clicking audience pills', async () => {
    configureAdminSession();
    let requestedAudience: string | null = null;

    server.use(
      http.get('/api/opr/announcements', ({ request }) => {
        const url = new URL(request.url);
        requestedAudience = url.searchParams.get('TargetAudience');
        return HttpResponse.json({
          items: [createSampleAnnouncements()[0]],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        });
      })
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Teacher Evaluation Guidelines (Draft)');
    });

    // Click 'TEACHERS' audience pill
    const teachersPill = wrapper.find('[data-testid="audience-pill-TEACHERS"]');
    await teachersPill.trigger('click');

    await vi.waitFor(() => {
      expect(requestedAudience).toBe('TEACHERS');
    });

    // Click 'All Audiences' pill
    const allPill = wrapper.find('[data-testid="audience-pill-all"]');
    await allPill.trigger('click');

    await vi.waitFor(() => {
      expect(requestedAudience).toBeNull();
    });
  });

  it('allows an administrator to compose and save a new draft announcement', async () => {
    configureAdminSession();
    const existing = createSampleAnnouncements();
    let postBody: Record<string, unknown> | null = null;

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items: existing,
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.post('/api/opr/announcements', async ({ request }) => {
        postBody = (await request.json()) as Record<string, unknown>;
        const created: AnnouncementDto = {
          id: '01900000-0000-7000-8000-000000000503',
          academicYearId: '01900000-0000-7000-8000-000000000101',
          academicYearName: '2025/2026',
          title: String(postBody['title']),
          content: String(postBody['content']),
          authorId: '01900000-0000-7000-8000-000000000001',
          authorName: 'Administrator',
          targetAudience: String(postBody['targetAudience']),
          status: 'DRAFT',
          publishedAt: null,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        };
        return HttpResponse.json(created, { status: 201 });
      })
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('School Announcements & Bulletin');
    });

    // Open create drawer
    const createBtn = wrapper.find('[data-testid="create-announcement-button"]');
    await createBtn.trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="announcement-form-drawer"]').exists()).toBe(true);
    });

    // Fill form
    const titleInput = wrapper.find('[data-testid="announcement-title-input"]');
    await titleInput.setValue('Parent-Teacher Conference Schedule');

    const contentInput = wrapper.find('[data-testid="announcement-content-input"]');
    await contentInput.setValue('Conferences will be held next Friday in the main hall.');

    // Submit form
    await wrapper.find('#announcement-form').trigger('submit');

    await vi.waitFor(() => {
      expect(postBody).not.toBeNull();
      expect(postBody?.title).toBe('Parent-Teacher Conference Schedule');
      expect(postBody?.content).toBe('Conferences will be held next Friday in the main hall.');
      expect(postBody?.targetAudience).toBe('ALL');
    });
  });

  it('publishes a draft announcement via ConfirmActionDialog (DRAFT -> PUBLISHED)', async () => {
    configureAdminSession();
    const items = createSampleAnnouncements();
    let publishedCalled = false;

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items,
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      ),
      http.post(`/api/opr/announcements/${ANNOUNCEMENT_DRAFT_ID}/publish`, () => {
        publishedCalled = true;
        const updated: AnnouncementDto = {
          ...items[0],
          status: 'PUBLISHED',
          publishedAt: '2025-08-15T10:00:00Z'
        };
        return HttpResponse.json(updated);
      })
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Teacher Evaluation Guidelines (Draft)');
    });

    // The draft row has row-publish-button
    const publishButton = wrapper.find('[data-testid="row-publish-button"]');
    expect(publishButton.exists()).toBe(true);
    await publishButton.trigger('click');

    // ConfirmActionDialog opens
    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Publish Announcement');
      expect(wrapper.text()).toContain('Teacher Evaluation Guidelines (Draft)');
    });

    // Click confirm in dialog
    const confirmBtn = wrapper
      .findAll('button')
      .find((b) => b.text().includes('Publish Announcement') && b.classes().includes('p-button'));
    expect(confirmBtn).toBeDefined();
    await confirmBtn?.trigger('click');

    await vi.waitFor(() => {
      expect(publishedCalled).toBe(true);
    });
  });

  it('renders read-only view for Teacher (hiding Create, Publish, Edit, and Status filters)', async () => {
    configureTeacherSession();
    const publishedOnly = [createSampleAnnouncements()[1]];

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items: publishedOnly,
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Welcome Back Assembly 2025/2026');
    });

    // New Announcement button is NOT shown
    expect(wrapper.find('[data-testid="create-announcement-button"]').exists()).toBe(false);

    // Status filter dropdown is NOT shown
    expect(wrapper.find('[data-testid="status-filter-container"]').exists()).toBe(false);

    // Row action buttons do NOT include edit or publish
    expect(wrapper.find('[data-testid="row-edit-button"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="row-publish-button"]').exists()).toBe(false);

    // View button is present
    expect(wrapper.find('[data-testid="row-view-button"]').exists()).toBe(true);
  });

  it('renders read-only view for Principal', async () => {
    configurePrincipalSession();
    const publishedOnly = [createSampleAnnouncements()[1]];

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items: publishedOnly,
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Welcome Back Assembly 2025/2026');
    });

    // Read-only controls for Principal
    expect(wrapper.find('[data-testid="create-announcement-button"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="status-filter-container"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="row-view-button"]').exists()).toBe(true);
  });

  it('opens detail dialog reader in plain-text format without executing HTML', async () => {
    configureTeacherSession();
    const announcements = [
      {
        ...createSampleAnnouncements()[1],
        content:
          'Notice:\nLine 1: No classes on Monday.\n<b>Bold HTML tag</b> <script>alert(1)</script>'
      }
    ];

    server.use(
      http.get('/api/opr/announcements', () =>
        HttpResponse.json({
          items: announcements,
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1
        })
      )
    );

    const wrapper = mountAnnouncementsView();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Welcome Back Assembly 2025/2026');
    });

    // Click title link to view detail
    const titleLink = wrapper.find('[data-testid="announcement-title-link"]');
    await titleLink.trigger('click');

    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="announcement-detail-dialog"]').exists()).toBe(true);
    });

    const contentBody = wrapper.find('[data-testid="dialog-content-body"]');
    expect(contentBody.exists()).toBe(true);
    // Verifies text is rendered as plain text content, NOT innerHTML
    expect(contentBody.text()).toContain('<b>Bold HTML tag</b> <script>alert(1)</script>');
    expect(contentBody.find('b').exists()).toBe(false);
    expect(contentBody.find('script').exists()).toBe(false);
  });
});
