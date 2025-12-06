# Техническое задание на Frontend-разработку MedicationAssist

## 1. Общая информация

### 1.1 Название проекта
**MedicationAssist Frontend** — клиентское веб-приложение для системы контроля приема лекарственных препаратов.

### 1.2 Технический стек
- **React 18+** — основной фреймворк
- **MobX** — управление состоянием
- **CSS Modules** — стилизация компонентов
- **React Router v6** — маршрутизация
- **Axios** — HTTP-клиент для работы с API
- **TypeScript** — типизация (рекомендуется)

### 1.3 Цель приложения
Создать современный, отзывчивый веб-интерфейс для управления списком лекарств и регистрации их приема. Приложение должно быть интуитивно понятным для целевой аудитории (пожилые люди, пациенты с хроническими заболеваниями).

### 1.4 Целевая аудитория
- Пациенты, принимающие лекарства регулярно
- Люди с хроническими заболеваниями
- Пожилые люди
- Пользователи, принимающие несколько препаратов одновременно

**Важно:** Интерфейс должен быть простым, с крупными элементами управления, хорошей контрастностью и понятной навигацией.

---

## 2. Базовый URL API

```
Development: http://localhost:5000/api
Production: настраивается через переменные окружения
```

---

## 3. Аутентификация

### 3.1 Механизм аутентификации
- JWT токены в заголовке `Authorization: Bearer {token}`
- Токен хранится в `localStorage`
- Время жизни токена: 60 минут (Production), 1440 минут (Development)
- При истечении токена — редирект на страницу входа

### 3.2 Публичные маршруты (без авторизации)
- `/login` — страница входа
- `/register` — страница регистрации

### 3.3 Защищенные маршруты (требуют авторизации)
- `/` — главная страница (дашборд)
- `/medications` — список лекарств
- `/medications/:id` — детали лекарства
- `/intakes` — история приемов
- `/profile` — профиль пользователя

---

## 4. Структура приложения

### 4.1 Структура директорий

```
src/
├── api/                    # API клиент и сервисы
│   ├── client.ts           # Axios instance с интерцепторами
│   ├── auth.api.ts         # Методы аутентификации
│   ├── users.api.ts        # Методы работы с пользователями
│   ├── medications.api.ts  # Методы работы с лекарствами
│   └── intakes.api.ts      # Методы работы с приемами
│
├── stores/                 # MobX сторы
│   ├── RootStore.ts        # Корневой стор
│   ├── AuthStore.ts        # Стор аутентификации
│   ├── UserStore.ts        # Стор пользователя
│   ├── MedicationStore.ts  # Стор лекарств
│   ├── IntakeStore.ts      # Стор приемов
│   └── UIStore.ts          # Стор UI состояния
│
├── components/             # React компоненты
│   ├── common/             # Общие компоненты
│   │   ├── Button/
│   │   ├── Input/
│   │   ├── Modal/
│   │   ├── Card/
│   │   ├── Loader/
│   │   ├── ErrorMessage/
│   │   └── ...
│   │
│   ├── layout/             # Компоненты разметки
│   │   ├── Header/
│   │   ├── Sidebar/
│   │   ├── Footer/
│   │   └── MainLayout/
│   │
│   ├── auth/               # Компоненты аутентификации
│   │   ├── LoginForm/
│   │   ├── RegisterForm/
│   │   └── ProtectedRoute/
│   │
│   ├── medications/        # Компоненты лекарств
│   │   ├── MedicationList/
│   │   ├── MedicationCard/
│   │   ├── MedicationForm/
│   │   └── MedicationDetail/
│   │
│   ├── intakes/            # Компоненты приемов
│   │   ├── IntakeList/
│   │   ├── IntakeCard/
│   │   ├── IntakeForm/
│   │   ├── IntakeFilter/
│   │   └── QuickIntakeButton/
│   │
│   └── dashboard/          # Компоненты дашборда
│       ├── TodayIntakes/
│       ├── MedicationSummary/
│       └── RecentActivity/
│
├── pages/                  # Страницы приложения
│   ├── LoginPage/
│   ├── RegisterPage/
│   ├── DashboardPage/
│   ├── MedicationsPage/
│   ├── MedicationDetailPage/
│   ├── IntakesPage/
│   ├── ProfilePage/
│   └── NotFoundPage/
│
├── hooks/                  # Кастомные хуки
│   ├── useAuth.ts
│   ├── useStores.ts
│   └── useForm.ts
│
├── types/                  # TypeScript типы
│   ├── user.types.ts
│   ├── medication.types.ts
│   ├── intake.types.ts
│   └── api.types.ts
│
├── utils/                  # Утилиты
│   ├── formatDate.ts
│   ├── validators.ts
│   └── constants.ts
│
├── styles/                 # Глобальные стили
│   ├── variables.css       # CSS переменные
│   ├── reset.css           # Сброс стилей
│   └── global.css          # Глобальные стили
│
├── App.tsx                 # Корневой компонент
├── index.tsx               # Точка входа
└── routes.tsx              # Конфигурация маршрутов
```

---

## 5. TypeScript типы

### 5.1 Типы пользователя

```typescript
// types/user.types.ts

export enum UserRole {
  User = 'User',
  Admin = 'Admin'
}

export interface User {
  id: string;           // GUID
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;    // ISO 8601 datetime
  updatedAt: string | null;
}

export interface RegisterDto {
  name: string;
  email: string;
  password: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface UpdateUserDto {
  name: string;
  email: string;
}
```

### 5.2 Типы лекарств

```typescript
// types/medication.types.ts

export interface Medication {
  id: string;           // GUID
  userId: string;       // GUID
  name: string;
  description: string | null;
  dosage: string | null;
  createdAt: string;    // ISO 8601 datetime
  updatedAt: string | null;
}

export interface CreateMedicationDto {
  name: string;
  description?: string;
  dosage?: string;
}

export interface UpdateMedicationDto {
  name: string;
  description?: string;
  dosage?: string;
}
```

### 5.3 Типы приемов

```typescript
// types/intake.types.ts

export interface MedicationIntake {
  id: string;           // GUID
  userId: string;       // GUID
  medicationId: string; // GUID
  medicationName: string;
  intakeTime: string;   // ISO 8601 datetime
  notes: string | null;
  createdAt: string;    // ISO 8601 datetime
  updatedAt: string | null;
}

export interface CreateIntakeDto {
  medicationId: string;
  intakeTime?: string;  // ISO 8601, опционально (по умолчанию — текущее время)
  notes?: string;
}

export interface UpdateIntakeDto {
  intakeTime: string;   // ISO 8601
  notes?: string;
}

export interface IntakeFilter {
  fromDate?: string;    // ISO 8601
  toDate?: string;      // ISO 8601
  medicationId?: string;
}
```

### 5.4 Типы API

```typescript
// types/api.types.ts

export interface ApiError {
  error: string;
}

export interface ApiResponse<T> {
  data: T;
  status: number;
}
```

---

## 6. API клиент

### 6.1 Базовый клиент (Axios)

```typescript
// api/client.ts

import axios, { AxiosError, AxiosInstance } from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const client: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request интерцептор — добавление токена
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response интерцептор — обработка ошибок
client.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Токен истек или невалидный
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default client;
```

### 6.2 API сервисы

#### Auth API

```typescript
// api/auth.api.ts

import client from './client';
import { RegisterDto, LoginDto, AuthResponse } from '../types/user.types';

export const authApi = {
  register: (data: RegisterDto) => 
    client.post<AuthResponse>('/auth/register', data),
  
  login: (data: LoginDto) => 
    client.post<AuthResponse>('/auth/login', data),
};
```

#### Users API

```typescript
// api/users.api.ts

import client from './client';
import { User, UpdateUserDto } from '../types/user.types';

export const usersApi = {
  getAll: () => 
    client.get<User[]>('/users'),
  
  getById: (id: string) => 
    client.get<User>(`/users/${id}`),
  
  getByEmail: (email: string) => 
    client.get<User>(`/users/by-email/${email}`),
  
  update: (id: string, data: UpdateUserDto) => 
    client.put<User>(`/users/${id}`, data),
  
  delete: (id: string) => 
    client.delete(`/users/${id}`),
};
```

#### Medications API

```typescript
// api/medications.api.ts

import client from './client';
import { Medication, CreateMedicationDto, UpdateMedicationDto } from '../types/medication.types';

export const medicationsApi = {
  getAll: (userId: string) => 
    client.get<Medication[]>(`/users/${userId}/medications`),
  
  getById: (userId: string, id: string) => 
    client.get<Medication>(`/users/${userId}/medications/${id}`),
  
  create: (userId: string, data: CreateMedicationDto) => 
    client.post<Medication>(`/users/${userId}/medications`, data),
  
  update: (userId: string, id: string, data: UpdateMedicationDto) => 
    client.put<Medication>(`/users/${userId}/medications/${id}`, data),
  
  delete: (userId: string, id: string) => 
    client.delete(`/users/${userId}/medications/${id}`),
};
```

#### Intakes API

```typescript
// api/intakes.api.ts

import client from './client';
import { MedicationIntake, CreateIntakeDto, UpdateIntakeDto, IntakeFilter } from '../types/intake.types';

export const intakesApi = {
  getAll: (userId: string, filter?: IntakeFilter) => {
    const params = new URLSearchParams();
    if (filter?.fromDate) params.append('fromDate', filter.fromDate);
    if (filter?.toDate) params.append('toDate', filter.toDate);
    if (filter?.medicationId) params.append('medicationId', filter.medicationId);
    
    return client.get<MedicationIntake[]>(`/users/${userId}/intakes?${params.toString()}`);
  },
  
  getById: (userId: string, id: string) => 
    client.get<MedicationIntake>(`/users/${userId}/intakes/${id}`),
  
  create: (userId: string, data: CreateIntakeDto) => 
    client.post<MedicationIntake>(`/users/${userId}/intakes`, data),
  
  update: (userId: string, id: string, data: UpdateIntakeDto) => 
    client.put<MedicationIntake>(`/users/${userId}/intakes/${id}`, data),
  
  delete: (userId: string, id: string) => 
    client.delete(`/users/${userId}/intakes/${id}`),
};
```

---

## 7. MobX Сторы

### 7.1 Root Store

```typescript
// stores/RootStore.ts

import { AuthStore } from './AuthStore';
import { UserStore } from './UserStore';
import { MedicationStore } from './MedicationStore';
import { IntakeStore } from './IntakeStore';
import { UIStore } from './UIStore';

export class RootStore {
  authStore: AuthStore;
  userStore: UserStore;
  medicationStore: MedicationStore;
  intakeStore: IntakeStore;
  uiStore: UIStore;

  constructor() {
    this.authStore = new AuthStore(this);
    this.userStore = new UserStore(this);
    this.medicationStore = new MedicationStore(this);
    this.intakeStore = new IntakeStore(this);
    this.uiStore = new UIStore(this);
  }
}

export const rootStore = new RootStore();
```

### 7.2 Auth Store

```typescript
// stores/AuthStore.ts

import { makeAutoObservable, runInAction } from 'mobx';
import { authApi } from '../api/auth.api';
import { User, RegisterDto, LoginDto } from '../types/user.types';
import { RootStore } from './RootStore';

export class AuthStore {
  rootStore: RootStore;
  user: User | null = null;
  token: string | null = null;
  isLoading: boolean = false;
  error: string | null = null;
  isInitialized: boolean = false;

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore;
    makeAutoObservable(this);
    this.initFromStorage();
  }

  get isAuthenticated(): boolean {
    return !!this.token && !!this.user;
  }

  get userId(): string | null {
    return this.user?.id || null;
  }

  private initFromStorage() {
    const token = localStorage.getItem('token');
    const userJson = localStorage.getItem('user');
    
    if (token && userJson) {
      try {
        this.token = token;
        this.user = JSON.parse(userJson);
      } catch {
        this.clearAuth();
      }
    }
    this.isInitialized = true;
  }

  private saveToStorage(token: string, user: User) {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
  }

  private clearAuth() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.token = null;
    this.user = null;
  }

  async register(data: RegisterDto): Promise<boolean> {
    this.isLoading = true;
    this.error = null;

    try {
      const response = await authApi.register(data);
      runInAction(() => {
        this.token = response.data.token;
        this.user = response.data.user;
        this.saveToStorage(response.data.token, response.data.user);
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка регистрации';
        this.isLoading = false;
      });
      return false;
    }
  }

  async login(data: LoginDto): Promise<boolean> {
    this.isLoading = true;
    this.error = null;

    try {
      const response = await authApi.login(data);
      runInAction(() => {
        this.token = response.data.token;
        this.user = response.data.user;
        this.saveToStorage(response.data.token, response.data.user);
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Неверный email или пароль';
        this.isLoading = false;
      });
      return false;
    }
  }

  logout() {
    this.clearAuth();
    // Очистка других сторов
    this.rootStore.medicationStore.clear();
    this.rootStore.intakeStore.clear();
  }

  clearError() {
    this.error = null;
  }
}
```

### 7.3 Medication Store

```typescript
// stores/MedicationStore.ts

import { makeAutoObservable, runInAction } from 'mobx';
import { medicationsApi } from '../api/medications.api';
import { Medication, CreateMedicationDto, UpdateMedicationDto } from '../types/medication.types';
import { RootStore } from './RootStore';

export class MedicationStore {
  rootStore: RootStore;
  medications: Medication[] = [];
  selectedMedication: Medication | null = null;
  isLoading: boolean = false;
  error: string | null = null;

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore;
    makeAutoObservable(this);
  }

  get userId(): string | null {
    return this.rootStore.authStore.userId;
  }

  get sortedMedications(): Medication[] {
    return [...this.medications].sort((a, b) => a.name.localeCompare(b.name));
  }

  async fetchMedications(): Promise<void> {
    if (!this.userId) return;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await medicationsApi.getAll(this.userId);
      runInAction(() => {
        this.medications = response.data;
        this.isLoading = false;
      });
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка загрузки лекарств';
        this.isLoading = false;
      });
    }
  }

  async fetchMedicationById(id: string): Promise<void> {
    if (!this.userId) return;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await medicationsApi.getById(this.userId, id);
      runInAction(() => {
        this.selectedMedication = response.data;
        this.isLoading = false;
      });
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Лекарство не найдено';
        this.isLoading = false;
      });
    }
  }

  async createMedication(data: CreateMedicationDto): Promise<Medication | null> {
    if (!this.userId) return null;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await medicationsApi.create(this.userId, data);
      runInAction(() => {
        this.medications.push(response.data);
        this.isLoading = false;
      });
      return response.data;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка создания лекарства';
        this.isLoading = false;
      });
      return null;
    }
  }

  async updateMedication(id: string, data: UpdateMedicationDto): Promise<boolean> {
    if (!this.userId) return false;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await medicationsApi.update(this.userId, id, data);
      runInAction(() => {
        const index = this.medications.findIndex(m => m.id === id);
        if (index !== -1) {
          this.medications[index] = response.data;
        }
        if (this.selectedMedication?.id === id) {
          this.selectedMedication = response.data;
        }
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка обновления лекарства';
        this.isLoading = false;
      });
      return false;
    }
  }

  async deleteMedication(id: string): Promise<boolean> {
    if (!this.userId) return false;
    
    this.isLoading = true;
    this.error = null;

    try {
      await medicationsApi.delete(this.userId, id);
      runInAction(() => {
        this.medications = this.medications.filter(m => m.id !== id);
        if (this.selectedMedication?.id === id) {
          this.selectedMedication = null;
        }
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка удаления лекарства';
        this.isLoading = false;
      });
      return false;
    }
  }

  getMedicationById(id: string): Medication | undefined {
    return this.medications.find(m => m.id === id);
  }

  clear() {
    this.medications = [];
    this.selectedMedication = null;
    this.error = null;
  }

  clearError() {
    this.error = null;
  }
}
```

### 7.4 Intake Store

```typescript
// stores/IntakeStore.ts

import { makeAutoObservable, runInAction } from 'mobx';
import { intakesApi } from '../api/intakes.api';
import { MedicationIntake, CreateIntakeDto, UpdateIntakeDto, IntakeFilter } from '../types/intake.types';
import { RootStore } from './RootStore';

export class IntakeStore {
  rootStore: RootStore;
  intakes: MedicationIntake[] = [];
  selectedIntake: MedicationIntake | null = null;
  filter: IntakeFilter = {};
  isLoading: boolean = false;
  error: string | null = null;

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore;
    makeAutoObservable(this);
  }

  get userId(): string | null {
    return this.rootStore.authStore.userId;
  }

  // Сортировка по времени приема (новые сверху)
  get sortedIntakes(): MedicationIntake[] {
    return [...this.intakes].sort(
      (a, b) => new Date(b.intakeTime).getTime() - new Date(a.intakeTime).getTime()
    );
  }

  // Приемы за сегодня
  get todayIntakes(): MedicationIntake[] {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    return this.sortedIntakes.filter(intake => {
      const intakeDate = new Date(intake.intakeTime);
      return intakeDate >= today && intakeDate < tomorrow;
    });
  }

  // Группировка по дате
  get intakesGroupedByDate(): Map<string, MedicationIntake[]> {
    const grouped = new Map<string, MedicationIntake[]>();
    
    this.sortedIntakes.forEach(intake => {
      const date = new Date(intake.intakeTime).toLocaleDateString('ru-RU');
      const existing = grouped.get(date) || [];
      grouped.set(date, [...existing, intake]);
    });
    
    return grouped;
  }

  setFilter(filter: IntakeFilter) {
    this.filter = filter;
  }

  clearFilter() {
    this.filter = {};
  }

  async fetchIntakes(customFilter?: IntakeFilter): Promise<void> {
    if (!this.userId) return;
    
    this.isLoading = true;
    this.error = null;
    const filterToUse = customFilter || this.filter;

    try {
      const response = await intakesApi.getAll(this.userId, filterToUse);
      runInAction(() => {
        this.intakes = response.data;
        this.isLoading = false;
      });
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка загрузки истории приемов';
        this.isLoading = false;
      });
    }
  }

  async fetchIntakeById(id: string): Promise<void> {
    if (!this.userId) return;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await intakesApi.getById(this.userId, id);
      runInAction(() => {
        this.selectedIntake = response.data;
        this.isLoading = false;
      });
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Запись не найдена';
        this.isLoading = false;
      });
    }
  }

  async createIntake(data: CreateIntakeDto): Promise<MedicationIntake | null> {
    if (!this.userId) return null;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await intakesApi.create(this.userId, data);
      runInAction(() => {
        this.intakes.unshift(response.data);
        this.isLoading = false;
      });
      return response.data;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка регистрации приема';
        this.isLoading = false;
      });
      return null;
    }
  }

  async updateIntake(id: string, data: UpdateIntakeDto): Promise<boolean> {
    if (!this.userId) return false;
    
    this.isLoading = true;
    this.error = null;

    try {
      const response = await intakesApi.update(this.userId, id, data);
      runInAction(() => {
        const index = this.intakes.findIndex(i => i.id === id);
        if (index !== -1) {
          this.intakes[index] = response.data;
        }
        if (this.selectedIntake?.id === id) {
          this.selectedIntake = response.data;
        }
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка обновления записи';
        this.isLoading = false;
      });
      return false;
    }
  }

  async deleteIntake(id: string): Promise<boolean> {
    if (!this.userId) return false;
    
    this.isLoading = true;
    this.error = null;

    try {
      await intakesApi.delete(this.userId, id);
      runInAction(() => {
        this.intakes = this.intakes.filter(i => i.id !== id);
        if (this.selectedIntake?.id === id) {
          this.selectedIntake = null;
        }
        this.isLoading = false;
      });
      return true;
    } catch (err: any) {
      runInAction(() => {
        this.error = err.response?.data?.error || 'Ошибка удаления записи';
        this.isLoading = false;
      });
      return false;
    }
  }

  // Быстрая регистрация приема (текущее время)
  async quickIntake(medicationId: string, notes?: string): Promise<MedicationIntake | null> {
    return this.createIntake({
      medicationId,
      notes,
      // intakeTime не указываем — сервер установит текущее время
    });
  }

  clear() {
    this.intakes = [];
    this.selectedIntake = null;
    this.filter = {};
    this.error = null;
  }

  clearError() {
    this.error = null;
  }
}
```

### 7.5 UI Store

```typescript
// stores/UIStore.ts

import { makeAutoObservable } from 'mobx';
import { RootStore } from './RootStore';

export type ModalType = 'createMedication' | 'editMedication' | 'deleteMedication' |
                        'createIntake' | 'editIntake' | 'deleteIntake' | 
                        'quickIntake' | 'confirm' | null;

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

export class UIStore {
  rootStore: RootStore;
  
  // Модальные окна
  activeModal: ModalType = null;
  modalData: any = null;
  
  // Тосты (уведомления)
  toasts: Toast[] = [];
  
  // Сайдбар
  isSidebarCollapsed: boolean = false;
  
  // Мобильное меню
  isMobileMenuOpen: boolean = false;

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore;
    makeAutoObservable(this);
  }

  // Модальные окна
  openModal(type: ModalType, data?: any) {
    this.activeModal = type;
    this.modalData = data || null;
  }

  closeModal() {
    this.activeModal = null;
    this.modalData = null;
  }

  // Тосты
  showToast(type: ToastType, message: string) {
    const id = Date.now().toString();
    this.toasts.push({ id, type, message });
    
    // Автоматическое удаление через 5 секунд
    setTimeout(() => {
      this.removeToast(id);
    }, 5000);
  }

  removeToast(id: string) {
    this.toasts = this.toasts.filter(t => t.id !== id);
  }

  // Сайдбар
  toggleSidebar() {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  // Мобильное меню
  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  closeMobileMenu() {
    this.isMobileMenuOpen = false;
  }
}
```

---

## 8. Описание страниц

### 8.1 Страница входа (`/login`)

**Компоненты:**
- Форма входа с полями:
  - Email (input type="email", обязательное)
  - Пароль (input type="password", обязательное)
- Кнопка "Войти"
- Ссылка "Нет аккаунта? Зарегистрируйтесь"
- Отображение ошибки аутентификации

**Поведение:**
- При успешном входе — редирект на `/`
- При ошибке — показать сообщение "Неверный email или пароль"
- Валидация формы на клиенте

**UI/UX:**
- Центрирование формы по вертикали и горизонтали
- Логотип приложения над формой
- Крупные поля ввода (мин. 48px высота)
- Заметная кнопка входа

### 8.2 Страница регистрации (`/register`)

**Компоненты:**
- Форма регистрации с полями:
  - Имя (input type="text", обязательное, макс 200 символов)
  - Email (input type="email", обязательное, макс 200 символов)
  - Пароль (input type="password", обязательное, мин 6 символов)
  - Подтверждение пароля
- Кнопка "Зарегистрироваться"
- Ссылка "Уже есть аккаунт? Войдите"

**Валидация:**
- Имя: не пустое, максимум 200 символов
- Email: корректный формат, максимум 200 символов
- Пароль: минимум 6 символов
- Подтверждение пароля: совпадает с паролем

**Поведение:**
- При успешной регистрации — автоматический вход и редирект на `/`
- При ошибке "Пользователь с таким email уже существует" — показать сообщение

### 8.3 Главная страница / Дашборд (`/`)

**Компоненты:**
- **Приветствие** — "Здравствуйте, {имя}!"
- **Карточка "Приемы сегодня"**:
  - Список приемов за сегодня
  - Если пусто — "Сегодня приемов не было"
- **Кнопка быстрого приема**:
  - Большая заметная кнопка "Принял лекарство"
  - Открывает модальное окно выбора лекарства
- **Карточка "Мои лекарства"**:
  - Количество лекарств
  - Кнопка "Все лекарства"
- **Карточка "Последняя активность"**:
  - 5 последних приемов
  - Ссылка "Вся история"

**UI/UX:**
- Крупные карточки с иконками
- Яркая кнопка быстрого приема
- Адаптивная сетка (1-2-3 колонки)

### 8.4 Страница лекарств (`/medications`)

**Компоненты:**
- **Заголовок страницы** с кнопкой "Добавить лекарство"
- **Список лекарств** в виде карточек:
  - Название
  - Дозировка (если указана)
  - Описание (сокращенное)
  - Кнопки: "Подробнее", "Принять", "Редактировать", "Удалить"
- **Пустое состояние**: "У вас пока нет лекарств. Добавьте первое лекарство."

**Модальные окна:**
- Создание лекарства
- Редактирование лекарства
- Подтверждение удаления
- Быстрая регистрация приема

### 8.5 Страница деталей лекарства (`/medications/:id`)

**Компоненты:**
- **Карточка лекарства**:
  - Название (крупным шрифтом)
  - Дозировка
  - Описание (полное)
  - Дата добавления
- **Кнопки действий**:
  - "Принял лекарство" (большая, заметная)
  - "Редактировать"
  - "Удалить"
- **История приемов этого лекарства**:
  - Последние 10 приемов
  - Ссылка "Вся история"

### 8.6 Страница истории приемов (`/intakes`)

**Компоненты:**
- **Заголовок страницы**
- **Панель фильтров**:
  - Выбор периода (от-до) с datepicker
  - Выбор лекарства (select/dropdown)
  - Кнопки "Применить" и "Сбросить"
- **Список приемов** сгруппированный по датам:
  - Дата-разделитель
  - Карточки приемов:
    - Название лекарства
    - Время приема
    - Примечания (если есть)
    - Кнопки: "Редактировать", "Удалить"
- **Пагинация** или infinite scroll (опционально)
- **Пустое состояние**: "Нет записей о приеме лекарств"

### 8.7 Страница профиля (`/profile`)

**Компоненты:**
- **Карточка пользователя**:
  - Имя
  - Email
  - Роль
  - Дата регистрации
- **Форма редактирования**:
  - Поле имени
  - Поле email
  - Кнопка "Сохранить"
- **Кнопка выхода**

### 8.8 Страница 404 (`/404`)

**Компоненты:**
- Иконка или иллюстрация
- Заголовок "Страница не найдена"
- Описание
- Кнопка "На главную"

---

## 9. Общие компоненты (UI Kit)

### 9.1 Button

```typescript
interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'small' | 'medium' | 'large';
  fullWidth?: boolean;
  disabled?: boolean;
  loading?: boolean;
  icon?: React.ReactNode;
  onClick?: () => void;
  type?: 'button' | 'submit' | 'reset';
}
```

**Варианты:**
- `primary` — основная кнопка (яркий цвет)
- `secondary` — вторичная кнопка (светлая)
- `danger` — опасное действие (красный)
- `ghost` — прозрачная кнопка

### 9.2 Input

```typescript
interface InputProps {
  label?: string;
  placeholder?: string;
  type?: 'text' | 'email' | 'password' | 'number' | 'date' | 'datetime-local';
  value: string;
  onChange: (value: string) => void;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  maxLength?: number;
}
```

### 9.3 Select

```typescript
interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps {
  label?: string;
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  error?: string;
  disabled?: boolean;
  required?: boolean;
}
```

### 9.4 Modal

```typescript
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  size?: 'small' | 'medium' | 'large';
}
```

### 9.5 Card

```typescript
interface CardProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  actions?: React.ReactNode;
  onClick?: () => void;
  hoverable?: boolean;
}
```

### 9.6 Loader

```typescript
interface LoaderProps {
  size?: 'small' | 'medium' | 'large';
  fullScreen?: boolean;
  text?: string;
}
```

### 9.7 Toast

```typescript
interface ToastProps {
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  onClose: () => void;
}
```

### 9.8 EmptyState

```typescript
interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}
```

### 9.9 ConfirmDialog

```typescript
interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  onConfirm: () => void;
  onCancel: () => void;
  variant?: 'danger' | 'warning' | 'info';
}
```

---

## 10. Стилизация (CSS Modules)

### 10.1 CSS Переменные

```css
/* styles/variables.css */

:root {
  /* Цвета */
  --color-primary: #2563eb;
  --color-primary-hover: #1d4ed8;
  --color-primary-light: #dbeafe;
  
  --color-secondary: #64748b;
  --color-secondary-hover: #475569;
  
  --color-success: #22c55e;
  --color-success-light: #dcfce7;
  
  --color-danger: #ef4444;
  --color-danger-hover: #dc2626;
  --color-danger-light: #fee2e2;
  
  --color-warning: #f59e0b;
  --color-warning-light: #fef3c7;
  
  --color-info: #3b82f6;
  --color-info-light: #dbeafe;
  
  /* Нейтральные цвета */
  --color-white: #ffffff;
  --color-gray-50: #f8fafc;
  --color-gray-100: #f1f5f9;
  --color-gray-200: #e2e8f0;
  --color-gray-300: #cbd5e1;
  --color-gray-400: #94a3b8;
  --color-gray-500: #64748b;
  --color-gray-600: #475569;
  --color-gray-700: #334155;
  --color-gray-800: #1e293b;
  --color-gray-900: #0f172a;
  
  /* Текст */
  --text-primary: var(--color-gray-900);
  --text-secondary: var(--color-gray-600);
  --text-muted: var(--color-gray-400);
  --text-inverse: var(--color-white);
  
  /* Фон */
  --bg-primary: var(--color-white);
  --bg-secondary: var(--color-gray-50);
  --bg-tertiary: var(--color-gray-100);
  
  /* Границы */
  --border-color: var(--color-gray-200);
  --border-radius-sm: 4px;
  --border-radius-md: 8px;
  --border-radius-lg: 12px;
  --border-radius-xl: 16px;
  --border-radius-full: 9999px;
  
  /* Тени */
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
  --shadow-xl: 0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1);
  
  /* Отступы */
  --spacing-xs: 4px;
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
  --spacing-xl: 32px;
  --spacing-2xl: 48px;
  
  /* Типографика */
  --font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-size-xs: 12px;
  --font-size-sm: 14px;
  --font-size-md: 16px;
  --font-size-lg: 18px;
  --font-size-xl: 20px;
  --font-size-2xl: 24px;
  --font-size-3xl: 30px;
  --font-size-4xl: 36px;
  
  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
  
  --line-height-tight: 1.25;
  --line-height-normal: 1.5;
  --line-height-relaxed: 1.75;
  
  /* Z-index слои */
  --z-dropdown: 100;
  --z-sticky: 200;
  --z-fixed: 300;
  --z-modal-backdrop: 400;
  --z-modal: 500;
  --z-popover: 600;
  --z-tooltip: 700;
  --z-toast: 800;
  
  /* Анимации */
  --transition-fast: 150ms ease;
  --transition-normal: 300ms ease;
  --transition-slow: 500ms ease;
  
  /* Breakpoints (для медиа-запросов) */
  --breakpoint-sm: 640px;
  --breakpoint-md: 768px;
  --breakpoint-lg: 1024px;
  --breakpoint-xl: 1280px;
}
```

### 10.2 Пример CSS Module для компонента

```css
/* components/common/Button/Button.module.css */

.button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  
  font-family: var(--font-family);
  font-weight: var(--font-weight-medium);
  line-height: 1;
  
  border: none;
  border-radius: var(--border-radius-md);
  cursor: pointer;
  
  transition: all var(--transition-fast);
}

.button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Размеры */
.small {
  height: 32px;
  padding: 0 var(--spacing-md);
  font-size: var(--font-size-sm);
}

.medium {
  height: 44px;
  padding: 0 var(--spacing-lg);
  font-size: var(--font-size-md);
}

.large {
  height: 56px;
  padding: 0 var(--spacing-xl);
  font-size: var(--font-size-lg);
}

/* Варианты */
.primary {
  background-color: var(--color-primary);
  color: var(--text-inverse);
}

.primary:hover:not(:disabled) {
  background-color: var(--color-primary-hover);
}

.secondary {
  background-color: var(--bg-tertiary);
  color: var(--text-primary);
}

.secondary:hover:not(:disabled) {
  background-color: var(--color-gray-200);
}

.danger {
  background-color: var(--color-danger);
  color: var(--text-inverse);
}

.danger:hover:not(:disabled) {
  background-color: var(--color-danger-hover);
}

.ghost {
  background-color: transparent;
  color: var(--color-primary);
}

.ghost:hover:not(:disabled) {
  background-color: var(--color-primary-light);
}

/* Полная ширина */
.fullWidth {
  width: 100%;
}

/* Состояние загрузки */
.loading {
  position: relative;
  color: transparent;
}

.spinner {
  position: absolute;
  width: 20px;
  height: 20px;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 0.75s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
```

---

## 11. Доступность (Accessibility)

### 11.1 Требования

**WCAG 2.1 AA:**
- Контрастность текста минимум 4.5:1
- Интерактивные элементы минимум 44x44 пикселя
- Все функции доступны с клавиатуры
- Фокус видим на всех интерактивных элементах
- ARIA-атрибуты для кастомных компонентов

### 11.2 Реализация

**Формы:**
```jsx
<label htmlFor="email">Email</label>
<input 
  id="email"
  type="email"
  aria-required="true"
  aria-invalid={!!error}
  aria-describedby={error ? "email-error" : undefined}
/>
{error && <span id="email-error" role="alert">{error}</span>}
```

**Модальные окна:**
```jsx
<div 
  role="dialog"
  aria-modal="true"
  aria-labelledby="modal-title"
>
  <h2 id="modal-title">Заголовок</h2>
</div>
```

**Кнопки с иконками:**
```jsx
<button aria-label="Удалить лекарство">
  <TrashIcon aria-hidden="true" />
</button>
```

**Уведомления:**
```jsx
<div role="alert" aria-live="polite">
  {message}
</div>
```

---

## 12. Адаптивный дизайн

### 12.1 Breakpoints

- **Mobile**: < 640px
- **Tablet**: 640px - 1023px
- **Desktop**: ≥ 1024px

### 12.2 Принципы

**Mobile First:**
- Базовые стили для мобильных устройств
- Расширение через `@media (min-width: ...)`

**Сетка:**
- Мобильный: 1 колонка
- Планшет: 2 колонки
- Десктоп: 3-4 колонки

**Навигация:**
- Мобильный: hamburger-меню
- Планшет/Десктоп: боковая панель или верхняя навигация

**Touch targets:**
- Минимальный размер 44x44px для мобильных устройств

---

## 13. Обработка ошибок

### 13.1 Типы ошибок

**Сетевые ошибки:**
```typescript
// Нет соединения
"Нет подключения к интернету. Проверьте соединение."

// Сервер недоступен
"Сервер временно недоступен. Попробуйте позже."
```

**API ошибки:**
```typescript
// Ошибки валидации (400)
// Отображаем сообщение из API: error.response.data.error

// Не авторизован (401)
"Сессия истекла. Войдите снова."

// Нет доступа (403)
"У вас нет доступа к этому ресурсу."

// Не найдено (404)
"Запрашиваемый ресурс не найден."

// Внутренняя ошибка (500)
"Произошла ошибка на сервере. Попробуйте позже."
```

### 13.2 Отображение ошибок

**Формы:**
- Ошибки полей под соответствующими полями
- Общие ошибки формы над кнопкой отправки

**Списки:**
- Ошибки загрузки в центре с кнопкой "Повторить"

**Тосты:**
- Уведомления об успешных и неуспешных операциях

---

## 14. Сообщения пользователю

### 14.1 Успешные действия (Toast success)

```
"Регистрация прошла успешно!"
"Вход выполнен успешно"
"Лекарство добавлено"
"Лекарство обновлено"
"Лекарство удалено"
"Прием зарегистрирован"
"Запись обновлена"
"Запись удалена"
"Профиль обновлен"
```

### 14.2 Ошибки (Toast error)

```
"Неверный email или пароль"
"Пользователь с таким email уже существует"
"Ошибка загрузки данных"
"Не удалось сохранить изменения"
"Не удалось удалить"
```

### 14.3 Подтверждения

```
Удаление лекарства:
  Заголовок: "Удалить лекарство?"
  Сообщение: "Лекарство «{название}» будет удалено. Это действие нельзя отменить."
  Кнопки: "Отмена", "Удалить"

Удаление записи о приеме:
  Заголовок: "Удалить запись?"
  Сообщение: "Запись о приеме будет удалена. Это действие нельзя отменить."
  Кнопки: "Отмена", "Удалить"

Выход из аккаунта:
  Заголовок: "Выйти из аккаунта?"
  Сообщение: "Вы уверены, что хотите выйти?"
  Кнопки: "Отмена", "Выйти"
```

---

## 15. Валидация форм

### 15.1 Регистрация

```typescript
const registerValidation = {
  name: {
    required: "Введите ваше имя",
    maxLength: { value: 200, message: "Имя не может превышать 200 символов" }
  },
  email: {
    required: "Введите email",
    pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: "Некорректный email" },
    maxLength: { value: 200, message: "Email не может превышать 200 символов" }
  },
  password: {
    required: "Введите пароль",
    minLength: { value: 6, message: "Пароль должен содержать минимум 6 символов" },
    maxLength: { value: 100, message: "Пароль не может превышать 100 символов" }
  },
  confirmPassword: {
    required: "Подтвердите пароль",
    validate: (value, formValues) => value === formValues.password || "Пароли не совпадают"
  }
};
```

### 15.2 Вход

```typescript
const loginValidation = {
  email: {
    required: "Введите email"
  },
  password: {
    required: "Введите пароль"
  }
};
```

### 15.3 Лекарство

```typescript
const medicationValidation = {
  name: {
    required: "Введите название лекарства",
    maxLength: { value: 200, message: "Название не может превышать 200 символов" }
  },
  description: {
    maxLength: { value: 1000, message: "Описание не может превышать 1000 символов" }
  },
  dosage: {
    maxLength: { value: 100, message: "Дозировка не может превышать 100 символов" }
  }
};
```

### 15.4 Прием лекарства

```typescript
const intakeValidation = {
  medicationId: {
    required: "Выберите лекарство"
  },
  intakeTime: {
    validate: (value) => {
      if (!value) return true; // Необязательное поле
      const date = new Date(value);
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      return date <= tomorrow || "Время приема не может быть более чем через день";
    }
  },
  notes: {
    maxLength: { value: 500, message: "Примечания не могут превышать 500 символов" }
  }
};
```

---

## 16. Форматирование данных

### 16.1 Даты и время

```typescript
// utils/formatDate.ts

export const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  });
  // Результат: "6 декабря 2025"
};

export const formatTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit'
  });
  // Результат: "14:30"
};

export const formatDateTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
  // Результат: "6 декабря 2025, 14:30"
};

export const formatRelativeDate = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Только что';
  if (diffMins < 60) return `${diffMins} мин. назад`;
  if (diffHours < 24) return `${diffHours} ч. назад`;
  if (diffDays < 7) return `${diffDays} дн. назад`;
  
  return formatDate(dateString);
};

// Для input type="datetime-local"
export const toDateTimeLocalValue = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toISOString().slice(0, 16);
};
```

---

## 17. Роутинг

### 17.1 Конфигурация маршрутов

```typescript
// routes.tsx

import { createBrowserRouter, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { MainLayout } from './components/layout/MainLayout';
import { AuthLayout } from './components/layout/AuthLayout';

// Pages
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { DashboardPage } from './pages/DashboardPage';
import { MedicationsPage } from './pages/MedicationsPage';
import { MedicationDetailPage } from './pages/MedicationDetailPage';
import { IntakesPage } from './pages/IntakesPage';
import { ProfilePage } from './pages/ProfilePage';
import { NotFoundPage } from './pages/NotFoundPage';

export const router = createBrowserRouter([
  // Публичные маршруты (аутентификация)
  {
    element: <AuthLayout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
    ],
  },
  
  // Защищенные маршруты
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <MainLayout />,
        children: [
          { path: '/', element: <DashboardPage /> },
          { path: '/medications', element: <MedicationsPage /> },
          { path: '/medications/:id', element: <MedicationDetailPage /> },
          { path: '/intakes', element: <IntakesPage /> },
          { path: '/profile', element: <ProfilePage /> },
        ],
      },
    ],
  },
  
  // 404
  { path: '/404', element: <NotFoundPage /> },
  { path: '*', element: <Navigate to="/404" replace /> },
]);
```

### 17.2 Protected Route

```typescript
// components/auth/ProtectedRoute/ProtectedRoute.tsx

import { observer } from 'mobx-react-lite';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useStores } from '../../../hooks/useStores';
import { Loader } from '../../common/Loader';

export const ProtectedRoute = observer(() => {
  const { authStore } = useStores();
  const location = useLocation();

  // Ждем инициализации стора
  if (!authStore.isInitialized) {
    return <Loader fullScreen />;
  }

  // Если не авторизован — редирект на логин
  if (!authStore.isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
});
```

---

## 18. Хуки

### 18.1 useStores

```typescript
// hooks/useStores.ts

import { createContext, useContext } from 'react';
import { RootStore, rootStore } from '../stores/RootStore';

export const StoreContext = createContext<RootStore>(rootStore);

export const useStores = () => {
  const context = useContext(StoreContext);
  if (!context) {
    throw new Error('useStores must be used within StoreProvider');
  }
  return context;
};
```

### 18.2 useAuth

```typescript
// hooks/useAuth.ts

import { useStores } from './useStores';

export const useAuth = () => {
  const { authStore } = useStores();
  
  return {
    user: authStore.user,
    isAuthenticated: authStore.isAuthenticated,
    isLoading: authStore.isLoading,
    error: authStore.error,
    login: authStore.login.bind(authStore),
    register: authStore.register.bind(authStore),
    logout: authStore.logout.bind(authStore),
    clearError: authStore.clearError.bind(authStore),
  };
};
```

---

## 19. Оптимизация производительности

### 19.1 Мемоизация

```typescript
// Использовать React.memo для компонентов списков
const MedicationCard = React.memo(({ medication, onEdit, onDelete }) => {
  // ...
});

// Использовать useMemo для вычисляемых значений
const sortedMedications = useMemo(
  () => medications.sort((a, b) => a.name.localeCompare(b.name)),
  [medications]
);

// Использовать useCallback для обработчиков
const handleDelete = useCallback((id: string) => {
  // ...
}, []);
```

### 19.2 Ленивая загрузка

```typescript
// Ленивая загрузка страниц
const DashboardPage = React.lazy(() => import('./pages/DashboardPage'));
const MedicationsPage = React.lazy(() => import('./pages/MedicationsPage'));
// ...

// Обертка в Suspense
<Suspense fallback={<Loader fullScreen />}>
  <Routes>
    {/* ... */}
  </Routes>
</Suspense>
```

---

## 20. Переменные окружения

```env
# .env.example

# API URL
REACT_APP_API_URL=http://localhost:5000/api

# Название приложения
REACT_APP_NAME=MedicationAssist

# Режим разработки
REACT_APP_DEBUG=true
```

---

## 21. Запуск проекта

### 21.1 Установка зависимостей

```bash
npm install
# или
yarn install
```

### 21.2 Разработка

```bash
npm start
# или
yarn start
```

### 21.3 Сборка

```bash
npm run build
# или
yarn build
```

### 21.4 Тесты

```bash
npm test
# или
yarn test
```

---

## 22. Структура package.json

```json
{
  "name": "medication-assist-frontend",
  "version": "1.0.0",
  "private": true,
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.20.0",
    "mobx": "^6.12.0",
    "mobx-react-lite": "^4.0.5",
    "axios": "^1.6.2",
    "typescript": "^5.3.2"
  },
  "devDependencies": {
    "@types/react": "^18.2.42",
    "@types/react-dom": "^18.2.17",
    "@testing-library/react": "^14.1.2",
    "@testing-library/jest-dom": "^6.1.5"
  },
  "scripts": {
    "start": "react-scripts start",
    "build": "react-scripts build",
    "test": "react-scripts test",
    "eject": "react-scripts eject"
  }
}
```

---

## 23. Чеклист готовности

### 23.1 Функциональность

- [ ] Регистрация пользователя
- [ ] Вход в систему
- [ ] Выход из системы
- [ ] Просмотр списка лекарств
- [ ] Добавление лекарства
- [ ] Редактирование лекарства
- [ ] Удаление лекарства
- [ ] Регистрация приема лекарства (быстрый и полный)
- [ ] Просмотр истории приемов
- [ ] Фильтрация истории по дате
- [ ] Фильтрация истории по лекарству
- [ ] Редактирование записи о приеме
- [ ] Удаление записи о приеме
- [ ] Просмотр профиля
- [ ] Редактирование профиля

### 23.2 UI/UX

- [ ] Адаптивный дизайн (мобильный, планшет, десктоп)
- [ ] Состояния загрузки (Loader)
- [ ] Обработка ошибок
- [ ] Уведомления (Toast)
- [ ] Подтверждения удаления
- [ ] Пустые состояния
- [ ] Доступность (ARIA, клавиатурная навигация)

### 23.3 Качество кода

- [ ] TypeScript типизация
- [ ] Консистентный стиль кода
- [ ] Отсутствие ESLint ошибок
- [ ] Покрытие unit-тестами

---

## 24. Контакты и поддержка

При возникновении вопросов по спецификации API обращайтесь к документации:
- **Swagger UI**: `http://localhost:5000/swagger` (Development)
- **Backend спецификация**: `spec.md`

---

**Конец спецификации Frontend**

