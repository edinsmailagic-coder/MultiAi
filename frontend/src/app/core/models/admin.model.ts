export interface AdminUser {
  id: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  isDeleted: boolean;
  lockoutEnabled: boolean;
  isLockedOut: boolean;
  createdAt: string;
  roles: string[];
  permissions?: string[];
}

export interface CreateAdminUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface UpdateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive?: boolean;
  roles?: string[];
}

export interface AdminRole {
  id: string;
  name: string;
  userCount: number;
  permissions: string[];
}

export interface UpsertRoleRequest {
  name: string;
  permissions: string[];
}
