import type { RouteRecordRaw } from 'vue-router';
import { getActivePinia } from 'pinia';
import { useAuthStore } from '@/app/stores/auth.store';

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean;
    guestOnly?: boolean;
    permission?: string | string[];
    title?: string;
    layout?: string;
  }
}

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: () => {
      if (getActivePinia()) {
        const authStore = useAuthStore();
        if (authStore.hasAnyRole(['PARENT'])) {
          return '/portal/parent';
        }
        if (authStore.hasAnyRole(['STUDENT'])) {
          return '/portal/student';
        }
      }
      return '/operations/calendar';
    }
  },
  {
    path: '/login',
    redirect: '/auth/login'
  },
  {
    path: '/teaching/assignments',
    redirect: '/teachers/assignments'
  },
  {
    path: '/portal',
    component: () => import('@/app/layouts/PortalLayout.vue'),
    meta: { requiresAuth: true, layout: 'portal' },
    children: [
      {
        path: '',
        redirect: () => {
          if (getActivePinia()) {
            const authStore = useAuthStore();
            if (authStore.hasAnyRole(['STUDENT'])) {
              return '/portal/student';
            }
          }
          return '/portal/parent';
        }
      },
      {
        path: 'parent',
        name: 'portal.parent',
        component: () => import('@/features/portal-parent/views/ParentPortalView.vue'),
        meta: {
          requiresAuth: true,
          permission: 'Permissions.Students.Children.View',
          layout: 'portal',
          title: 'Portal Orang Tua'
        }
      },
      {
        path: 'student',
        name: 'portal.student',
        component: () => import('@/features/portal-student/views/StudentPortalView.vue'),
        meta: {
          requiresAuth: true,
          permission: 'Permissions.Operations.Timetables.ViewOwn',
          layout: 'portal',
          title: 'Portal Siswa'
        }
      }
    ]
  },
  {
    path: '/auth',
    component: () => import('@/app/layouts/AuthLayout.vue'),
    meta: { requiresAuth: false, guestOnly: true },
    children: [
      {
        path: 'login',
        name: 'auth.login',
        component: () => import('@/features/auth/views/LoginView.vue'),
        meta: {
          requiresAuth: false,
          guestOnly: true,
          title: 'Sign In'
        }
      }
    ]
  },
  {
    path: '/',
    component: () => import('@/app/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'operations',
        children: [
          {
            path: 'calendar',
            name: 'operations.calendar',
            component: () =>
              import('@/features/operations/calendar/views/CalendarWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Operations.Calendar.View',
                'Permissions.Operations.Calendar.Manage'
              ],
              title: 'School Calendar & Academic Cycle'
            }
          },
          {
            path: 'announcements',
            name: 'operations.announcements',
            component: () =>
              import('@/features/operations/announcements/views/AnnouncementsView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Operations.Announcements.View',
                'Permissions.Operations.Announcements.Create',
                'Permissions.Operations.Announcements.Publish'
              ],
              title: 'School Announcements & Bulletin'
            }
          },
          {
            path: 'timetables',
            name: 'operations.timetables',
            component: () =>
              import('@/features/operations/scheduling/views/TimetableWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Operations.Timetables.View',
                'Permissions.Operations.Timetables.Manage',
                'Permissions.Operations.Timetables.ViewOwn'
              ],
              title: 'Timetables & Scheduling'
            }
          }
        ]
      },
      {
        path: 'academic',
        children: [
          {
            path: 'curriculum',
            name: 'academic.curriculum',
            component: () =>
              import('@/features/academic/curriculum/views/CurriculumSetupView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Academic.Curriculum.View',
                'Permissions.Academic.Curriculum.Manage'
              ],
              title: 'Curriculum & Learning Structure'
            }
          },
          {
            path: 'attendance',
            name: 'academic.attendance',
            component: () =>
              import('@/features/academic/attendance/views/AttendanceWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Academic.Attendance.View',
                'Permissions.Academic.Attendance.Record'
              ],
              title: 'Classroom Attendance'
            }
          },
          {
            path: 'scorebook',
            name: 'academic.scorebook',
            component: () =>
              import('@/features/academic/scorebook/views/ScorebookWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Academic.Scorebook.View',
                'Permissions.Academic.Scorebook.Manage'
              ],
              title: 'Teacher Scorebook'
            }
          },
          {
            path: 'report-cards',
            name: 'academic.report-cards',
            component: () =>
              import('@/features/academic/report-cards/views/ReportCardsWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Academic.ReportCards.View',
                'Permissions.Academic.ReportCards.Generate',
                'Permissions.Academic.ReportCards.ProvideNotes',
                'Permissions.Academic.ReportCards.Remarks',
                'Permissions.Academic.ReportCards.Finalize'
              ],
              title: 'Semester Report Cards'
            }
          },
          {
            path: 'monitoring',
            name: 'academic.monitoring',
            component: () =>
              import('@/features/academic/monitoring/views/AcademicMonitoringView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'Permissions.Academic.Monitoring.View',
              layout: 'app',
              title: 'Monitoring Progres Akademik'
            }
          }
        ]
      },
      {
        path: 'admissions',
        children: [
          {
            path: 'queue',
            name: 'admissions.queue',
            component: () => import('@/features/student/admissions/views/AdmissionsQueueView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Students.Admissions.View',
                'Permissions.Students.Admissions.Register',
                'Permissions.Students.Admissions.Decide',
                'Permissions.Students.Admissions.Approve',
                'Permissions.Students.Admissions.Enroll'
              ],
              title: 'Admissions Processing Queue'
            }
          }
        ]
      },
      {
        path: 'students',
        children: [
          {
            path: 'directory',
            name: 'students.directory',
            component: () => import('@/features/student/directory/views/StudentDirectoryView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Students.Directory.View',
                'Permissions.Students.Directory.Manage',
                'Permissions.Students.Roster.ViewOwn'
              ],
              title: 'Student Directory & Placements'
            }
          }
        ]
      },
      {
        path: 'teachers',
        children: [
          {
            path: 'assignments',
            name: 'teachers.assignments',
            component: () =>
              import('@/features/teaching/assignments/views/TeacherAssignmentsView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Teaching.Assignments.View',
                'Permissions.Teaching.Assignments.Manage',
                'Permissions.Teaching.Assignments.Oversight',
                'Permissions.Teaching.Directory.Manage'
              ],
              title: 'Teachers & Assignments'
            }
          }
        ]
      },
      {
        path: 'development',
        children: [
          {
            path: 'pastoral',
            name: 'development.pastoral',
            component: () =>
              import('@/features/development/pastoral/views/PastoralDossierView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Development.Behaviour.Record',
                'Permissions.Development.Discipline.Record',
                'Permissions.Development.Discipline.Escalate',
                'Permissions.Development.Achievements.Record',
                'Permissions.Development.Dossier.View'
              ],
              title: 'Pastoral & Discipline'
            }
          },
          {
            path: 'extracurricular',
            name: 'development.extracurricular',
            component: () =>
              import(
                '@/features/development/extracurricular/views/ExtracurricularWorkbenchView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Development.Extracurricular.Manage',
                'Permissions.Development.Extracurricular.Log'
              ],
              title: 'Extracurricular Clubs'
            }
          }
        ]
      },
      {
        path: 'finance',
        children: [
          {
            path: 'fee-policy',
            name: 'finance.fee-policy',
            component: () =>
              import('@/features/finance/fee-policy/views/FeesPolicyView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Finance.FeePolicy.Manage',
                'Permissions.Finance.Discounts.Manage'
              ],
              title: 'Fee Policies & Discounts'
            }
          },
          {
            path: 'billing',
            name: 'finance.billing',
            component: () =>
              import('@/features/finance/billing/views/BillingWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Finance.Billing.Generate',
                'Permissions.Finance.Billing.ManageAdHoc',
                'Permissions.Finance.Billing.Cancel'
              ],
              title: 'Tuition Billing'
            }
          },
          {
            path: 'payments',
            name: 'finance.payments',
            component: () =>
              import('@/features/finance/payments/views/PaymentsWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: [
                'Permissions.Finance.Payments.Record',
                'Permissions.Finance.Deposits.Allocate',
                'Permissions.Finance.Statements.View'
              ],
              title: 'Cashier Counter'
            }
          },
          {
            path: 'receivables',
            name: 'finance.receivables',
            component: () =>
              import(
                '@/features/finance/receivables/views/ReceivablesAgingView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: ['Permissions.Finance.Reports.View'],
              title: 'Receivables & Arrears'
            }
          }
        ]
      }
    ]
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/shared/views/ForbiddenView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Access Forbidden'
    }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/shared/views/NotFoundView.vue'),
    meta: {
      requiresAuth: false,
      title: 'Page Not Found'
    }
  }
];
