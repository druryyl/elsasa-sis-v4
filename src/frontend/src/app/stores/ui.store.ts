import { ref } from 'vue';
import { defineStore } from 'pinia';

export interface BreadcrumbItem {
  label: string;
  to?: string | { name: string };
}

export const useUIStore = defineStore('ui', () => {
  const isSidebarCollapsed = ref(false);
  const breadcrumbs = ref<BreadcrumbItem[]>([]);

  function toggleSidebar(): void {
    isSidebarCollapsed.value = !isSidebarCollapsed.value;
  }

  function setSidebarCollapsed(collapsed: boolean): void {
    isSidebarCollapsed.value = collapsed;
  }

  function setBreadcrumbs(items: BreadcrumbItem[]): void {
    breadcrumbs.value = items;
  }

  function clearBreadcrumbs(): void {
    breadcrumbs.value = [];
  }

  return {
    isSidebarCollapsed,
    breadcrumbs,
    toggleSidebar,
    setSidebarCollapsed,
    setBreadcrumbs,
    clearBreadcrumbs
  };
});
