import { Component, ChangeDetectorRef, HostListener, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

type Downtime = {
  id: string;
  environment: string;
  durationMinutes: number;
  customers: string;
  reason: string;
  occurredAt: string;
};

type CreateDowntime = Omit<Downtime, 'id'>;

type UiModel = {
  environment: string;
  durationMinutes: number;
  occurredAt: string;
  customersSelected: string[];
  reasonsSelected: string[];
};

type SortField = 'occurredAt' | 'environment' | 'durationMinutes' | 'customers' | 'reason';
type SortDir = 'asc' | 'desc';

type ToastType = 'success' | 'error' | 'info';
type ToastState = { show: boolean; message: string; type: ToastType };

@Component({
  selector: 'app-incidents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './incidents.html',
  styleUrl: './incidents.scss',
})
export class IncidentsComponent {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  private readonly customersStorageKey = 'sla-customers-suggestions';
  private readonly reasonsStorageKey = 'sla-reasons-suggestions';

  environments = ['Eclit', 'Paris', 'Huawei', 'Ohio', 'UAE', 'Preprod Ireland'];
  customersSuggestions = ['Arçelik', 'Vestel', 'THY', 'aws', 'Genesys'];
  reasonSuggestions = ['IVR Client', 'Deployment', 'Network', 'DB latency', 'Timeout'];

  downtimes: Downtime[] = [];
  filtered: Downtime[] = [];

  loading = false;
  error = '';
  searchText = '';

  savingCreate = false;
  savingEdit = false;
  deletingId: string | null = null;
  editingId: string | null = null;

  customersOpenCreate = false;
  reasonsOpenCreate = false;
  customersOpenEdit = false;
  reasonsOpenEdit = false;

  newCustomerCreate = '';
  newReasonCreate = '';
  newCustomerEdit = '';
  newReasonEdit = '';

  sortField: SortField = 'occurredAt';
  sortDir: SortDir = 'desc';

  sortFields: { value: SortField; label: string }[] = [
    { value: 'occurredAt', label: 'Tarih' },
    { value: 'environment', label: 'Environment' },
    { value: 'durationMinutes', label: 'Süre (dk)' },
    { value: 'customers', label: 'Müşteri' },
    { value: 'reason', label: 'Reason' },
  ];

  toast: ToastState = { show: false, message: '', type: 'info' };
  private toastTimer: any = null;

  createValid = false;

  createModel: UiModel = {
    environment: '',
    durationMinutes: 5,
    occurredAt: this.toDatetimeLocal(new Date()),
    customersSelected: [],
    reasonsSelected: [],
  };

  editModel: UiModel = {
    environment: '',
    durationMinutes: 0,
    occurredAt: '',
    customersSelected: [],
    reasonsSelected: [],
  };

  ngOnInit() {
    this.loadSuggestionsFromStorage();
    this.refresh();
    this.updateCreateValid();
  }

  @HostListener('document:click')
  closePanels() {
    this.customersOpenCreate = false;
    this.reasonsOpenCreate = false;
    this.customersOpenEdit = false;
    this.reasonsOpenEdit = false;
  }

  onFormChange() {
    this.updateCreateValid();
    this.cdr.detectChanges();
  }

  private updateCreateValid() {
    this.createValid =
      !!this.createModel.environment &&
      !!this.createModel.occurredAt &&
      Number(this.createModel.durationMinutes) >= 1;
  }

  private loadSuggestionsFromStorage() {
    const savedCustomers = localStorage.getItem(this.customersStorageKey);
    const savedReasons = localStorage.getItem(this.reasonsStorageKey);

    if (savedCustomers) {
      try {
        const parsed = JSON.parse(savedCustomers);
        if (Array.isArray(parsed)) this.customersSuggestions = parsed;
      } catch {}
    }

    if (savedReasons) {
      try {
        const parsed = JSON.parse(savedReasons);
        if (Array.isArray(parsed)) this.reasonSuggestions = parsed;
      } catch {}
    }
  }

  private saveSuggestionsToStorage() {
    localStorage.setItem(this.customersStorageKey, JSON.stringify(this.customersSuggestions));
    localStorage.setItem(this.reasonsStorageKey, JSON.stringify(this.reasonSuggestions));
  }

  private parseApiDate(value: string | Date): Date {
    if (value instanceof Date) return value;

    const raw = (value || '').trim();
    if (!raw) return new Date('');

    const normalized = raw.replace(' ', 'T');
    const hasTimezone = /(?:Z|[+\-]\d{2}:\d{2})$/.test(normalized);

    return new Date(hasTimezone ? normalized : `${normalized}Z`);
  }

  private toDatetimeLocal(value: string | Date): string {
    const date = this.parseApiDate(value);
    if (Number.isNaN(date.getTime())) return '';

    const pad = (n: number) => String(n).padStart(2, '0');

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  formatLocalDate(value: string | Date): string {
    const date = this.parseApiDate(value);
    if (Number.isNaN(date.getTime())) return '';

    return date.toLocaleString(undefined, {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  }

  private showToast(message: string, type: ToastType = 'info', ms = 2400) {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toast = { show: true, message, type };
    this.cdr.detectChanges();

    this.toastTimer = setTimeout(() => {
      this.toast.show = false;
      this.cdr.detectChanges();
    }, ms);
  }

  isSelected(arr: string[] | undefined, item: string) {
    return !!arr?.includes(item);
  }

  toggleItem(arr: string[] | undefined, item: string) {
    if (!arr) return;
    const i = arr.indexOf(item);
    if (i >= 0) arr.splice(i, 1);
    else arr.push(item);

    this.updateCreateValid();
    this.cdr.detectChanges();
  }

  summaryText(arr: string[]) {
    if (!arr?.length) return 'Seçiniz...';
    if (arr.length === 1) return arr[0];
    return `${arr[0]} +${arr.length - 1}`;
  }

  private joinMulti(arr: string[]) {
    return (arr || []).join('; ');
  }

  private parseMulti(text: string) {
    if (!text) return [];
    return text
      .split(/;|,/g)
      .map((x) => x.trim())
      .filter(Boolean);
  }

  private normalizeLabel(v: string) {
    return (v || '').trim().replace(/\s+/g, ' ');
  }

  deleteCustomerSuggestion(value: string) {
    this.customersSuggestions = this.customersSuggestions.filter((x) => x !== value);
    this.createModel.customersSelected = this.createModel.customersSelected.filter((x) => x !== value);
    this.editModel.customersSelected = this.editModel.customersSelected.filter((x) => x !== value);

    this.saveSuggestionsToStorage();
    this.showToast('Müşteri listeden silindi ✅', 'success');
    this.cdr.detectChanges();
  }

  deleteReasonSuggestion(value: string) {
    this.reasonSuggestions = this.reasonSuggestions.filter((x) => x !== value);
    this.createModel.reasonsSelected = this.createModel.reasonsSelected.filter((x) => x !== value);
    this.editModel.reasonsSelected = this.editModel.reasonsSelected.filter((x) => x !== value);

    this.saveSuggestionsToStorage();
    this.showToast('Reason listeden silindi ✅', 'success');
    this.cdr.detectChanges();
  }

  addCustomerCreate() {
    const v = this.normalizeLabel(this.newCustomerCreate);
    if (!v) return;

    if (!this.customersSuggestions.some((x) => x.toLowerCase() === v.toLowerCase())) {
      this.customersSuggestions = [v, ...this.customersSuggestions];
    }
    if (!this.createModel.customersSelected.includes(v)) {
      this.createModel.customersSelected = [...this.createModel.customersSelected, v];
    }

    this.newCustomerCreate = '';
    this.saveSuggestionsToStorage();
    this.updateCreateValid();
    this.cdr.detectChanges();
  }

  addReasonCreate() {
    const v = this.normalizeLabel(this.newReasonCreate);
    if (!v) return;

    if (!this.reasonSuggestions.some((x) => x.toLowerCase() === v.toLowerCase())) {
      this.reasonSuggestions = [v, ...this.reasonSuggestions];
    }
    if (!this.createModel.reasonsSelected.includes(v)) {
      this.createModel.reasonsSelected = [...this.createModel.reasonsSelected, v];
    }

    this.newReasonCreate = '';
    this.saveSuggestionsToStorage();
    this.updateCreateValid();
    this.cdr.detectChanges();
  }

  addCustomerEdit() {
    const v = this.normalizeLabel(this.newCustomerEdit);
    if (!v) return;

    if (!this.customersSuggestions.some((x) => x.toLowerCase() === v.toLowerCase())) {
      this.customersSuggestions = [v, ...this.customersSuggestions];
    }
    if (!this.editModel.customersSelected.includes(v)) {
      this.editModel.customersSelected = [...this.editModel.customersSelected, v];
    }

    this.newCustomerEdit = '';
    this.saveSuggestionsToStorage();
    this.cdr.detectChanges();
  }

  addReasonEdit() {
    const v = this.normalizeLabel(this.newReasonEdit);
    if (!v) return;

    if (!this.reasonSuggestions.some((x) => x.toLowerCase() === v.toLowerCase())) {
      this.reasonSuggestions = [v, ...this.reasonSuggestions];
    }
    if (!this.editModel.reasonsSelected.includes(v)) {
      this.editModel.reasonsSelected = [...this.editModel.reasonsSelected, v];
    }

    this.newReasonEdit = '';
    this.saveSuggestionsToStorage();
    this.cdr.detectChanges();
  }

  onSortChange() {
    this.applyFilter();
    this.cdr.detectChanges();
  }

  isSorted(field: SortField) {
    return this.sortField === field;
  }

  setSort(field: SortField) {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = field === 'occurredAt' ? 'desc' : 'asc';
    }
    this.applyFilter();
    this.cdr.detectChanges();
  }

  private applySorting(rows: Downtime[]) {
    const dir = this.sortDir === 'asc' ? 1 : -1;
    const f = this.sortField;

    const getStr = (v: any) => (v ?? '').toString().toLowerCase();
    const getNum = (v: any) => Number(v ?? 0);
    const getDate = (v: any) => {
      const d = this.parseApiDate(v);
      const t = d.getTime();
      return Number.isNaN(t) ? 0 : t;
    };

    return [...rows].sort((a, b) => {
      if (f === 'durationMinutes') return (getNum(a.durationMinutes) - getNum(b.durationMinutes)) * dir;
      if (f === 'occurredAt') return (getDate(a.occurredAt) - getDate(b.occurredAt)) * dir;

      const av =
        f === 'environment' ? getStr(a.environment) : f === 'customers' ? getStr(a.customers) : getStr(a.reason);
      const bv =
        f === 'environment' ? getStr(b.environment) : f === 'customers' ? getStr(b.customers) : getStr(b.reason);

      if (av < bv) return -1 * dir;
      if (av > bv) return 1 * dir;
      return 0;
    });
  }

  refresh() {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    this.http.get<Downtime[]>('/api/Downtimes').subscribe({
      next: (rows) => {
        this.downtimes = rows ?? [];
        this.applyFilter();
        this.loading = false;
        this.updateCreateValid();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'Kesinti kayıtları alınamadı. API çalışıyor mu?';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  applyFilter() {
    const q = (this.searchText || '').trim().toLowerCase();
    let base = [...this.downtimes];

    if (q) {
      base = base.filter((x) => {
        return (
          (x.environment || '').toLowerCase().includes(q) ||
          (x.customers || '').toLowerCase().includes(q) ||
          (x.reason || '').toLowerCase().includes(q)
        );
      });
    }

    this.filtered = this.applySorting(base);
  }

  cancelCreate() {
    this.createModel = {
      environment: '',
      durationMinutes: 5,
      occurredAt: this.toDatetimeLocal(new Date()),
      customersSelected: [],
      reasonsSelected: [],
    };
    this.newCustomerCreate = '';
    this.newReasonCreate = '';
    this.updateCreateValid();
    this.cdr.detectChanges();
  }

  createDowntime() {
    if (!this.createValid || this.savingCreate) return;

    this.savingCreate = true;
    this.error = '';
    this.cdr.detectChanges();

    const payload: CreateDowntime = {
      environment: this.createModel.environment,
      durationMinutes: Number(this.createModel.durationMinutes),
      occurredAt: new Date(this.createModel.occurredAt).toISOString(),
      customers: this.joinMulti(this.createModel.customersSelected),
      reason: this.joinMulti(this.createModel.reasonsSelected),
    };

    this.http
      .post('/api/Downtimes', payload, { observe: 'response', responseType: 'text' })
      .subscribe({
        next: () => {
          this.savingCreate = false;
          this.cancelCreate();
          this.showToast('Kayıt eklendi ✅', 'success');
          this.refresh();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.error = 'Kayıt eklenemedi.';
          this.savingCreate = false;
          this.showToast('Kayıt eklenemedi ❌', 'error');
          this.cdr.detectChanges();
        },
      });
  }

  startEdit(r: Downtime) {
    if (this.deletingId || this.savingEdit) return;

    this.editingId = r.id;
    this.error = '';

    this.editModel = {
      environment: r.environment,
      durationMinutes: r.durationMinutes,
      occurredAt: this.toDatetimeLocal(r.occurredAt),
      customersSelected: this.parseMulti(r.customers),
      reasonsSelected: this.parseMulti(r.reason),
    };

    this.newCustomerEdit = '';
    this.newReasonEdit = '';
    this.cdr.detectChanges();
  }

  cancelEdit() {
    this.editingId = null;
    this.customersOpenEdit = false;
    this.reasonsOpenEdit = false;
    this.newCustomerEdit = '';
    this.newReasonEdit = '';
    this.cdr.detectChanges();
  }

  saveEdit(id: string) {
    if (!id || this.savingEdit) return;

    if (!this.editModel.environment) {
      this.error = 'Environment seçmelisin.';
      this.cdr.detectChanges();
      return;
    }

    if (!this.editModel.durationMinutes || this.editModel.durationMinutes < 1) {
      this.error = 'Süre (dk) en az 1 olmalı.';
      this.cdr.detectChanges();
      return;
    }

    this.savingEdit = true;
    this.error = '';
    this.cdr.detectChanges();

    const payload: CreateDowntime = {
      environment: this.editModel.environment,
      durationMinutes: Number(this.editModel.durationMinutes),
      occurredAt: new Date(this.editModel.occurredAt).toISOString(),
      customers: this.joinMulti(this.editModel.customersSelected),
      reason: this.joinMulti(this.editModel.reasonsSelected),
    };

    this.http
      .put(`/api/Downtimes/${id}`, payload, { observe: 'response', responseType: 'text' })
      .subscribe({
        next: () => {
          this.savingEdit = false;
          this.editingId = null;
          this.customersOpenEdit = false;
          this.reasonsOpenEdit = false;
          this.showToast('Kayıt güncellendi ✅', 'success');
          this.refresh();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.error = 'Güncelleme başarısız.';
          this.savingEdit = false;
          this.showToast('Güncelleme başarısız ❌', 'error');
          this.cdr.detectChanges();
        },
      });
  }

  deleteRow(id: string) {
    if (!id || this.deletingId) return;

    this.error = '';
    this.deletingId = id;
    this.cdr.detectChanges();

    this.http
      .delete(`/api/Downtimes/${id}`, { observe: 'response', responseType: 'text' })
      .subscribe({
        next: () => {
          this.deletingId = null;
          this.showToast('Silme başarılı ✅', 'success');
          this.refresh();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.deletingId = null;
          this.error = 'Silme başarısız.';
          this.showToast('Silme başarısız ❌', 'error');
          this.cdr.detectChanges();
        },
      });
  }
}