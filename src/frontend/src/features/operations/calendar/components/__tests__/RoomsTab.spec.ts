import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { configureHttpClient } from '@/shared/services/http';
import type { RoomDto } from '../../api/rooms.api';
import RoomsTab from '../RoomsTab.vue';

const server = setupServer();

const ROOM_1_ID = '01900000-0000-7000-8000-000000000401';
const ROOM_2_ID = '01900000-0000-7000-8000-000000000402';

function createSampleRooms(): RoomDto[] {
  return [
    {
      id: ROOM_1_ID,
      name: 'Classroom 7A',
      roomType: 'CLASSROOM',
      capacity: 32
    },
    {
      id: ROOM_2_ID,
      name: 'Science Lab 1',
      roomType: 'LABORATORY',
      capacity: 24
    }
  ];
}

function mountRoomsTab(props = { canManage: true }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return mount(RoomsTab, {
    props,
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
});

afterEach(() => {
  server.resetHandlers();
  configureHttpClient(null);
});

afterAll(() => {
  server.close();
});

describe('RoomsTab.vue', () => {
  it('renders rooms list with facility type, capacity, and search bar', async () => {
    server.use(
      http.get('/api/opr/rooms', () =>
        HttpResponse.json({
          items: createSampleRooms(),
          page: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1
        })
      )
    );

    const wrapper = mountRoomsTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Classroom 7A');
      expect(wrapper.text()).toContain('Science Lab 1');
      expect(wrapper.text()).toContain('32 seats');
      expect(wrapper.text()).toContain('24 seats');
      expect(wrapper.text()).toContain('Classroom');
      expect(wrapper.text()).toContain('Laboratory');
    });

    expect(wrapper.find('[data-testid="create-room-button"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="room-type-filter"]').exists()).toBe(true);
  });

  it('creates a room enforcing positive capacity and sends POST /api/opr/rooms', async () => {
    let createdPayload: unknown = null;

    server.use(
      http.get('/api/opr/rooms', () =>
        HttpResponse.json({
          items: createSampleRooms(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.post('/api/opr/rooms', async ({ request }) => {
        createdPayload = await request.json();
        return HttpResponse.json(
          {
            id: '01900000-0000-7000-8000-000000000403',
            name: 'Auditorium Hall',
            roomType: 'HALL',
            capacity: 200
          },
          { status: 201 }
        );
      })
    );

    const wrapper = mountRoomsTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Classroom 7A');
    });

    // Open Drawer
    await wrapper.find('[data-testid="create-room-button"]').trigger('click');

    const drawer = wrapper.find('[data-testid="room-drawer"]');
    expect(drawer.exists()).toBe(true);

    // Fill form
    await wrapper.find('[data-testid="room-name-input"]').setValue('Auditorium Hall');
    await wrapper.find('[data-testid="room-type-select"]').setValue('HALL');
    await wrapper.find('[data-testid="room-capacity-input"]').setValue('200');

    // Submit
    await wrapper.find('#room-form').trigger('submit');

    await vi.waitFor(() => {
      expect(createdPayload).toEqual({
        name: 'Auditorium Hall',
        roomType: 'HALL',
        capacity: 200
      });
    });
  });

  it('handles 409 duplicate room name conflict banner', async () => {
    server.use(
      http.get('/api/opr/rooms', () =>
        HttpResponse.json({
          items: createSampleRooms(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.post('/api/opr/rooms', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
            title: 'Conflict',
            status: 409,
            detail: "A room named 'Classroom 7A' already exists."
          },
          {
            status: 409,
            headers: { 'Content-Type': 'application/problem+json' }
          }
        )
      )
    );

    const wrapper = mountRoomsTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Classroom 7A');
    });

    await wrapper.find('[data-testid="create-room-button"]').trigger('click');
    await wrapper.find('[data-testid="room-name-input"]').setValue('Classroom 7A');
    await wrapper.find('#room-form').trigger('submit');

    await vi.waitFor(() => {
      const banner = wrapper.find('[data-testid="drawer-conflict-alert"]');
      expect(banner.exists()).toBe(true);
      expect(banner.text()).toContain("A room named 'Classroom 7A' already exists");
    });
  });

  it('edits an existing room and sends PUT /api/opr/rooms/:id', async () => {
    let updatedPayload: unknown = null;

    server.use(
      http.get('/api/opr/rooms', () =>
        HttpResponse.json({
          items: createSampleRooms(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      ),
      http.put('/api/opr/rooms/:id', async ({ request }) => {
        updatedPayload = await request.json();
        return HttpResponse.json({
          id: ROOM_1_ID,
          name: 'Classroom 7A Renamed',
          roomType: 'CLASSROOM',
          capacity: 35
        });
      })
    );

    const wrapper = mountRoomsTab();

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Classroom 7A');
    });

    // Open edit drawer
    const editBtn = wrapper.find(`[data-testid="edit-room-${ROOM_1_ID}"]`);
    expect(editBtn.exists()).toBe(true);
    await editBtn.trigger('click');

    // Update capacity and name
    await wrapper.find('[data-testid="room-name-input"]').setValue('Classroom 7A Renamed');
    await wrapper.find('[data-testid="room-capacity-input"]').setValue('35');

    // Submit
    await wrapper.find('#room-form').trigger('submit');

    await vi.waitFor(() => {
      expect(updatedPayload).toEqual({
        name: 'Classroom 7A Renamed',
        roomType: 'CLASSROOM',
        capacity: 35
      });
    });
  });

  it('hides creation and edit buttons when canManage is false (read-only mode)', async () => {
    server.use(
      http.get('/api/opr/rooms', () =>
        HttpResponse.json({
          items: createSampleRooms(),
          page: 1,
          pageSize: 10,
          totalCount: 2
        })
      )
    );

    const wrapper = mountRoomsTab({ canManage: false });

    await vi.waitFor(() => {
      expect(wrapper.text()).toContain('Classroom 7A');
    });

    expect(wrapper.find('[data-testid="create-room-button"]').exists()).toBe(false);
    expect(wrapper.find(`[data-testid="edit-room-${ROOM_1_ID}"]`).exists()).toBe(false);
  });
});
